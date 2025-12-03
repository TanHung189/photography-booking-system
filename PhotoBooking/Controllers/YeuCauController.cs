using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace PhotoBooking.Controllers
{
    [Authorize]
    public class YeuCauController : Controller
    {
        private readonly PhotoBookingContext _context;

        public YeuCauController(PhotoBookingContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH YÊU CẦU CỦA TÔI (Khách xem lại tin đã đăng)
        // ==========================================
        public async Task<IActionResult> MyRequests()
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var list = await _context.YeuCaus
                .Where(y => y.MaKhachHang == userId)
                .OrderByDescending(y => y.NgayTao)
                .ToListAsync();

            return View(list);
        }

        // ==========================================
        // 2. TẠO YÊU CẦU MỚI (GET)
        // ==========================================
        public IActionResult Create()
        {
            return View();
        }

        // ==========================================
        // 2. TẠO YÊU CẦU MỚI (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(YeuCau yeuCau)
        {
            // Tự động lấy ID người đang đăng nhập
            var userId = int.Parse(User.FindFirst("UserId").Value);
            yeuCau.MaKhachHang = userId;

            // Các giá trị mặc định
            yeuCau.NgayTao = DateTime.Now;
            yeuCau.TrangThai = 0; // 0 = Đang tìm

            // Bỏ qua check khóa ngoại
            ModelState.Remove("MaKhachHangNavigation");

            if (ModelState.IsValid)
            {
                _context.Add(yeuCau);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đăng tin tìm thợ thành công!";
                return RedirectToAction(nameof(MyRequests));
            }

            return View(yeuCau);
        }

        // ==========================================
        // 3. XÓA YÊU CẦU
        // ==========================================
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);
            var item = await _context.YeuCaus.FindAsync(id);

            // Chỉ được xóa tin của chính mình
            if (item != null && item.MaKhachHang == userId)
            {
                _context.YeuCaus.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa tin đăng.";
            }
            return RedirectToAction(nameof(MyRequests));
        }

        [Authorize]
        public async Task<IActionResult> JobMarket(string searchLocation, string sortOrder)
        {
            // 1. Query cơ bản (Chỉ lấy tin đang mở)
            var query = _context.YeuCaus
                .Include(y => y.MaKhachHangNavigation)
                .Where(y => y.TrangThai == 0)
                .AsQueryable();

            // 2. Xử lý Tìm kiếm theo Địa điểm (Nếu có nhập)
            if (!string.IsNullOrEmpty(searchLocation))
            {
                query = query.Where(y => y.DiaChi.Contains(searchLocation) || y.TieuDe.Contains(searchLocation));
                ViewBag.CurrentFilter = searchLocation; // Lưu lại để hiện lên ô input
            }

            // 3. Xử lý Sắp xếp
            switch (sortOrder)
            {
                case "price_desc": // Giá cao nhất
                    query = query.OrderByDescending(y => y.NganSach);
                    break;
                default: // Mới nhất (Mặc định)
                    query = query.OrderByDescending(y => y.NgayTao);
                    break;
            }

            var jobs = await query.ToListAsync();
            return View(jobs);
        }
    }
}
