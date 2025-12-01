using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using PhotoBooking.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PhotoBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Photographer")]
    public class GoiDichVuController : Controller
    {
        private readonly PhotoBookingContext _context;
        private readonly PhotoService _photoService;

        public GoiDichVuController(PhotoBookingContext context, PhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        // GET: Admin/GoiDichVu
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            var query = _context.GoiDichVus
                .Include(g => g.MaDanhMucNavigation)
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .AsQueryable();

            // Nếu là Photographer, lọc theo ID của họ
            if (userRole == "Photographer")
            {
                query = query.Where(g => g.MaNhiepAnhGia == userId);
            }

            return View(await query.ToListAsync());
        }

        // GET: Admin/GoiDichVu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var goiDichVu = await _context.GoiDichVus
                .Include(g => g.MaDanhMucNavigation)
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaGoi == id);
            if (goiDichVu == null)
            {
                return NotFound();
            }

            return View(goiDichVu);
        }

        // GET: Admin/GoiDichVu/Create
        public IActionResult Create()
        {
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "MaDanhMuc");
            return View();
        }

        // POST: Admin/GoiDichVu/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GoiDichVu goiDichVu, IFormFile imageFile)
        {
            // 1. Tự động lấy ID người đang đăng nhập gán vào
            var userIdStr = User.FindFirst("UserId")?.Value;
            goiDichVu.MaNhiepAnhGia = int.Parse(userIdStr);

            // 2. QUAN TRỌNG: Bỏ qua lỗi check các bảng liên quan
            // Nếu không có 2 dòng này,ModelState.IsValid luôn luôn False -> Không lưu được
            ModelState.Remove("MaNhiepAnhGiaNavigation");
            ModelState.Remove("MaDanhMucNavigation");

            if (imageFile != null)
            {
                // Upload lên Cloudinary và lấy link về
                goiDichVu.AnhDaiDien = await _photoService.UploadPhotoAsync(imageFile);
            }
            // ------------------------

            if (ModelState.IsValid)
            {
                _context.Add(goiDichVu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc", goiDichVu.MaDanhMuc);
            return View(goiDichVu);
        }

        // GET: Admin/GoiDichVu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var goiDichVu = await _context.GoiDichVus.FindAsync(id);
            if (goiDichVu == null)
            {
                return NotFound();
            }
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "MaDanhMuc", goiDichVu.MaDanhMuc);
            ViewData["MaNhiepAnhGia"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", goiDichVu.MaNhiepAnhGia);
            return View(goiDichVu);
        }

        // POST: Admin/GoiDichVu/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaGoi,TenGoi,GiaTien,GiaCoc,ThoiLuong,SoNguoiToiDa,MoTaChiTiet,SanPhamBanGiao,AnhDaiDien,MaDanhMuc,MaNhiepAnhGia")] GoiDichVu goiDichVu)
        {
            if (id != goiDichVu.MaGoi)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(goiDichVu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GoiDichVuExists(goiDichVu.MaGoi))
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
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "MaDanhMuc", goiDichVu.MaDanhMuc);
            ViewData["MaNhiepAnhGia"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", goiDichVu.MaNhiepAnhGia);
            return View(goiDichVu);
        }

        // GET: Admin/GoiDichVu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var goiDichVu = await _context.GoiDichVus
                .Include(g => g.MaDanhMucNavigation)
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaGoi == id);
            if (goiDichVu == null)
            {
                return NotFound();
            }

            return View(goiDichVu);
        }

        // POST: Admin/GoiDichVu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var goiDichVu = await _context.GoiDichVus.FindAsync(id);
            if (goiDichVu != null)
            {
                _context.GoiDichVus.Remove(goiDichVu);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GoiDichVuExists(int id)
        {
            return _context.GoiDichVus.Any(e => e.MaGoi == id);
        }
    }
}
