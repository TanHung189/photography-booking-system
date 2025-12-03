using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBooking.Models; 
using PhotoBooking.Web.Services;
using System.Security.Claims;

namespace PhotoBooking.Controllers
{
    public class AccountController : Controller
    {
        private readonly PhotoBookingContext _context;// readonly (chỉ đọc) chốt an toàn
        private readonly PhotoService _photoService;
        public AccountController(PhotoBookingContext context, PhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
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
                new Claim(ClaimTypes.NameIdentifier, user.TenDangNhap),
                new Claim(ClaimTypes.Role, user.VaiTro),
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
            model.VaiTro = "Customer";
            model.NgayTao = DateTime.Now;
            // Avatar random nếu chưa có
            if (string.IsNullOrEmpty(model.AnhDaiDien))
            {
                model.AnhDaiDien = "https://ui-avatars.com/api/?name=" + model.HoVaTen + "&background=random";
            }

            // =========================================================
            // 2. XÓA BỎ KIỂM TRA LỖI CHO CÁC TRƯỜNG ĐÃ GÁN HOẶC KHÔNG CẦN
            // =========================================================
            // Vì ta đã gán VaiTro ở trên, nên ta xóa lỗi của nó đi (để chắc ăn)
            ModelState.Remove("VaiTro");
            ModelState.Remove("AnhDaiDien");
            ModelState.Remove("NgayTao");

            // Các bảng liên quan (như cũ)
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

        // xử lý đăng xuất logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
