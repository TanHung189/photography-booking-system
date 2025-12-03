using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using PhotoBooking.ViewModels;
using System.Diagnostics;
using System.Security.Claims;
using PhotoBooking.Services;

namespace PhotoBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PhotoBookingContext _context;
        private readonly EmailSender _emailSender;
        public HomeController(PhotoBookingContext context, ILogger<HomeController> logger, EmailSender emailSender)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
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
        public IActionResult Search(string searchLocation, int? categoryId)
        {
            // 1. Bắt đầu truy vấn
            var query = _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation)
                    .ThenInclude(u => u.MaDiaDiemNavigation) // Include thêm bảng Địa điểm của Nhiếp ảnh gia
                .Include(g => g.MaDanhMucNavigation)
                .AsQueryable();

            // 2. Lọc theo TÊN ĐỊA ĐIỂM (Thay vì ID)
            if (!string.IsNullOrEmpty(searchLocation))
            {
                // Tìm những gói mà Nhiếp ảnh gia có địa chỉ chứa từ khóa (VD: Hà Nội)
                query = query.Where(g => g.MaNhiepAnhGiaNavigation.MaDiaDiemNavigation.TenThanhPho.Contains(searchLocation));
            }

            // 3. Lọc theo Danh mục (Giữ nguyên vì dropdown danh mục vẫn dùng ID)
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(g => g.MaDanhMuc == categoryId.Value);
            }

            // 4. Sắp xếp & Lấy kết quả
            var result = query.OrderByDescending(g => g.MaGoi).ToList();

            // Lưu lại để hiển thị trên View
            ViewBag.SelectedLocation = searchLocation;
            ViewBag.SelectedCategory = categoryId;

            return View(result);
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

            // --- GỬI EMAIL XÁC NHẬN CHO KHÁCH ---
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value; // Cách lấy email tùy vào lúc login bạn lưu claim nào, nếu chưa lưu Email thì phải query DB lại.
            var khachHang = await _context.NguoiDungs.FindAsync(maKhachHang);

            if (!string.IsNullOrEmpty(khachHang.Email))
            {
                string subject = $"[PotoBooking] Xác nhận đơn đặt lịch #{donMoi.MaDon}";
                string content = $@"
            <h3>Cảm ơn bạn đã đặt lịch tại PotoBooking!</h3>
            <p>Xin chào <b>{khachHang.HoVaTen}</b>,</p>
            <p>Yêu cầu đặt lịch của bạn đã được gửi đi. Nhiếp ảnh gia sẽ sớm phản hồi.</p>
            <ul>
                <li><b>Mã đơn:</b> #{donMoi.MaDon}</li>
                <li><b>Ngày chụp:</b> {donMoi.NgayChup:dd/MM/yyyy HH:mm}</li>
                <li><b>Địa điểm:</b> {donMoi.DiaChiChup}</li>
                <li><b>Tổng tiền:</b> {donMoi.TongTien:N0} đ</li>
            </ul>
            <p>Vui lòng truy cập website để theo dõi trạng thái đơn hàng.</p>
        ";

                await _emailSender.SendEmailAsync(khachHang.Email, subject, content);
            }
          
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
