using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;

namespace PhotoBooking.Controllers
{
    public class PhotographerController : Controller
    {
        private readonly PhotoBookingContext _context;

        public PhotographerController(PhotoBookingContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy tất cả user có vai trò là Photographer
            // Kèm theo thông tin Địa điểm để hiển thị
            var photographers = await _context.NguoiDungs
                .Include(u => u.MaDiaDiemNavigation)
                .Where(u => u.VaiTro == "Photographer")
                .OrderByDescending(u => u.NgayTao) // Người mới lên đầu
                .ToListAsync();

            return View(photographers);
        }


        // Xem hồ sơ chi tiết của 1 Nhiếp ảnh gia
        public async Task<IActionResult> Profile(int id)
        {
            // 1. Lấy thông tin Nhiếp ảnh gia
            var photographer = await _context.NguoiDungs
                .Include(u => u.MaDiaDiemNavigation)
                // Lấy kèm danh sách Gói dịch vụ
                .Include(u => u.GoiDichVus).ThenInclude(g => g.MaDanhMucNavigation)
                // Lấy kèm danh sách Album ảnh (Portfolio)
                .Include(u => u.AlbumAnhs).ThenInclude(a => a.AnhChiTiets)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);

            if (photographer == null || photographer.VaiTro != "Photographer")
            {
                return NotFound();
            }

            return View(photographer);
        }
    }
}
