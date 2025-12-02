using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using PhotoBooking.ViewModels;

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
            // 1. Lấy dữ liệu từ DB (Kèm các bảng liên quan)
            var photographers = await _context.NguoiDungs
                .Include(u => u.MaDiaDiemNavigation)
                .Include(u => u.GoiDichVus)
                    .ThenInclude(g => g.DonDatLiches) // Hoặc DonDatLichs (tùy tên trong Model)
                        .ThenInclude(d => d.DanhGium)
                .Where(u => u.VaiTro == "Photographer")
                .OrderByDescending(u => u.NgayTao)
                .ToListAsync();

            // 2. Chuyển đổi sang ViewModel
            var viewModels = new List<PhotographerViewModel>();

            foreach (var p in photographers)
            {
                var vm = new PhotographerViewModel();
                vm.User = p;

                // a. Lấy ảnh slide (Lấy từ ảnh đại diện của các gói chụp)
                vm.SlideImages = p.GoiDichVus
                                  .Where(g => !string.IsNullOrEmpty(g.AnhDaiDien))
                                  .Select(g => g.AnhDaiDien)
                                  .Take(5)
                                  .ToList();

                // Nếu không có ảnh nào, dùng ảnh bìa hoặc ảnh demo
                if (vm.SlideImages.Count == 0)
                {
                    vm.SlideImages.Add(p.AnhBia ?? "https://source.unsplash.com/random/400x300/?camera");
                }

                // b. Tính điểm đánh giá
                // Gom tất cả đánh giá từ tất cả đơn hàng
                var allRatings = p.GoiDichVus
                                  .SelectMany(g => g.DonDatLiches)
                                  .Where(d => d.DanhGium != null)
                                  .Select(d => d.DanhGium.SoSao);

                vm.AvgRating = allRatings.Any() ? allRatings.Average() : 0;
                vm.ReviewCount = allRatings.Count();
                vm.PackageCount = p.GoiDichVus.Count;

                viewModels.Add(vm);
            }

            return View(viewModels);
        }


        public async Task<IActionResult> Profile(int id)
        {
            var user = await _context.NguoiDungs
                .Include(u => u.MaDiaDiemNavigation)
                .Include(u => u.GoiDichVus).ThenInclude(g => g.MaDanhMucNavigation)
                .Include(u => u.AlbumAnhs).ThenInclude(a => a.AnhChiTiets)
                // Include thêm để tính điểm đánh giá
                .Include(u => u.GoiDichVus).ThenInclude(g => g.DonDatLiches).ThenInclude(d => d.DanhGium)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);

            if (user == null) return NotFound();

            // Chuyển sang ViewModel
            var vm = new PhotographerViewModel();
            vm.User = user;
            vm.PackageCount = user.GoiDichVus.Count;
            vm.NamKinhNghiem = user.SoNamKinhNghiem ?? 0;

            // Tính điểm
            var allRatings = user.GoiDichVus.SelectMany(g => g.DonDatLiches)
                                 .Where(d => d.DanhGium != null).Select(d => d.DanhGium.SoSao);
            vm.AvgRating = allRatings.Any() ? allRatings.Average() : 0;
            vm.ReviewCount = allRatings.Count();

            return View(vm);
        }
    }
}
