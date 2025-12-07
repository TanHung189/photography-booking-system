using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using X.PagedList; // Thư viện phân trang chính
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using X.PagedList.Extensions;

namespace PhotoBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được vào đây
    public class NguoiDungController : Controller
    {
        private readonly PhotoBookingContext _context;

        public NguoiDungController(PhotoBookingContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH + TÌM KIẾM + PHÂN TRANG
        // ==========================================
        public IActionResult Index(string searchString, string userRole, int? page)
        {
            // 1. Khởi tạo truy vấn
            var users = _context.NguoiDungs
                .Include(u => u.MaDiaDiemNavigation) // Kèm địa điểm để hiển thị
                .AsQueryable();

            // 2. Tìm kiếm (Tên hoặc Email)
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim(); // Xóa khoảng trắng thừa
                users = users.Where(u => u.HoVaTen.Contains(searchString)
                                      || u.Email.Contains(searchString)
                                      || u.TenDangNhap.Contains(searchString));
                ViewBag.CurrentFilter = searchString;
            }

            // 3. Lọc theo Vai trò
            if (!string.IsNullOrEmpty(userRole))
            {
                users = users.Where(u => u.VaiTro == userRole);
                ViewBag.CurrentRole = userRole;
            }

            // 4. Sắp xếp: Mới nhất lên đầu
            users = users.OrderByDescending(u => u.NgayTao);

            // 5. Phân trang
            int pageSize = 10; // 10 người/trang
            int pageNumber = (page ?? 1);

            // Lưu ý: Dùng ToPagedList (Sync) để tránh lỗi xung đột DataReader
            var pagedList = users.ToList().ToPagedList(pageNumber, pageSize);

            return View(pagedList);
        }

        // ==========================================
        // 2. TẠO MỚI (GET)
        // ==========================================
        public IActionResult Create()
        {
            ViewBag.RoleList = new SelectList(new[] { "Admin", "Photographer", "Customer" });
            ViewData["MaDiaDiem"] = new SelectList(_context.DiaDiems, "MaDiaDiem", "TenThanhPho");
            return View();
        }

        // ==========================================
        // 3. TẠO MỚI (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NguoiDung nguoiDung)
        {
            // Kiểm tra trùng tên đăng nhập
            if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == nguoiDung.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại!");
            }

            // Kiểm tra trùng Email
            if (await _context.NguoiDungs.AnyAsync(u => u.Email == nguoiDung.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng!");
            }

            // Bỏ qua validate các bảng không cần thiết
            ModelState.Remove("MaDiaDiemNavigation");
            ModelState.Remove("DonDatLichMaKhachHangNavigations");
            ModelState.Remove("DonDatLichMaNhiepAnhGiaNavigations");
            // ... các navigation khác nếu có

            if (ModelState.IsValid)
            {
                // Mã hóa mật khẩu
                nguoiDung.MatKhau = BCrypt.Net.BCrypt.HashPassword(nguoiDung.MatKhau);

                // Gán dữ liệu mặc định
                nguoiDung.NgayTao = System.DateTime.Now;
                if (string.IsNullOrEmpty(nguoiDung.AnhDaiDien))
                {
                    nguoiDung.AnhDaiDien = $"https://ui-avatars.com/api/?name={nguoiDung.HoVaTen}&background=random";
                }

                _context.Add(nguoiDung);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm người dùng thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Load lại dropdown nếu lỗi
            ViewBag.RoleList = new SelectList(new[] { "Admin", "Photographer", "Customer" }, nguoiDung.VaiTro);
            ViewData["MaDiaDiem"] = new SelectList(_context.DiaDiems, "MaDiaDiem", "TenThanhPho", nguoiDung.MaDiaDiem);
            return View(nguoiDung);
        }

        // ==========================================
        // 4. SỬA (GET)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.RoleList = new SelectList(new[] { "Admin", "Photographer", "Customer" }, user.VaiTro);
            ViewData["MaDiaDiem"] = new SelectList(_context.DiaDiems, "MaDiaDiem", "TenThanhPho", user.MaDiaDiem);
            return View(user);
        }

        // ==========================================
        // 5. SỬA (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NguoiDung nguoiDung)
        {
            if (id != nguoiDung.MaNguoiDung) return NotFound();

            // Kiểm tra trùng Email (trừ chính mình ra)
            if (await _context.NguoiDungs.AnyAsync(u => u.Email == nguoiDung.Email && u.MaNguoiDung != id))
            {
                ModelState.AddModelError("Email", "Email này đã thuộc về người khác!");
            }

            // Bỏ qua validate mật khẩu (vì nếu để trống nghĩa là không đổi)
            ModelState.Remove("MatKhau");
            ModelState.Remove("MaDiaDiemNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy dữ liệu cũ để đối chiếu
                    var userInDb = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(u => u.MaNguoiDung == id);

                    // Logic Mật khẩu:
                    // - Nếu ô mật khẩu TRỐNG -> Giữ nguyên mật khẩu cũ
                    // - Nếu có nhập -> Mã hóa mật khẩu mới
                    if (string.IsNullOrEmpty(nguoiDung.MatKhau))
                    {
                        nguoiDung.MatKhau = userInDb.MatKhau;
                    }
                    else
                    {
                        nguoiDung.MatKhau = BCrypt.Net.BCrypt.HashPassword(nguoiDung.MatKhau);
                    }

                    // Giữ nguyên các trường không có trong form edit
                    nguoiDung.NgayTao = userInDb.NgayTao;
                    nguoiDung.TenDangNhap = userInDb.TenDangNhap; // Không cho sửa tên đăng nhập
                    if (string.IsNullOrEmpty(nguoiDung.AnhDaiDien)) nguoiDung.AnhDaiDien = userInDb.AnhDaiDien;

                    _context.Update(nguoiDung);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thông tin thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.NguoiDungs.Any(e => e.MaNguoiDung == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoleList = new SelectList(new[] { "Admin", "Photographer", "Customer" }, nguoiDung.VaiTro);
            ViewData["MaDiaDiem"] = new SelectList(_context.DiaDiems, "MaDiaDiem", "TenThanhPho", nguoiDung.MaDiaDiem);
            return View(nguoiDung);
        }

        // ==========================================
        // 6. XÓA (POST - Xử lý trực tiếp)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user != null)
            {
                // Logic thực tế: Nên kiểm tra xem user có đơn hàng không trước khi xóa
                // Nếu có -> Chỉ khóa tài khoản (Active = false)
                // Ở đây xóa cứng theo yêu cầu đồ án
                _context.NguoiDungs.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa tài khoản vĩnh viễn.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 7. RESET MẬT KHẨU (Action Mới)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user != null)
            {
                // Đặt về mật khẩu mặc định "123456"
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword("123456");
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã đặt lại mật khẩu cho {user.HoVaTen} thành '123456'";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}