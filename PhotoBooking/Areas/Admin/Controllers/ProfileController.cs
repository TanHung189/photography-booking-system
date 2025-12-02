using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBooking.Models;
using PhotoBooking.Web.Services;

namespace PhotoBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ProfileController : Controller
    {

        private readonly PhotoBookingContext _context;
        private readonly PhotoService _photoService;

        public ProfileController(PhotoBookingContext context, PhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        // ==========================================
        // 1. HIỂN THỊ THÔNG TIN CÁ NHÂN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy ID người đang đăng nhập từ Cookie
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account", new { area = "" });

            int userId = int.Parse(userIdStr);

            var user = await _context.NguoiDungs.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        // ==========================================
        // 2. CẬP NHẬT THÔNG TIN & AVATAR
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(NguoiDung model, IFormFile avatarFile, IFormFile coverFile)
        {
            // Lấy ID người đang đăng nhập (Để đảm bảo họ chỉ sửa được của chính mình)
            var userId = int.Parse(User.FindFirst("UserId").Value);

            // Lấy dữ liệu gốc từ DB
            var userInDb = await _context.NguoiDungs.FindAsync(userId);
            if (userInDb == null) return NotFound();

            // 1. Cập nhật thông tin chữ
            userInDb.HoVaTen = model.HoVaTen;
            userInDb.SoDienThoai = model.SoDienThoai;
            userInDb.Email = model.Email;
            userInDb.GioiThieu = model.GioiThieu;
            userInDb.SoNamKinhNghiem = model.SoNamKinhNghiem;

            // (Không cho sửa Tên đăng nhập, Mật khẩu, Vai trò ở đây)

            // 2. Xử lý Avatar
            if (avatarFile != null)
            {
                userInDb.AnhDaiDien = await _photoService.UploadPhotoAsync(avatarFile);
            }

            // 3. Xử lý Ảnh bìa (Nếu bạn muốn cho họ sửa cả ảnh bìa)
            if (coverFile != null)
            {
                userInDb.AnhBia = await _photoService.UploadPhotoAsync(coverFile);
            }

            // 4. Lưu lại
            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật hồ sơ thành công!";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
            }

            return View("Index", userInDb);
        }
    }
}
