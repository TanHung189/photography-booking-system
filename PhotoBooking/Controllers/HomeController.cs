using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using PhotoBooking.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace PhotoBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PhotoBookingContext _context;

        public HomeController(PhotoBookingContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }


        public IActionResult Index()
        {

            var viewModel = new HomeViewModel();




            viewModel.FeaturedPackages = _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation) // Kèm thông tin Photographer
                .Include(g => g.MaDanhMucNavigation)     // Kèm thông tin Danh m?c
                .OrderByDescending(g => g.MaGoi)         // M?i nh?t lên ??u
                .Take(6)                                 // Ch? l?y 6 cái
                .ToList();


            viewModel.Categories = _context.DanhMucs.ToList();


            viewModel.Locations = _context.DiaDiems.ToList();


            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Search(int? locationId, int? categoryId)
        {
            var query = _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .Include(g => g.MaDanhMucNavigation)
                .AsQueryable();

            //vi?t b? l?c 
            if (locationId.HasValue)
            {
                query = query.Where(g => g.
                MaNhiepAnhGiaNavigation.MaDiaDiem == locationId.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(g => g.MaDanhMuc == categoryId.Value);
            }
            //th?c thi truy v?n và l?y k?t qu? thành danh ssach
            var results = query.OrderByDescending(g => g.MaGoi).ToList();
            return View(results);
        }

        [HttpGet]
        public IActionResult Details(int? id)
        {
            // 1. Ki?m tra n?u id không h?p l?
            if (id == null || id == 0)
            {
                return NotFound(); // Tr? v? l?i 404
            }
            var package = _context.
                GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .Include(g => g.MaDanhMucNavigation)
                .FirstOrDefault(g => g.MaGoi == id);
            if (package == null)
            {
                return NotFound();
            }
            return View(package);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Authorize] // BẮT BUỘC: Chỉ cho phép người đã đăng nhập gọi hàm này
        public async Task<IActionResult> Book(int MaGoi, DateTime NgayChup, string DiaChiChup, string GhiChu)
        {
            // 1. Lấy ID của khách hàng đang đăng nhập
            // Chúng ta lấy nó từ cái Claim "UserId" đã lưu lúc đăng nhập
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null)
            {
                // Trường hợp hiếm: Đã đăng nhập nhưng không tìm thấy ID -> Bắt đăng nhập lại
                return RedirectToAction("Login", "Account");
            }
            int maKhachHang = int.Parse(userIdClaim.Value);

            // 2. Lấy thông tin Gói dịch vụ để biết giá tiền
            var package = await _context.GoiDichVus.FindAsync(MaGoi);
            if (package == null)
            {
                return NotFound("Gói dịch vụ không tồn tại.");
            }

            // 3. Tạo đối tượng Đơn đặt lịch mới (DonDatLich)
            var donMoi = new DonDatLich
            {
                MaGoi = MaGoi,
                MaKhachHang = maKhachHang,
                NgayChup = NgayChup,
                DiaChiChup = DiaChiChup,
                GhiChu = GhiChu,
                // Tự động lấy giá từ gói dịch vụ điền vào đơn
                TongTien = package.GiaTien,
                TienDaCoc = package.GiaCoc ?? 0, // Nếu giá cọc null thì lấy bằng 0
                                                 // Đặt trạng thái mặc định
                TrangThai = 0, // 0: Chờ duyệt
                TrangThaiThanhToan = 0, // 0: Chưa thanh toán
                NgayTao = DateTime.Now
            };

            // 4. Lưu vào Cơ sở dữ liệu
            _context.Add(donMoi);
            await _context.SaveChangesAsync();

            // 5. Thông báo thành công
            // Sử dụng TempData để gửi một tin nhắn ngắn sang trang kế tiếp
            TempData["SuccessMessage"] = "🎉 Chúc mừng! Bạn đã gửi yêu cầu đặt lịch thành công. Nhiếp ảnh gia sẽ sớm liên hệ lại.";

            // 6. Quay lại trang chi tiết gói chụp đó
            return RedirectToAction("Details", new { id = MaGoi });
        }


        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int userId = int.Parse(userIdClaim.Value);

            var listDonHang = await _context.DonDatLiches
        .Include(d => d.MaGoiNavigation)
            .ThenInclude(g => g.MaNhiepAnhGiaNavigation)
        // 👇 THÊM DÒNG NÀY:
        .Include(d => d.DanhGium) // Để view check (item.DanhGia == null)
                                 // 👆
        .Where(d => d.MaKhachHang == userId)
        .OrderByDescending(d => d.NgayTao)
        .ToListAsync();

            return View(listDonHang);
        }

        // ==========================================
        // Action Đặt lịch Trực tiếp (Không qua gói)
        // ==========================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> BookDirect(int MaNhiepAnhGia, DateTime NgayChup, string DiaChiChup, string GhiChu)
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int maKhachHang = int.Parse(userIdClaim.Value);

            // Tạo đơn mới
            var donMoi = new DonDatLich
            {
                MaNhiepAnhGia = MaNhiepAnhGia, // Lưu trực tiếp ID nhiếp ảnh gia
                MaGoi = null,                  // Không chọn gói -> Null
                MaKhachHang = maKhachHang,
                NgayChup = NgayChup,
                DiaChiChup = DiaChiChup,
                GhiChu = GhiChu,

                // Vì đặt trực tiếp nên giá là Thỏa thuận (0đ)
                TongTien = 0,
                TienDaCoc = 0,

                TrangThai = 0, // Chờ duyệt
                TrangThaiThanhToan = 0,
                NgayTao = DateTime.Now
            };

            _context.Add(donMoi);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "🎉 Đã gửi yêu cầu đặt lịch riêng! Vui lòng chờ nhiếp ảnh gia báo giá.";

            // Quay lại trang Profile của nhiếp ảnh gia đó
            return RedirectToAction("Profile", "Photographer", new { id = MaNhiepAnhGia });
        }

        // ==========================================
        // Action Gửi Đánh Giá (POST)
        // ==========================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitReview(int MaDon, int SoSao, string BinhLuan)
        {
            // Tạm thời bỏ qua kiểm tra người dùng và trạng thái để test việc lưu
            var review = new DanhGium
            {
                MaDon = MaDon,
                SoSao = SoSao,
                BinhLuan = BinhLuan ?? "Không có bình luận", // Tránh null
                NgayDanhGia = DateTime.Now
            };

            try
            {
                _context.DanhGia.Add(review);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã lưu đánh giá thành công!";
            }
            catch (Exception ex)
            {
                // Nếu lỗi, in ra màn hình console của Visual Studio để xem
                System.Diagnostics.Debug.WriteLine("LỖI LƯU DB: " + ex.Message);
                TempData["Error"] = "Lỗi lưu: " + ex.Message;
            }

            return RedirectToAction(nameof(MyBookings));
        }
    }
}
