using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nethereum.Util;
using PhotoBooking.Models;
using PhotoBooking.Services;
using PhotoBooking.ViewModels;
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
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public BookingController(PhotoBookingContext context, BlockchainService blockchainService, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _blockchainService = blockchainService;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // 1. CHỨC NĂNG ĐẶT LỊCH
        // ==========================================

        // GET: Hiển thị trang điền thông tin đặt lịch
        [HttpGet]
        public async Task<IActionResult> Create(int? packageId)
        {
            if (packageId == null || packageId == 0) return RedirectToAction("Index", "Home");

            var goi = await _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .FirstOrDefaultAsync(g => g.MaGoi == packageId);

            if (goi == null) return NotFound("Không tìm thấy gói nào ứng với ID vừa cung cấp.");

            // Đổ dữ liệu từ Gói sang ViewModel để hiện lên form
            var viewModel = new BookingViewModel
            {
                MaGoi = goi.MaGoi,
                TenGoi = goi.TenGoi,
                GiaTien = goi.GiaTien,
                GiaCoc = goi.GiaCoc ?? 0,
                AnhBia = goi.AnhDaiDien,
                TenNhiepAnhGia = goi.MaNhiepAnhGiaNavigation?.HoVaTen ?? "Nhiếp ảnh gia"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            Console.WriteLine("------------------------------------------");
            Console.WriteLine($">>> NHẬN ĐƠN HÀNG MỚI: Gói {model.MaGoi}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== FORM DỮ LIỆU ĐANG BỊ LỖI RÀNG BUỘC ===");
                foreach (var modelStateKey in ModelState.Keys)
                {
                    var value = ModelState[modelStateKey];
                    foreach (var error in value!.Errors)
                        Console.WriteLine($"Lỗi tại {modelStateKey}: {error.ErrorMessage}");
                }
                return View(model);
            }

            // --- Lấy thông tin người dùng hiện tại ---
            var userIdStr = User.FindFirst("UserId")?.Value;
            int userId = int.Parse(userIdStr ?? "0");

            // --- Truy vấn gói dịch vụ kèm thông tin nhiếp ảnh gia (bao gồm DiaChiVi) ---
            Console.WriteLine(">>> [Bước 1] Đang lấy thông tin gói dịch vụ và ví thợ ảnh...");
            var goiGoc = await _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation) // Lấy NguoiDung.DiaChiVi
                .FirstOrDefaultAsync(g => g.MaGoi == model.MaGoi);

            if (goiGoc == null) return NotFound("Không tìm thấy gói dịch vụ.");

            // =====================================================================
            // CYBERSECURITY CHECK: Xác thực địa chỉ ví Ethereum của nhiếp ảnh gia
            // =====================================================================
            string? viThoAnh = goiGoc.MaNhiepAnhGiaNavigation?.DiaChiVi;
            Console.WriteLine($">>> [Bước 1] Địa chỉ ví thợ ảnh từ DB (DiaChiVi): {viThoAnh ?? "NULL"}");

            var addressUtil = new AddressUtil();
            if (string.IsNullOrWhiteSpace(viThoAnh) || !addressUtil.IsValidEthereumAddressHexFormat(viThoAnh))
            {
                Console.WriteLine("!!! BẢO MẬT: Từ chối giao dịch - Ví thợ ảnh không hợp lệ.");
                ModelState.AddModelError(string.Empty,
                    "Nhiếp ảnh gia chưa thiết lập địa chỉ ví Ethereum. " +
                    "Giao dịch bị từ chối vì không thể tạo Hợp đồng Thông minh. " +
                    "Vui lòng chọn nhiếp ảnh gia khác hoặc liên hệ Admin.");
                return View(model);
            }

            Console.WriteLine($">>> [Bước 1] Ví thợ ảnh hợp lệ: {viThoAnh}");

            // =====================================================================
            // BƯỚC 2: Khởi tạo đơn hàng trong SQL với TrangThai = PENDING
            // =====================================================================
            Console.WriteLine(">>> [Bước 2] Đang lưu đơn hàng tạm vào SQL với trạng thái PENDING...");
            var donHang = new DonDatLich
            {
                MaGoi            = model.MaGoi,
                MaKhachHang      = userId,
                MaNhiepAnhGia    = goiGoc.MaNhiepAnhGia,
                NgayChup         = model.NgayChup,
                DiaChiChup       = model.DiaChiChup ?? "Địa điểm mặc định",
                GhiChu           = model.GhiChu,
                TongTien         = model.GiaTien,
                DiaChiHopDong    = "INITIALIZING", // Trạng thái trước khi có địa chỉ hợp đồng
                NgayTao          = DateTime.Now,
                TrangThai        = 0,              // 0 = Pending
                TrangThaiThanhToan = 0
            };

            _context.DonDatLiches.Add(donHang);
            await _context.SaveChangesAsync();

            Console.WriteLine($">>> [Bước 2] Đã tạo đơn hàng tạm: ID #{donHang.MaDon}. DiaChiHopDong = 'INITIALIZING'");

            // =====================================================================
            // BƯỚC 3 & 4: Atomic Blockchain Workflow – Deploy Smart Contract
            // =====================================================================
            try
            {
                Console.WriteLine($">>> [Bước 3] Đang gọi Ganache để deploy hợp đồng cho thợ ảnh: {viThoAnh}");
                var khachHang = await _context.NguoiDungs.FindAsync(userId); // userId lấy từ Session hoặc User.Identity
                string viKhachHang = khachHang?.DiaChiVi ?? "0x0000000000000000000000000000000000000000";
                string contractAddress = await _blockchainService.DeployContractAsync(viThoAnh, viKhachHang);
                // BƯỚC 3: Cập nhật địa chỉ hợp đồng thực tế vào DB
                Console.WriteLine($">>> [Bước 3] Deploy thành công! Đang cập nhật DiaChiHopDong = {contractAddress}");
                var donHangDaLuu = await _context.DonDatLiches.FindAsync(donHang.MaDon);
                if (donHangDaLuu != null)
                {
                    donHangDaLuu.DiaChiHopDong = contractAddress;
                    await _context.SaveChangesAsync();
                }

                Console.WriteLine($"✅ Đã cập nhật DiaChiHopDong thành công cho đơn #{donHang.MaDon}");
                Console.WriteLine("------------------------------------------");
            }
            catch (Exception ex)
            {
                // BƯỚC 4: Giao dịch Blockchain thất bại – cập nhật FAILED_ON_CHAIN để đối soát
                Console.WriteLine($"!!! LỖI BLOCKCHAIN (Đơn #{donHang.MaDon}): {ex.Message}");

                try
                {
                    var donHangFail = await _context.DonDatLiches.FindAsync(donHang.MaDon);
                    if (donHangFail != null)
                    {
                        donHangFail.DiaChiHopDong = "FAILED_ON_CHAIN";
                        await _context.SaveChangesAsync();
                        Console.WriteLine($">>> Đã ghi FAILED_ON_CHAIN vào đơn #{donHang.MaDon} để đối soát.");
                    }
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"!!! Lỗi khi cập nhật FAILED_ON_CHAIN: {dbEx.Message}");
                }

                // Ghi log chi tiết ra file
                string logPath = System.IO.Path.Combine(_env.ContentRootPath, "blockchain_error.txt");
                System.IO.File.AppendAllText(logPath,
                    $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Đơn #{donHang.MaDon}: {ex}");

                ModelState.AddModelError(string.Empty,
                    "Đặt lịch thành công nhưng không thể tạo Hợp đồng Thông minh trên Blockchain. " +
                    "Đơn hàng đã được lưu với trạng thái FAILED_ON_CHAIN. Vui lòng liên hệ Admin để xử lý.");
                return View(model);
            }

            return RedirectToAction("Payment", new { id = donHang.MaDon });
        }

        public IActionResult BookingSuccess()
        {
            return View();
        }

        // ==========================================
        // 2. CHỨC NĂNG THANH TOÁN
        // ==========================================

        [HttpGet]
        public IActionResult Payment(int id)
        {
            var donHang = _context.DonDatLiches
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaNhiepAnhGiaNavigation)
                .Include(d => d.MaGoiNavigation)
                .FirstOrDefault(d => d.MaDon == id);

            if (donHang == null) return NotFound();

            var userIdStr = User.FindFirst("UserId")?.Value;
            int userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            if (donHang.MaKhachHang != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

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

            donHang.TrangThaiThanhToan = 1; // 1 = Đã cọc
            donHang.TrangThai = 1;          // 1 = Đã xác nhận

            _context.Update(donHang);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xác nhận thanh toán thành công! Nhiếp ảnh gia sẽ liên hệ với bạn sớm.";

            return RedirectToAction("MyBookings", "Home");
        }


        [HttpGet]
        public async Task<IActionResult> ConfirmBlockchainPayment(int id)
        {
            // Tìm đơn hàng trong DB
            var donHang = await _context.DonDatLiches.FindAsync(id);
            
            if (donHang == null) 
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            // Cập nhật trạng thái sang "Đã đặt cọc"
            donHang.TrangThaiThanhToan = 1; // 1 = Đã cọc
            donHang.TrangThai = 1;          // 1 = Đã xác nhận/Đã cọc
            
            // Bạn có thể lưu thêm Hash giao dịch vào Ghi chú nếu muốn (Tùy chọn)
            donHang.GhiChu += $" [Paid via Blockchain at {DateTime.Now:HH:mm dd/MM}]";

            _context.Update(donHang);
            await _context.SaveChangesAsync();

            TempData["Success"] = "🎉 Tuyệt vời! Hệ thống đã ghi nhận khoản đặt cọc qua Blockchain.";

            // Chuyển hướng người dùng về trang danh sách đơn hàng của họ
            return RedirectToAction("MyBookings", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> BookingSuccess(int id)
        {
            var donHang = await _context.DonDatLiches.FindAsync(id);
            
            // Cập nhật trạng thái thành Đã thanh toán / Đã cọc
            if (donHang != null && donHang.TrangThaiThanhToan == 0)
            {
                donHang.TrangThaiThanhToan = 1; 
                donHang.TrangThai = 1;          
                await _context.SaveChangesAsync();
            }

            // Truyền dữ liệu đơn hàng ra màn hình Thành công
            return View(donHang);
        }
    }
}