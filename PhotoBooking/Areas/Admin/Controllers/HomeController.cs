using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhotoBooking.Models;
using System.Security.Claims;
using X.PagedList.Extensions;

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
        public IActionResult Index(int? page)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            var query = _context.DonDatLiches
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaGoiNavigation)
                .Include(d => d.MaNhiepAnhGiaNavigation)
                .AsQueryable();

            if (userRole == "Photographer")
            {
                query = query.Where(d => d.MaNhiepAnhGia == userId);
            }

            // 1. LẤY TOÀN BỘ DANH SÁCH (Để tính thống kê & Biểu đồ)
            // Bắt buộc phải lấy hết thì biểu đồ mới đúng được
            var fullList = query.OrderByDescending(d => d.NgayTao).ToList();

            // --- PHẦN THỐNG KÊ (Dùng fullList) ---
            ViewBag.TongDonHang = fullList.Count;
            ViewBag.DonChoDuyet = fullList.Count(d => d.TrangThai == 0);
            ViewBag.TongDoanhThu = fullList.Where(d => d.TrangThai == 2).Sum(d => d.TongTien ?? 0);

            if (userRole == "Admin")
            {
                ViewBag.TongNguoiDung = _context.NguoiDungs.Count(u => u.VaiTro == "Customer");
                ViewBag.TongThoAnh = _context.NguoiDungs.Count(u => u.VaiTro == "Photographer");
            }

            // --- PHẦN BIỂU ĐỒ (Dùng fullList) ---
            int[] pieData = new int[] {
                fullList.Count(x => x.TrangThai == 0),
                fullList.Count(x => x.TrangThai == 1),
                fullList.Count(x => x.TrangThai == 2),
                fullList.Count(x => x.TrangThai == 3)
            };
            ViewBag.PieData = JsonConvert.SerializeObject(pieData);

            var currentYear = DateTime.Now.Year;
            decimal[] revenueData = new decimal[12];
            var donNamNay = fullList
                .Where(d => d.TrangThai == 2 && d.NgayTao.HasValue && d.NgayTao.Value.Year == currentYear)
                .ToList();

            foreach (var don in donNamNay)
            {
                int monthIndex = don.NgayTao.Value.Month - 1;
                revenueData[monthIndex] += (don.TongTien ?? 0);
            }
            ViewBag.RevenueData = JsonConvert.SerializeObject(revenueData);

            // 2. PHÂN TRANG CHO BẢNG (Chỉ cắt data để hiển thị bảng)
            int pageSize = 10; // Số dòng mỗi trang
            int pageNumber = (page ?? 1);

            // Chuyển fullList thành PagedList
            var pagedModel = fullList.ToPagedList(pageNumber, pageSize);

            // Trả về pagedModel thay vì list thường
            return View(pagedModel);
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