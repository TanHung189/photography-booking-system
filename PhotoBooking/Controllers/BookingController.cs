using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
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

        public BookingController(PhotoBookingContext context)
        {
            _context = context;
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
                // Lấy ID người dùng hiện tại
                var userIdStr = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
                int userId = int.Parse(userIdStr);

                // Lấy thông tin gói để xác định thợ
                var goiGoc = await _context.GoiDichVus.FindAsync(model.MaGoi);
                if (goiGoc == null) return NotFound();

                // 1. Tạo đơn hàng mới
                var donHang = new DonDatLich
                {
                    MaGoi = model.MaGoi,
                    MaKhachHang = userId,
                    MaNhiepAnhGia = goiGoc.MaNhiepAnhGia, // Lấy ID thợ từ gói
                    NgayChup = model.NgayChup,
                    DiaChiChup = model.DiaChiChup,
                    GhiChu = model.GhiChu,
                    TongTien = model.GiaTien,
                    TienDaCoc = 0, // Mới đặt chưa cọc
                    TrangThai = 0, // 0: Chờ duyệt
                    TrangThaiThanhToan = 0, // 0: Chưa thanh toán
                    NgayTao = DateTime.Now
                };

                _context.DonDatLiches.Add(donHang);
                await _context.SaveChangesAsync();

                // 2. LOGIC THÔNG MINH: Đặt xong chuyển thẳng sang trang Thanh Toán
                return RedirectToAction("Payment", new { id = donHang.MaDon });
            }

            // Nếu form lỗi thì hiện lại để sửa
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