using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using PhotoBooking.Services;
using PhotoBooking.Web.Services;
using System.Security.Claims;


namespace PhotoBooking.Controllers
{
    public class AccountController : Controller
    {
        private readonly PhotoBookingContext _context;// readonly (chỉ đọc) chốt an toàn
        private readonly PhotoService _photoService;
        private readonly EmailSender _emailSender;

        public AccountController(PhotoBookingContext context, PhotoService photoService, EmailSender emailSender)
        {
            _context = context;
            _photoService = photoService;
            _emailSender = emailSender;
        }

        //Get: hiển thị trang đăng nhập
        public IActionResult Login(string returnUrl = null)
        {
            // dòng này thể hiện nếu đã đăng nhập rồi thì không cho vào lại trang login nữa
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        //Post: xử lý nút đăng nhập
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1. Kiểm tra trong Database (Tìm người dùng có username và password khớp)
            // Lưu ý: Hiện tại chúng ta đang so sánh mật khẩu dạng thô (plaintext).
            // Ở giai đoạn sau, chúng ta NÊN nâng cấp lên mã hóa MD5 hoặc BCrypt.
            var user = _context.NguoiDungs
        .FirstOrDefault(u => u.TenDangNhap == username);

            // 2. Kiểm tra:
            // - User có tồn tại không?
            // - Mật khẩu nhập vào (password) có khớp với mã hóa trong DB (user.MatKhau) không?
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.MatKhau))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                return View();
            }

            // 3. Nếu tìm thấy -> Đăng nhập thành công -> Tạo "Vé thông hành" (Claims)
            // Chúng ta sẽ lưu những thông tin cần thiết nhất vào Cookie để dùng lại sau này
            var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, user.HoVaTen),
    
    // --- SỬA QUAN TRỌNG: NameIdentifier nên lưu ID số (hoặc GUID) ---
    // Điều này giúp User.Identity.Name thì ra tên, nhưng định danh chính lại là ID
    new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),

    new Claim(ClaimTypes.Role, user.VaiTro),
    
    // Giữ nguyên custom claim này để tương thích code cũ
    new Claim("UserId", user.MaNguoiDung.ToString()),

    new Claim("Avatar", user.AnhDaiDien ?? "")
};

            // Tạo danh tính (Identity) từ các thông tin trên, xác nhận dùng "Kiểu Cookie"
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Tạo đối tượng người dùng chính (Principal) nắm giữ danh tính đó
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // 4. Ghi Cookie xuống trình duyệt (Bước quan trọng nhất)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = true, // Tự động ghi nhớ đăng nhập (như tích vào ô "Remember me")
                    ExpiresUtc = DateTime.UtcNow.AddDays(7) // Cookie sẽ hết hạn sau 7 ngày
                }
            );

            // 5. Chuyển hướng về trang chủ
            return RedirectToAction("Index", "Home");
        }

        // =============================================
        // GET: Hiển thị trang Đăng ký
        // =============================================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // =============================================
        // POST: Xử lý Đăng ký
        // =============================================
        [HttpPost]
        public async Task<IActionResult> Register(NguoiDung model, string ConfirmPassword)
        {
            // =========================================================
            // 1. GÁN GIÁ TRỊ MẶC ĐỊNH TRƯỚC (QUAN TRỌNG)
            // =========================================================
            // Phải gán ngay để ModelState không báo lỗi "VaiTro field is required"
            if (model.VaiTro != "Photographer")
            {
                // Nếu người dùng chọn lung tung hoặc không chọn -> Mặc định về Customer
                // Dòng này cũng chặn luôn việc ai đó cố tình gửi "Admin" lên
                model.VaiTro = "Customer";
            }
            model.NgayTao = DateTime.Now;
            model.SoNamKinhNghiem = 0; // Mặc định kinh nghiệm là 0
            if (string.IsNullOrEmpty(model.AnhDaiDien))
            {
                model.AnhDaiDien = "https://ui-avatars.com/api/?name=" + model.HoVaTen + "&background=random";
            }

            // =========================================================
            // 2. XÓA BỎ KIỂM TRA LỖI CHO CÁC TRƯỜNG ĐÃ GÁN HOẶC KHÔNG CẦN
            // =========================================================
            // Vì ta đã gán VaiTro ở trên, nên ta xóa lỗi của nó đi (để chắc ăn)

            ModelState.Remove("AnhDaiDien");
            ModelState.Remove("NgayTao");
            ModelState.Remove("SoNamKinhNghiem");

            // Các bảng liên quan (như cũ)
            ModelState.Remove("MaDiaDiem");
            ModelState.Remove("MaDiaDiemNavigation");
            ModelState.Remove("DonDatLichMaKhachHangNavigations");
            ModelState.Remove("DonDatLichMaNhiepAnhGiaNavigations");
            ModelState.Remove("GoiDichVus");
            ModelState.Remove("AlbumAnhs");
            ModelState.Remove("DanhGia");

            // =========================================================
            // 3. BẮT ĐẦU KIỂM TRA
            // =========================================================
            if (ModelState.IsValid)
            {
                // Kiểm tra mật khẩu xác nhận
                if (model.MatKhau != ConfirmPassword)
                {
                    ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                    return View(model);
                }

                // Kiểm tra trùng tên đăng nhập
                if (_context.NguoiDungs.Any(u => u.TenDangNhap == model.TenDangNhap))
                {
                    ViewBag.Error = "Tên đăng nhập này đã được sử dụng!";
                    return View(model);
                }

                // Mã hóa mật khẩu
                model.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);

                // Lưu vào DB
                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Hãy đăng nhập.";
                return RedirectToAction("Login");
            }

            // DEBUG: Nếu vẫn lỗi thì xem nó báo gì ở Output
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var err in errors)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi Validation: " + err.ErrorMessage);
            }

            return View(model);
        }

        // ==========================================
        // XEM & SỬA HỒ SƠ CÁ NHÂN (GET)
        // ==========================================
        [Authorize]
        public async Task<IActionResult> ProfileCustomer()
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);
            var user = await _context.NguoiDungs.FindAsync(userId);
            return View(user);
        }

        // ==========================================
        // CẬP NHẬT THÔNG TIN (POST)
        // ==========================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(NguoiDung model, IFormFile avatarFile)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);
            var userInDb = await _context.NguoiDungs.FindAsync(userId);

            if (userInDb == null) return NotFound();

            // 1. Cập nhật thông tin cơ bản
            userInDb.HoVaTen = model.HoVaTen;
            userInDb.SoDienThoai = model.SoDienThoai;
            userInDb.Email = model.Email;

            // 2. Xử lý Avatar (Nếu có chọn ảnh mới)
            if (avatarFile != null)
            {
                userInDb.AnhDaiDien = await _photoService.UploadPhotoAsync(avatarFile);
            }

            _context.Update(userInDb);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction(nameof(ProfileCustomer));
        }

        // ==========================================
        // ĐỔI MẬT KHẨU (POST)
        // ==========================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);
            var user = await _context.NguoiDungs.FindAsync(userId);

            // 1. Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, user.MatKhau))
            {
                TempData["ErrorPass"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction(nameof(ProfileCustomer));
            }

            // 2. Kiểm tra xác nhận mật khẩu mới
            if (NewPassword != ConfirmNewPassword)
            {
                TempData["ErrorPass"] = "Xác nhận mật khẩu mới không khớp.";
                return RedirectToAction(nameof(ProfileCustomer));
            }

            // 3. Đổi mật khẩu
            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessPass"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(ProfileCustomer));
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //Actio Quên mật khẩu
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Bảo mật: Không báo lỗi "Email không tồn tại" để tránh hacker dò mail
                // Cứ báo thành công ảo hoặc báo chung chung
                TempData["Success"] = "Nếu email tồn tại, mã xác nhận đã được gửi.";
                return View();
            }

            // Tạo mã ngẫu nhiên 6 số
            string code = new Random().Next(100000, 999999).ToString();

            // Lưu vào DB (Hết hạn sau 10 phút)
            user.MaXacNhan = code;
            user.HanMaXacNhan = DateTime.Now.AddMinutes(10);
            await _context.SaveChangesAsync();

            // Gửi Email
            string subject = "[PotoBooking] Mã xác nhận quên mật khẩu";
            string body = $"<h3>Mã xác nhận của bạn là: <b style='color:red; font-size:20px;'>{code}</b></h3>" +
                          "<p>Mã này có hiệu lực trong 10 phút. Tuyệt đối không chia sẻ cho ai.</p>";

            await _emailSender.SendEmailAsync(email, subject, body);

            // Chuyển sang trang Nhập mã
            return RedirectToAction("ResetPassword", new { email = email });
        }

        // ==========================================
        // 2. TRANG ĐẶT LẠI MẬT KHẨU (Nhập Mã + Pass mới)
        // ==========================================
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string code, string newPassword, string confirmPassword)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email);

            // Kiểm tra các điều kiện
            if (user == null)
            {
                ViewBag.Error = "Email không hợp lệ.";
                return View();
            }
            if (user.MaXacNhan != code)
            {
                ViewBag.Error = "Mã xác nhận không đúng.";
                ViewBag.Email = email; return View();
            }
            if (user.HanMaXacNhan < DateTime.Now)
            {
                ViewBag.Error = "Mã xác nhận đã hết hạn. Vui lòng thử lại.";
                ViewBag.Email = email; return View();
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                ViewBag.Email = email; return View();
            }

            // Đổi mật khẩu thành công
            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Xóa mã xác nhận đi để không dùng lại được
            user.MaXacNhan = null;
            user.HanMaXacNhan = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Hãy đăng nhập ngay.";
            return RedirectToAction("Login");
        }

        // xử lý đăng xuất logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
