using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using PhotoBooking.ViewModels;
using System.Diagnostics;

namespace PhotoBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PhotoBookingContext _context;

        public HomeController(PhotoBookingContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }
      

        public IActionResult Index()
        {
            
            var viewModel = new HomeViewModel();

          

            
            viewModel.FeaturedPackages = _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation) // Kèm thông tin Photographer
                .Include(g => g.MaDanhMucNavigation)     // Kèm thông tin Danh m?c
                .OrderByDescending(g => g.MaGoi)         // M?i nh?t lên ??u
                .Take(6)                                 // Ch? l?y 6 cái
                .ToList();

           
            viewModel.Categories = _context.DanhMucs.ToList();

            
            viewModel.Locations = _context.DiaDiems.ToList();

           
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Search(int? locationId, int? categoryId)
        {
            var query = _context.GoiDichVus
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .Include(g => g.MaDanhMucNavigation)
                .AsQueryable();

            //vi?t b? l?c 
            if (locationId.HasValue)
            {
                query = query.Where(g => g.
                MaNhiepAnhGiaNavigation.MaDiaDiem == locationId.Value);
            }

            if(categoryId.HasValue)
            {
                query = query.Where(g => g.MaDanhMuc == categoryId.Value);
            }
            //th?c thi truy v?n và l?y k?t qu? thành danh ssach
            var results = query.OrderByDescending(g => g.MaGoi).ToList();
            return View(results);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
