using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Photographer")]
    public class AlbumAnhController : Controller
    {
        private readonly PhotoBookingContext _context;

        public AlbumAnhController(PhotoBookingContext context)
        {
            _context = context;
        }

        // GET: Admin/AlbumAnh
        public async Task<IActionResult> Index()
        {
            var photoBookingContext = _context.AlbumAnhs.Include(a => a.MaNhiepAnhGiaNavigation);
            return View(await photoBookingContext.ToListAsync());
        }

        // GET: Admin/AlbumAnh/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var albumAnh = await _context.AlbumAnhs
                .Include(a => a.MaNhiepAnhGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaAlbum == id);
            if (albumAnh == null)
            {
                return NotFound();
            }

            return View(albumAnh);
        }

        // GET: Admin/AlbumAnh/Create
        public IActionResult Create()
        {
            ViewData["MaNhiepAnhGia"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung");
            return View();
        }

        // POST: Admin/AlbumAnh/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaAlbum,TieuDe,MoTa,MaNhiepAnhGia")] AlbumAnh albumAnh)
        {
            if (ModelState.IsValid)
            {
                _context.Add(albumAnh);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaNhiepAnhGia"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", albumAnh.MaNhiepAnhGia);
            return View(albumAnh);
        }

        // GET: Admin/AlbumAnh/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var albumAnh = await _context.AlbumAnhs.FindAsync(id);
            if (albumAnh == null)
            {
                return NotFound();
            }
            ViewData["MaNhiepAnhGia"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", albumAnh.MaNhiepAnhGia);
            return View(albumAnh);
        }

        // POST: Admin/AlbumAnh/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaAlbum,TieuDe,MoTa,MaNhiepAnhGia")] AlbumAnh albumAnh)
        {
            if (id != albumAnh.MaAlbum)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(albumAnh);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AlbumAnhExists(albumAnh.MaAlbum))
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
            ViewData["MaNhiepAnhGia"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", albumAnh.MaNhiepAnhGia);
            return View(albumAnh);
        }

        // GET: Admin/AlbumAnh/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var albumAnh = await _context.AlbumAnhs
                .Include(a => a.MaNhiepAnhGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaAlbum == id);
            if (albumAnh == null)
            {
                return NotFound();
            }

            return View(albumAnh);
        }

        // POST: Admin/AlbumAnh/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var albumAnh = await _context.AlbumAnhs.FindAsync(id);
            if (albumAnh != null)
            {
                _context.AlbumAnhs.Remove(albumAnh);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AlbumAnhExists(int id)
        {
            return _context.AlbumAnhs.Any(e => e.MaAlbum == id);
        }
    }
}
