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
            // A. Lấy thông tin Nhiếp ảnh gia và toàn bộ dữ liệu liên quan
            var user = await _context.NguoiDungs
                .Include(u => u.MaDiaDiemNavigation)
                // Lấy Gói dịch vụ -> Đơn hàng -> Đánh giá
                .Include(u => u.GoiDichVus).ThenInclude(g => g.DonDatLiches).ThenInclude(d => d.DanhGium)
                .Include(u => u.GoiDichVus).ThenInclude(g => g.MaDanhMucNavigation)
                // Lấy Album -> Ảnh chi tiết
                .Include(u => u.AlbumAnhs).ThenInclude(a => a.AnhChiTiets)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);

            if (user == null || user.VaiTro != "Photographer")
            {
                return NotFound();
            }

            // B. Chuyển sang ViewModel (Tính toán số liệu)
            var vm = new PhotographerViewModel();
            vm.User = user;

            // 1. Tính kinh nghiệm & Số lượng
            vm.NamKinhNghiem = user.SoNamKinhNghiem ?? 0;
            vm.PackageCount = user.GoiDichVus.Count;

            // 2. Tính điểm đánh giá trung bình
            // Gom tất cả đơn hàng từ tất cả các gói
            var allBookings = user.GoiDichVus.SelectMany(g => g.DonDatLiches);
            // Lọc ra các đơn có đánh giá
            var ratings = allBookings.Where(d => d.DanhGium != null).Select(d => d.DanhGium.SoSao);

            vm.AvgRating = ratings.Any() ? ratings.Average() : 0;
            vm.ReviewCount = ratings.Count();

            return View(vm);
        }
    }
}
