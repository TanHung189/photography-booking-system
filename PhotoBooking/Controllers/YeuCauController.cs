using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBooking.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Cần thêm dòng này để dùng ClaimTypes

namespace PhotoBooking.Controllers
{
    // [Authorize] <-- Tạm thời bỏ ở class, vì Action JobMarket và Details ai cũng xem được
    public class YeuCauController : Controller
    {
        private readonly PhotoBookingContext _context;

        public YeuCauController(PhotoBookingContext context)
        {
            _context = context;
        }

        // ==========================================
        // HÀM PHỤ TRỢ: LẤY ID NGƯỜI DÙNG AN TOÀN
        // ==========================================
        // Hàm này giúp lấy ID mà không bao giờ bị crash lỗi FormatException
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId"); // Tìm đúng key "UserId" như bên AccountController
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0; // Trả về 0 nếu chưa đăng nhập hoặc lỗi
        }

        // ==========================================
        // 1. DANH SÁCH YÊU CẦU CỦA TÔI
        // ==========================================
        [Authorize]
        public async Task<IActionResult> MyRequests()
        {
            var userId = GetCurrentUserId(); // Dùng hàm an toàn
            var list = await _context.YeuCaus
                .Where(y => y.MaKhachHang == userId)
                .OrderByDescending(y => y.NgayTao)
                .ToListAsync();

            return View(list);
        }

        // ==========================================
        // 2. TẠO YÊU CẦU (GET + POST)
        // ==========================================
        [Authorize]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(YeuCau yeuCau)
        {
            var userId = GetCurrentUserId();
            yeuCau.MaKhachHang = userId;
            yeuCau.NgayTao = DateTime.Now;
            yeuCau.TrangThai = 0;

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
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var item = await _context.YeuCaus.FindAsync(id);

            if (item != null && item.MaKhachHang == userId)
            {
                _context.YeuCaus.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa tin đăng.";
            }
            return RedirectToAction(nameof(MyRequests));
        }

        // ==========================================
        // 4. CHỢ VIỆC LÀM (JOB MARKET)
        // ==========================================
        // Bỏ Authorize để khách vãng lai cũng xem được
        public async Task<IActionResult> JobMarket(string searchLocation, string sortOrder)
        {
            var query = _context.YeuCaus
                .Include(y => y.MaKhachHangNavigation)
                .Where(y => y.TrangThai == 0)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchLocation))
            {
                query = query.Where(y => y.DiaChi.Contains(searchLocation) || y.TieuDe.Contains(searchLocation));
                ViewBag.CurrentFilter = searchLocation;
            }

            switch (sortOrder)
            {
                case "price_desc": query = query.OrderByDescending(y => y.NganSach); break;
                default: query = query.OrderByDescending(y => y.NgayTao); break;
            }

            return View(await query.ToListAsync());
        }

        // ==========================================
        // 5. NHIẾP ẢNH GIA ỨNG TUYỂN
        // ==========================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Apply(int MaYeuCau, decimal GiaBao, string LoiNhan)
        {
            var userId = GetCurrentUserId();

            // Nếu user chưa đăng nhập hoặc lỗi ID -> Đá về login
            if (userId == 0) return RedirectToAction("Login", "Account");

            var exists = await _context.UngTuyens.AnyAsync(u => u.MaYeuCau == MaYeuCau && u.MaNhiepAnhGia == userId);
            if (exists)
            {
                TempData["Error"] = "Bạn đã gửi báo giá cho yêu cầu này rồi!";
                return RedirectToAction(nameof(Details), new { id = MaYeuCau }); // Quay lại trang chi tiết thay vì JobMarket
            }

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

            TempData["Success"] = "Đã gửi báo giá thành công!";
            return RedirectToAction(nameof(Details), new { id = MaYeuCau });
        }

        // ==========================================
        // 6. CHI TIẾT YÊU CẦU (ĐÃ SỬA LỖI CRASH)
        // ==========================================
        public async Task<IActionResult> Details(int id)
        {
            var yeuCau = await _context.YeuCaus
                .Include(y => y.UngTuyens)
                    .ThenInclude(ut => ut.MaNhiepAnhGiaNavigation)
                .Include(y => y.MaKhachHangNavigation)
                .FirstOrDefaultAsync(m => m.MaYeuCau == id);

            if (yeuCau == null) return NotFound();

            // --- ĐÂY LÀ ĐOẠN FIX LỖI ---
            // Dùng hàm GetCurrentUserId() an toàn thay vì int.Parse
            int currentUserId = GetCurrentUserId();

            if (currentUserId > 0)
            {
                // Tìm xem user hiện tại (nếu có) đã báo giá chưa
                var myBid = yeuCau.UngTuyens.FirstOrDefault(ut => ut.MaNhiepAnhGia == currentUserId);
                ViewBag.HoSoCuaToi = myBid;
            }

            return View(yeuCau);
        }

        // ==========================================
        // 7. CHẤP NHẬN BÁO GIÁ
        // ==========================================
        [HttpPost]
        [Authorize]
        public IActionResult AcceptBid(int idUngTuyen)
        {
            var ungTuyen = _context.UngTuyens
                           .Include(u => u.MaYeuCauNavigation)
                           .FirstOrDefault(u => u.MaUngTuyen == idUngTuyen);

            if (ungTuyen == null) return NotFound();

            if (ungTuyen.MaYeuCauNavigation.TrangThai == 1)
            {
                TempData["Error"] = "Yêu cầu này đã được chốt!";
                return RedirectToAction("Details", new { id = ungTuyen.MaYeuCau });
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    ungTuyen.TrangThai = 1; // Được chọn
                    ungTuyen.MaYeuCauNavigation.TrangThai = 1; // Đóng yêu cầu

                    // Tạo đơn đặt lịch
                    var donMoi = new DonDatLich
                    {
                        MaKhachHang = ungTuyen.MaYeuCauNavigation.MaKhachHang,
                        MaNhiepAnhGia = ungTuyen.MaNhiepAnhGia,
                        NgayChup = ungTuyen.MaYeuCauNavigation.NgayCanChup ?? DateTime.Now.AddDays(1),
                        TongTien = ungTuyen.GiaBao,
                        TienDaCoc = 0,
                        DiaChiChup = ungTuyen.MaYeuCauNavigation.DiaChi,
                        GhiChu = $"Từ yêu cầu: {ungTuyen.MaYeuCauNavigation.TieuDe}. Note: {ungTuyen.LoiNhan}",
                        TrangThai = 0,
                        TrangThaiThanhToan = 0,
                        NgayTao = DateTime.Now
                    };

                    // LƯU Ý: Tên bảng trong DBContext của bạn là DonDatLichs hay DonDatLiches?
                    // Hãy kiểm tra kỹ file Context. Ở đây tôi dùng DonDatLichs (chuẩn)
                    _context.DonDatLiches.Add(donMoi);
                    _context.SaveChanges();

                    transaction.Commit();

                    // Chuyển sang BookingController (Nếu chưa tạo thì tạo file này nhé)
                    return RedirectToAction("Payment", "Booking", new { id = donMoi.MaDon });
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return View("Error");
                }
            }
        }
    }
}