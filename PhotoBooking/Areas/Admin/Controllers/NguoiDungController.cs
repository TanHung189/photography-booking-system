using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using X.PagedList.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NguoiDungController : Controller
    {
        private readonly PhotoBookingContext _context;

        public NguoiDungController(PhotoBookingContext context)
        {
            _context = context;
        }

        // GET: Admin/NguoiDung
        public async Task<IActionResult> Index()
        {
            var photoBookingContext = _context.NguoiDungs.Include(n => n.MaDiaDiemNavigation);
            return View(await photoBookingContext.ToListAsync());
        }

        // GET: Admin/NguoiDung/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nguoiDung = await _context.NguoiDungs
                .Include(n => n.MaDiaDiemNavigation)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            return View(nguoiDung);
        }

        // GET: Admin/NguoiDung/Create
        // 2. TẠO NGƯỜI DÙNG MỚI (GET)
        public IActionResult Create()
        {
            // Tạo danh sách chọn Vai trò
            var roles = new List<string> { "Admin", "Photographer", "Customer" };
            ViewBag.RoleList = new SelectList(roles);

            // Tạo danh sách chọn Địa điểm
            ViewData["MaDiaDiem"] = new SelectList(_context.DiaDiems, "MaDiaDiem", "TenThanhPho");

            return View();
        }

        // POST: Admin/NguoiDung/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNguoiDung,TenDangNhap,MatKhau,HoVaTen,Email,SoDienThoai,VaiTro,AnhDaiDien,AnhBia,GioiThieu,MaDiaDiem,NgayTao")] NguoiDung nguoiDung)
        {
            if (ModelState.IsValid)
            {
                nguoiDung.MatKhau = BCrypt.Net.BCrypt.HashPassword(nguoiDung.MatKhau);

                _context.Add(nguoiDung);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaDiaDiem"] = new SelectList(_context.DiaDiems, "MaDiaDiem", "MaDiaDiem", nguoiDung.MaDiaDiem);
            return View(nguoiDung);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null) return NotFound();

            // Tạo danh sách chọn Vai trò
            var roles = new List<string> { "Admin", "Photographer", "Customer" };
            ViewBag.RoleList = new SelectList(roles, user.VaiTro);

            // Danh sách địa điểm
            ViewData["MaDiaDiem"] = new SelectList(_context.DiaDiems, "MaDiaDiem", "TenThanhPho", user.MaDiaDiem);

            return View(user);
        }

        // POST: Admin/NguoiDung/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NguoiDung nguoiDung)
        {
            if (id != nguoiDung.MaNguoiDung) return NotFound();

            // --- KIỂM TRA TRÙNG EMAIL (LOGIC MỚI) ---
            // Tìm xem có ai KHÁC (id != nguoiDung.MaNguoiDung) mà đang dùng email này không
            bool isEmailExists = await _context.NguoiDungs.AnyAsync(u => u.Email == nguoiDung.Email && u.MaNguoiDung != id);

            if (isEmailExists)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi người khác!");
            }
            // -----------------------------------------

            // Bỏ qua validate các bảng liên quan (Giữ nguyên code cũ)
            ModelState.Remove("MaDiaDiemNavigation");
            ModelState.Remove("MatKhau");

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy dữ liệu cũ từ DB để giữ lại Mật khẩu cũ nếu admin không nhập mới
                    var userCu = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(u => u.MaNguoiDung == id);

                    // Logic: Nếu ô mật khẩu bỏ trống -> Giữ mật khẩu cũ. Nếu nhập -> Mã hóa mới.
                    if (string.IsNullOrEmpty(nguoiDung.MatKhau))
                    {
                        nguoiDung.MatKhau = userCu.MatKhau;
                    }
                    else
                    {
                        nguoiDung.MatKhau = BCrypt.Net.BCrypt.HashPassword(nguoiDung.MatKhau);
                    }

                    nguoiDung.MatKhau = string.IsNullOrEmpty(nguoiDung.MatKhau) ? userCu.MatKhau : BCrypt.Net.BCrypt.HashPassword(nguoiDung.MatKhau);
                    nguoiDung.NgayTao = userCu.NgayTao;
                    if (string.IsNullOrEmpty(nguoiDung.AnhDaiDien)) nguoiDung.AnhDaiDien = userCu.AnhDaiDien;

                    _context.Update(nguoiDung);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.NguoiDungs.Any(e => e.MaNguoiDung == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Nạp lại View nếu lỗi
            var roles = new List<string> { "Admin", "Photographer", "Customer" };
            ViewBag.RoleList = new SelectList(roles, nguoiDung.VaiTro);
            ViewData["MaDiaDiem"] = new SelectList(_context.DiaDiems, "MaDiaDiem", "TenThanhPho", nguoiDung.MaDiaDiem);
            return View(nguoiDung);
        }

        // GET: Admin/NguoiDung/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nguoiDung = await _context.NguoiDungs
                .Include(n => n.MaDiaDiemNavigation)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            return View(nguoiDung);
        }

        // POST: Admin/NguoiDung/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung != null)
            {
                _context.NguoiDungs.Remove(nguoiDung);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NguoiDungExists(int id)
        {
            return _context.NguoiDungs.Any(e => e.MaNguoiDung == id);
        }
    }
}
