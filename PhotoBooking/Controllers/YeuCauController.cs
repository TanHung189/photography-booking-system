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

        // ==========================================
        // 5. NHIẾP ẢNH GIA GỬI BÁO GIÁ (POST)
        // ==========================================
        [HttpPost]
        [Authorize] // Ai đăng nhập cũng được (nhưng logic dưới sẽ check vai trò)
        public async Task<IActionResult> Apply(int MaYeuCau, decimal GiaBao, string LoiNhan)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            // 1. Kiểm tra xem đã ứng tuyển chưa (Tránh spam)
            var exists = await _context.UngTuyens.AnyAsync(u => u.MaYeuCau == MaYeuCau && u.MaNhiepAnhGia == userId);
            if (exists)
            {
                TempData["Error"] = "Bạn đã gửi báo giá cho yêu cầu này rồi!";
                return RedirectToAction(nameof(JobMarket));
            }

            // 2. Tạo đơn ứng tuyển
            var ungTuyen = new UngTuyen
            {
                MaYeuCau = MaYeuCau,
                MaNhiepAnhGia = userId,
                GiaBao = GiaBao,
                LoiNhan = LoiNhan,
                TrangThai = 0,
                NgayUngTuyen = DateTime.Now
            };

            _context.Add(ungTuyen);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi báo giá thành công! Chờ khách hàng phản hồi.";
            return RedirectToAction(nameof(JobMarket));
        }

        // ==========================================
        // 6. KHÁCH HÀNG XEM CHI TIẾT & CHỌN THỢ
        // ==========================================
        public async Task<IActionResult> Details(int id)
        {
            var yeuCau = await _context.YeuCaus
                .Include(y => y.UngTuyens) // Lấy danh sách người ứng tuyển
                    .ThenInclude(ut => ut.MaNhiepAnhGiaNavigation) // Lấy tên thợ
                .FirstOrDefaultAsync(m => m.MaYeuCau == id);

            if (yeuCau == null) return NotFound();

            return View(yeuCau);
        }

        // ==========================================
        // 7. KHÁCH HÀNG CHỐT THỢ (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> AcceptOffer(int id) // id là MaUngTuyen
        {
            // 1. Tìm đơn ứng tuyển
            var ungTuyen = await _context.UngTuyens
                .Include(u => u.MaYeuCauNavigation) // Lấy thông tin yêu cầu gốc
                .FirstOrDefaultAsync(u => u.MaUngTuyen == id);

            if (ungTuyen == null) return NotFound();

            // 2. Cập nhật trạng thái
            ungTuyen.TrangThai = 1; // Được chọn
            ungTuyen.MaYeuCauNavigation.TrangThai = 1; // Đóng yêu cầu (Đã có thợ)

            // 3. Tự động tạo Đơn Đặt Lịch (DonDatLich) chính thức
            var donMoi = new DonDatLich
            {
                MaKhachHang = ungTuyen.MaYeuCauNavigation.MaKhachHang,
                MaNhiepAnhGia = ungTuyen.MaNhiepAnhGia,
                MaGoi = null, // Không theo gói
                NgayChup = ungTuyen.MaYeuCauNavigation.NgayCanChup ?? DateTime.Now,
                DiaChiChup = ungTuyen.MaYeuCauNavigation.DiaChi,
                GhiChu = "Đơn hàng tạo từ yêu cầu tìm thợ: " + ungTuyen.MaYeuCauNavigation.TieuDe,

                TongTien = ungTuyen.GiaBao, // Lấy theo giá thợ báo
                TienDaCoc = 0,
                TrangThai = 0, // Chờ duyệt (hoặc có thể set thành 1 luôn nếu muốn)
                TrangThaiThanhToan = 0,
                NgayTao = DateTime.Now
            };

            _context.DonDatLiches.Add(donMoi);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã chốt thợ thành công! Đơn đặt lịch đã được tạo.";
            return RedirectToAction(nameof(MyRequests));
        }
    }
}
