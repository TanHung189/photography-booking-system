using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;

namespace PhotoBooking.Web.Areas.Admin.Controllers
{
    // ⚠️ BẮT BUỘC PHẢI CÓ DÒNG NÀY
    [Area("Admin")]
    // Chỉ Admin hoặc Photographer mới được vào
    [Authorize(Roles = "Admin,Photographer")]
    public class HomeController : Controller
    {
        private readonly PhotoBookingContext _context;

        public HomeController(PhotoBookingContext context)
        {
            _context = context;
        }

        // Dashboard chính của Admin
        public IActionResult Index()
        {
            // Lấy thống kê sơ bộ để hiển thị
            ViewBag.TongDonHang = _context.DonDatLiches.Count();
            ViewBag.DonChoDuyet = _context.DonDatLiches.Count(d => d.TrangThai == 0);
            ViewBag.TongDoanhThu = _context.DonDatLiches.Where(d => d.TrangThai == 2).Sum(d => d.TongTien);

            return View();
        }
    }
}