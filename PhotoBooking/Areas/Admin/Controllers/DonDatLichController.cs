using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PhotoBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Photographer")]
    public class DonDatLichController : Controller
    {
        private readonly PhotoBookingContext _context;

        public DonDatLichController(PhotoBookingContext context)
        {
            _context = context;
        }

        // GET: Admin/DonDatLich
        public async Task<IActionResult> Index()
        {
            // 1. Lấy thông tin người đang đăng nhập
            var userIdStr = User.FindFirst("UserId")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            // 2. Query cơ bản: Lấy đơn kèm thông tin Khách và Gói
            var query = _context.DonDatLiches
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaGoiNavigation)
                .AsQueryable();

            // 3. Phân quyền: Photographer chỉ xem đơn đặt gói của mình
            if (userRole == "Photographer")
            {
                query = query.Where(d => d.MaGoiNavigation.MaNhiepAnhGia == userId);
            }

            // 4. Sắp xếp đơn mới nhất lên đầu
            var listDon = await query.OrderByDescending(d => d.NgayTao).ToListAsync();

            return View(listDon);
        }

        // ACTION: DUYỆT ĐƠN
        public async Task<IActionResult> Approve(int id)
        {
            var don = await _context.DonDatLiches.FindAsync(id);
            if (don != null && don.TrangThai == 0) // Chỉ duyệt đơn đang chờ
            {
                don.TrangThai = 1; // 1 = Đã xác nhận
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã duyệt đơn hàng #" + id;
            }
            return RedirectToAction(nameof(Index));
        }

        // ACTION: TỪ CHỐI / HỦY ĐƠN
        public async Task<IActionResult> Reject(int id)
        {
            var don = await _context.DonDatLiches.FindAsync(id);
            if (don != null)
            {
                don.TrangThai = 3; // 3 = Đã hủy
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã từ chối đơn hàng #" + id;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/DonDatLich/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donDatLich = await _context.DonDatLiches
                .Include(d => d.MaGoiNavigation)
                .Include(d => d.MaKhachHangNavigation)
                .FirstOrDefaultAsync(m => m.MaDon == id);
            if (donDatLich == null)
            {
                return NotFound();
            }

            return View(donDatLich);
        }

        // GET: Admin/DonDatLich/Create
        public IActionResult Create()
        {
            ViewData["MaGoi"] = new SelectList(_context.GoiDichVus, "MaGoi", "MaGoi");
            ViewData["MaKhachHang"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung");
            return View();
        }

        // POST: Admin/DonDatLich/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDon,NgayChup,TongTien,TienDaCoc,GhiChu,DiaChiChup,TrangThai,TrangThaiThanhToan,NgayTao,MaKhachHang,MaGoi")] DonDatLich donDatLich)
        {
            if (ModelState.IsValid)
            {
                _context.Add(donDatLich);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaGoi"] = new SelectList(_context.GoiDichVus, "MaGoi", "MaGoi", donDatLich.MaGoi);
            ViewData["MaKhachHang"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", donDatLich.MaKhachHang);
            return View(donDatLich);
        }

        // GET: Admin/DonDatLich/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donDatLich = await _context.DonDatLiches.FindAsync(id);
            if (donDatLich == null)
            {
                return NotFound();
            }
            ViewData["MaGoi"] = new SelectList(_context.GoiDichVus, "MaGoi", "MaGoi", donDatLich.MaGoi);
            ViewData["MaKhachHang"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", donDatLich.MaKhachHang);
            return View(donDatLich);
        }

        // POST: Admin/DonDatLich/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDon,NgayChup,TongTien,TienDaCoc,GhiChu,DiaChiChup,TrangThai,TrangThaiThanhToan,NgayTao,MaKhachHang,MaGoi")] DonDatLich donDatLich)
        {
            if (id != donDatLich.MaDon)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(donDatLich);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonDatLichExists(donDatLich.MaDon))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaGoi"] = new SelectList(_context.GoiDichVus, "MaGoi", "MaGoi", donDatLich.MaGoi);
            ViewData["MaKhachHang"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", donDatLich.MaKhachHang);
            return View(donDatLich);
        }

        // GET: Admin/DonDatLich/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donDatLich = await _context.DonDatLiches
                .Include(d => d.MaGoiNavigation)
                .Include(d => d.MaKhachHangNavigation)
                .FirstOrDefaultAsync(m => m.MaDon == id);
            if (donDatLich == null)
            {
                return NotFound();
            }

            return View(donDatLich);
        }

        // POST: Admin/DonDatLich/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donDatLich = await _context.DonDatLiches.FindAsync(id);
            if (donDatLich != null)
            {
                _context.DonDatLiches.Remove(donDatLich);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DonDatLichExists(int id)
        {
            return _context.DonDatLiches.Any(e => e.MaDon == id);
        }
    }
}
