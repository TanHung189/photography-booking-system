using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PhotoBooking.Models; 
using System.Security.Claims;
namespace PhotoBooking.Controllers
{
    public class AccountController : Controller
    {
        private readonly PhotoBookingContext _context;// readonly (chỉ đọc) chốt an toàn

        public AccountController(PhotoBookingContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            // Kiểm tra tài khoản trong CSDL
            var user = _context.NguoiDungs.FirstOrDefault(u => u.Email == email && u.MatKhau == password);
            if (user != null)
            {
                // Tạo các claim
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.HoVaTen),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("MaNguoiDung", user.MaNguoiDung.ToString())
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Lưu trạng thái đăng nhập
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Thời gian hết hạn
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            ViewBag.ErrorMessage = "Email hoặc mật khẩu không đúng.";
            return View();
        }

        // xử lý đăng xuất logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
