using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using PhotoBooking.Services;
using PhotoBooking.ViewModels; // Nhớ using cái này
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PhotoBooking.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly PhotoBookingContext _context;
        private readonly BlockchainService _blockchainService;
        public BookingController(PhotoBookingContext context, BlockchainService blockchainService)
        {
            _context = context;
            _blockchainService = blockchainService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // 1. CHỨC NĂNG ĐẶT LỊCH (MỚI THÊM)
        // ==========================================

        // GET: Hiển thị trang điền thông tin đặt lịch
        [HttpGet]
        public async Task<IActionResult> Create(int packageId)
        {
            var goi = await _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .FirstOrDefaultAsync(g => g.MaGoi == packageId);

            if (goi == null) return NotFound();

            // Đổ dữ liệu từ Gói sang ViewModel để hiện lên form
            var viewModel = new BookingViewModel
            {
                MaGoi = goi.MaGoi,
                TenGoi = goi.TenGoi,
                GiaTien = goi.GiaTien,
                GiaCoc = goi.GiaCoc ?? 0, // Nếu null thì lấy 0
                AnhBia = goi.AnhDaiDien,
                TenNhiepAnhGia = goi.MaNhiepAnhGiaNavigation?.HoVaTen ?? "Nhiếp ảnh gia"
            };

            return View(viewModel);
        }

        // POST: Xử lý lưu đơn đặt lịch
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(BookingViewModel model)
{
    if (ModelState.IsValid)
    {
        var userIdStr = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
        int userId = int.Parse(userIdStr);

        var goiGoc = await _context.GoiDichVus.FindAsync(model.MaGoi);
        if (goiGoc == null) return NotFound();

        // --- BẮT ĐẦU LOGIC BLOCKCHAIN ---
        string smartContractAddress = "";
        try 
        {
            // Tự động gọi Service để đúc một Két sắt mới trên Ganache cho đơn hàng này
            smartContractAddress = await _blockchainService.DeployContractAsync();
        }
        catch (Exception ex)
        {
            // Nếu lỗi Blockchain, bạn có thể báo lỗi hoặc gán tạm địa chỉ rỗng
            ModelState.AddModelError("", "Không thể khởi tạo hợp đồng Blockchain: " + ex.Message);
            return View(model);
        }
        // --- KẾT THÚC LOGIC BLOCKCHAIN ---

        var donHang = new DonDatLich
        {
            MaGoi = model.MaGoi,
            MaKhachHang = userId,
            MaNhiepAnhGia = goiGoc.MaNhiepAnhGia,
            NgayChup = model.NgayChup,
            DiaChiChup = model.DiaChiChup,
            GhiChu = model.GhiChu,
            TongTien = model.GiaTien,
            TienDaCoc = 0,
            TrangThai = 0,
            TrangThaiThanhToan = 0,
            NgayTao = DateTime.Now,
            
            // LƯU Ý: Bạn nên thêm một cột "ContractAddress" vào bảng DonDatLich trong Database 
            // để lưu cái địa chỉ này lại nhé!
            // GhiChu = "Blockchain Address: " + smartContractAddress // Tạm thời lưu vào ghi chú nếu chưa có cột riêng
        };

        _context.DonDatLiches.Add(donHang);
        await _context.SaveChangesAsync();

        return RedirectToAction("Payment", new { id = donHang.MaDon });
    }
    return View(model);
}

        public IActionResult BookingSuccess()
        {
            return View();
        }

        // ==========================================
        // 2. CHỨC NĂNG THANH TOÁN (CODE CŨ CỦA BẠN)
        // ==========================================

        [HttpGet]
        public IActionResult Payment(int id)
        {
            // Lấy thông tin đơn hàng
            var donHang = _context.DonDatLiches
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaNhiepAnhGiaNavigation)
                .Include(d => d.MaGoiNavigation)
                .FirstOrDefault(d => d.MaDon == id);

            if (donHang == null) return NotFound();

            // Bảo mật: Chỉ chủ đơn hoặc Admin mới được xem
            var userIdStr = User.FindFirst("UserId")?.Value;
            int userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            if (donHang.MaKhachHang != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Nếu đã thanh toán rồi thì đá về lịch sử
            if (donHang.TrangThaiThanhToan > 0)
            {
                return RedirectToAction("MyBookings", "Home");
            }

            return View(donHang);
        }

        // Xác nhận đã chuyển khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var donHang = await _context.DonDatLiches.FindAsync(id);
            if (donHang == null) return NotFound();

            // Cập nhật trạng thái
            donHang.TrangThaiThanhToan = 1; // 1 = Đã cọc
            donHang.TrangThai = 1;          // 1 = Đã xác nhận

            _context.Update(donHang);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xác nhận thanh toán thành công! Nhiếp ảnh gia sẽ liên hệ với bạn sớm.";

            // Chuyển hướng về trang Lịch sử đơn hàng
            return RedirectToAction("MyBookings", "Home");
        }
    }
}