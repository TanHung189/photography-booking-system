using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using System.Security.Claims;

namespace PhotoBooking.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Photographer")]
    public class HomeController : Controller
    {
        private readonly PhotoBookingContext _context;

        public HomeController(PhotoBookingContext context)
        {
            _context = context;
        }

        // ==========================================
        // DASHBOARD: Thống kê + Danh sách đơn
        // ==========================================
        public IActionResult Index()
        {
            // 1. Lấy thông tin người dùng hiện tại
            var userIdStr = User.FindFirst("UserId")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            // 2. Chuẩn bị truy vấn
            var query = _context.DonDatLiches
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaGoiNavigation)
                .AsQueryable();

            // 3. Phân quyền dữ liệu
            if (userRole == "Photographer")
            {
                // Nếu là Photographer, chỉ lấy đơn của mình
                query = query.Where(d => d.MaGoiNavigation.MaNhiepAnhGia == userId);
            }

            // 4. Tính toán số liệu thống kê (ViewBag)
            // Lưu ý: Tính toán trên query đã lọc theo quyền
            ViewBag.TongDonHang = query.Count();
            ViewBag.DonChoDuyet = query.Count(d => d.TrangThai == 0);
            ViewBag.TongDoanhThu = query.Where(d => d.TrangThai == 2).Sum(d => d.TongTien ?? 0);

            // 5. Lấy danh sách đơn hàng để hiển thị bảng (Mới nhất lên đầu)
            var listDonHang = query.OrderByDescending(d => d.NgayTao).ToList();

            return View(listDonHang);
        }

        // ==========================================
        // Xử lý Duyệt đơn
        // ==========================================
        public async Task<IActionResult> Approve(int id)
        {
            var don = await _context.DonDatLiches.FindAsync(id);
            if (don != null)
            {
                don.TrangThai = 1; // 1 = Đã xác nhận
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // Xử lý Từ chối đơn
        // ==========================================
        public async Task<IActionResult> Reject(int id)
        {
            var don = await _context.DonDatLiches.FindAsync(id);
            if (don != null)
            {
                don.TrangThai = 3; // 3 = Hủy/Từ chối
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}