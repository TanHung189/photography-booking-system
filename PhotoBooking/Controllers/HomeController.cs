using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using PhotoBooking.Services;
using PhotoBooking.ViewModels;
using System.Diagnostics;
using System.Security.Claims;
using PhotoBooking.Web.Hubs;

namespace PhotoBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PhotoBookingContext _context;
        private readonly EmailSender _emailSender;
        private readonly IHubContext<BookingHub> _hubContext;
        public HomeController(PhotoBookingContext context, ILogger<HomeController> logger, EmailSender emailSender, IHubContext<BookingHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
            _hubContext = hubContext;
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

            viewModel.Photographers = _context.NguoiDungs
    .Where(u => u.VaiTro == "Photographer")
    .OrderByDescending(u => u.SoNamKinhNghiem)
    .Take(4)
    .Select(u => new PhotographerViewModel
    {
        User = u,

        // --- CODE MỚI: LẤY LIST ẢNH CHO SLIDE ---
        // Lấy ảnh đại diện của 3 gói dịch vụ đầu tiên làm slide
        SlideImages = _context.GoiDichVus
                              .Where(g => g.MaNhiepAnhGia == u.MaNguoiDung)
                              .Select(g => g.AnhDaiDien)
                              .Take(3)
                              .ToList()
    })
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

        // ==========================================
        // Action Xử lý Đặt lịch (POST) - Đã tích hợp Email & SignalR
        // ==========================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Book(int MaGoi, DateTime NgayChup, string DiaChiChup, string GhiChu)
        {
            // 1. Lấy ID người dùng từ Cookie
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int maKhachHang = int.Parse(userIdClaim.Value);

            // 2. Lấy thông tin Khách hàng (CHỈ KHAI BÁO 1 LẦN Ở ĐÂY)
            var khachHang = await _context.NguoiDungs.FindAsync(maKhachHang);

            // 3. Lấy thông tin Gói dịch vụ
            var package = await _context.GoiDichVus.FindAsync(MaGoi);
            if (package == null) return NotFound("Gói dịch vụ không tồn tại.");

            // 4. Tạo đơn hàng
            var donMoi = new DonDatLich
            {
                MaGoi = MaGoi,
                MaNhiepAnhGia = package.MaNhiepAnhGia, // Gán đúng chủ gói
                MaKhachHang = maKhachHang,
                NgayChup = NgayChup,
                DiaChiChup = DiaChiChup,
                GhiChu = GhiChu,
                TongTien = package.GiaTien,
                TienDaCoc = package.GiaCoc ?? 0,
                TrangThai = 0,
                TrangThaiThanhToan = 0,
                NgayTao = DateTime.Now
            };

            _context.Add(donMoi);
            await _context.SaveChangesAsync();

            // 5. --- GỬI THÔNG BÁO (SIGNALR) ---
            // Báo cho Admin/Nhiếp ảnh gia biết có đơn mới
            await _hubContext.Clients.All.SendAsync("ReceiveBooking", khachHang.HoVaTen, donMoi.MaDon);

            // 6. --- GỬI EMAIL XÁC NHẬN ---
            if (!string.IsNullOrEmpty(khachHang.Email))
            {
                string subject = $"[TAHU.FOTO] Xác nhận yêu cầu đặt lịch #{donMoi.MaDon}";
                string content = $@"
            <h3>Cảm ơn bạn đã đặt lịch tại TAHU.FOTO!</h3>
            <p>Xin chào <b>{khachHang.HoVaTen}</b>,</p>
            <p>Yêu cầu đặt lịch cho gói <b>{package.TenGoi}</b> đã được ghi nhận.</p>
            <ul>
                <li><b>Thời gian:</b> {donMoi.NgayChup:dd/MM/yyyy HH:mm}</li>
                <li><b>Địa điểm:</b> {donMoi.DiaChiChup}</li>
                <li><b>Tổng tiền:</b> {donMoi.TongTien:N0} đ</li>
            </ul>
            <p>Nhiếp ảnh gia sẽ sớm phản hồi yêu cầu của bạn.</p>
        ";

                // Gọi hàm gửi mail (không cần await để web chạy nhanh hơn)
                _emailSender.SendEmailAsync(khachHang.Email, subject, content);
            }

            // 7. Hoàn tất
            TempData["SuccessMessage"] = "🎉 Đặt lịch thành công! Vui lòng kiểm tra Email.";
            return RedirectToAction("Details", new { id = MaGoi });
        }


        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            // 1. Lấy ID an toàn (Tránh lỗi FormatException)
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var listDonHang = await _context.DonDatLiches // Hoặc DonDatLichs (Check lại tên trong Context)

                // A. Lấy thông tin Gói (để hiện tên gói, có thể null)
                .Include(d => d.MaGoiNavigation)

                // B. Lấy thông tin Thợ (QUAN TRỌNG: Lấy trực tiếp, không qua Gói)
                // Để đơn custom (không gói) vẫn hiện tên thợ
                .Include(d => d.MaNhiepAnhGiaNavigation)

                // C. Lấy đánh giá (Để check xem đơn này đã được đánh giá chưa)
                // Lưu ý: Tên property thường là DanhGia (số ít) hoặc DanhGias (số nhiều)
                // Kiểm tra file Model DonDatLich.cs để biết chính xác
                .Include(d => d.DanhGium)

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
