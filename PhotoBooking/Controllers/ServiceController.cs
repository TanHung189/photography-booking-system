using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;

namespace PhotoBooking.Web.Controllers
{
    public class ServiceController : Controller
    {
        private readonly PhotoBookingContext _context;

        public ServiceController(PhotoBookingContext context)
        {
            _context = context;
        }

        // ==========================================
        // DANH SÁCH TẤT CẢ GÓI CHỤP
        // ==========================================
        public async Task<IActionResult> Index(string sortOrder)
        {
            // 1. Chuẩn bị truy vấn
            var query = _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation) // Lấy tên Nhiếp ảnh gia
                .Include(g => g.MaDanhMucNavigation)     // Lấy tên Danh mục
                .AsQueryable();

            // 2. Xử lý Sắp xếp (Sort) nếu người dùng chọn
            ViewBag.PriceSortParam = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            ViewBag.DateSortParam = String.IsNullOrEmpty(sortOrder) ? "date_old" : "";

            switch (sortOrder)
            {
                case "price_asc": // Giá thấp -> cao
                    query = query.OrderBy(g => g.GiaTien);
                    break;
                case "price_desc": // Giá cao -> thấp
                    query = query.OrderByDescending(g => g.GiaTien);
                    break;
                case "date_old": // Cũ nhất trước
                    query = query.OrderBy(g => g.MaGoi);
                    break;
                default: // Mặc định: Mới nhất trước
                    query = query.OrderByDescending(g => g.MaGoi);
                    break;
            }

            // 3. Lấy dữ liệu
            var services = await query.ToListAsync();

            return View(services);
        }
    }
}