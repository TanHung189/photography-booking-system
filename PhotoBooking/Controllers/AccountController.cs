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
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1. Kiểm tra trong Database (Tìm người dùng có username và password khớp)
            // Lưu ý: Hiện tại chúng ta đang so sánh mật khẩu dạng thô (plaintext).
            // Ở giai đoạn sau, chúng ta NÊN nâng cấp lên mã hóa MD5 hoặc BCrypt.
            var user = _context.NguoiDungs
                .FirstOrDefault(u => u.TenDangNhap == username && u.MatKhau == password);

            // 2. Nếu không tìm thấy (user == null) -> Đăng nhập thất bại
            if (user == null)
            {
                // Gửi thông báo lỗi sang View để hiển thị lên
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                // Trả về lại trang Login để nhập lại
                return View();
            }

            // 3. Nếu tìm thấy -> Đăng nhập thành công -> Tạo "Vé thông hành" (Claims)
            // Chúng ta sẽ lưu những thông tin cần thiết nhất vào Cookie để dùng lại sau này
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.HoVaTen),            // Lưu Họ tên (để hiển thị "Chào,Bùi Đỗ Tấn Hưng")
                new Claim(ClaimTypes.NameIdentifier, user.TenDangNhap), // Lưu Username (ID định danh chính)
                new Claim(ClaimTypes.Role, user.VaiTro),             // Lưu Vai trò (để phân quyền Admin/Khách)
                new Claim("UserId", user.MaNguoiDung.ToString()),    // Lưu ID số (để truy vấn dữ liệu liên quan)
                new Claim("Avatar", user.AnhDaiDien ?? "")           // Lưu link Avatar (nếu có, không thì để rỗng)
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

        // xử lý đăng xuất logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
