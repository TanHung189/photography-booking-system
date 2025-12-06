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
using X.PagedList.Extensions;

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
        public async Task<IActionResult> Index(int? page)
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

            // --- 6. PHÂN TRANG (Mới) ---
            int pageSize = 10; // Số dòng trên 1 trang
            int pageNumber = (page ?? 1); // Nếu không có trang thì mặc định trang 1

            // Chuyển đổi sang danh sách phân trang (ToPagedList)
            // Lưu ý: X.PagedList hoạt động tốt nhất với dữ liệu đã tải về hoặc IQueryable
            // Để đơn giản và tránh lỗi async, ta ToList trước rồi mới phân trang
            var listData = await query.ToListAsync();
            var pagedList = listData.ToPagedList(pageNumber, pageSize);

            return View(pagedList);
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
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc");
            return View();
        }

        // POST: Admin/GoiDichVu/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Admin/GoiDichVu/Create
        // POST: Admin/GoiDichVu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 1. SỬA DÒNG NÀY: Thêm dấu ? vào sau string imageUrl và IFormFile imageFile
        public async Task<IActionResult> Create(GoiDichVu goiDichVu, IFormFile? imageFile, string? imageUrl)
        {
            // Lấy ID người dùng
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account", new { area = "" });

            goiDichVu.MaNhiepAnhGia = int.Parse(userIdStr);

            // Xử lý ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                goiDichVu.AnhDaiDien = await _photoService.UploadPhotoAsync(imageFile);
            }
            else if (!string.IsNullOrEmpty(imageUrl))
            {
                goiDichVu.AnhDaiDien = imageUrl;
            }
            else
            {
                goiDichVu.AnhDaiDien = "https://placehold.co/400x300?text=No+Image";
            }

            // --- QUAN TRỌNG: XÓA HẾT CÁC LỖI VALIDATION THỪA ---

            // 1. Xóa lỗi của tham số imageUrl (Cái bạn đang bị lỗi)
            ModelState.Remove("imageUrl");
            ModelState.Remove("imageFile");

            // 2. Xóa lỗi của Model chính
            ModelState.Remove("AnhDaiDien");
            ModelState.Remove("MaNhiepAnhGiaNavigation");
            ModelState.Remove("MaDanhMucNavigation");

            // 3. Xóa lỗi các bảng con (Rất hay bị dính)
            ModelState.Remove("DonDatLiches");
            ModelState.Remove("DonDatLichs");
            ModelState.Remove("DonDatLich");
            ModelState.Remove("DanhGias");

            if (ModelState.IsValid)
            {
                _context.Add(goiDichVu);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo gói dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Debug lỗi ra cửa sổ Output nếu vẫn không lưu được
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var err in errors)
            {
                System.Diagnostics.Debug.WriteLine(">>> LỖI: " + err.ErrorMessage);
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
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc", goiDichVu.MaDanhMuc);
            ViewData["MaNhiepAnhGia"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", goiDichVu.MaNhiepAnhGia);
            return View(goiDichVu);
        }

        // POST: Admin/GoiDichVu/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ⚠️ QUAN TRỌNG: Phải có tham số 'string imageUrl' để nhận link từ ô nhập
        public async Task<IActionResult> Edit(int id, GoiDichVu goiDichVu, IFormFile imageFile, string imageUrl)
        {
            if (id != goiDichVu.MaGoi) return NotFound();

            // 1. Lấy dữ liệu gốc từ DB ra
            var serviceInDb = await _context.GoiDichVus.FindAsync(id);
            if (serviceInDb == null) return NotFound();

            // 2. Cập nhật thông tin chữ
            serviceInDb.TenGoi = goiDichVu.TenGoi;
            serviceInDb.MaDanhMuc = goiDichVu.MaDanhMuc;
            serviceInDb.GiaTien = goiDichVu.GiaTien;
            serviceInDb.GiaCoc = goiDichVu.GiaCoc;
            serviceInDb.ThoiLuong = goiDichVu.ThoiLuong;
            serviceInDb.SoNguoiToiDa = goiDichVu.SoNguoiToiDa;
            serviceInDb.MoTaChiTiet = goiDichVu.MoTaChiTiet;
            serviceInDb.SanPhamBanGiao = goiDichVu.SanPhamBanGiao;

            // 3. --- XỬ LÝ ẢNH (LOGIC MỚI) ---

            // ƯU TIÊN 1: Nếu người dùng chọn FILE từ máy
            if (imageFile != null && imageFile.Length > 0)
            {
                // Upload lên Cloudinary và lấy link mới
                serviceInDb.AnhDaiDien = await _photoService.UploadPhotoAsync(imageFile);
            }
            // ƯU TIÊN 2: Nếu không có File, nhưng có nhập LINK URL
            else if (!string.IsNullOrEmpty(imageUrl))
            {
                // Gán trực tiếp link người dùng nhập vào Database
                // (Bất kể link đó là cloudinary, unsplash hay google...)
                serviceInDb.AnhDaiDien = imageUrl;
            }
         

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
            }

            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc", goiDichVu.MaDanhMuc);
            return View(goiDichVu);
        }


        // ==========================================
        // 1. TRANG XÁC NHẬN XÓA (GET)
        // ==========================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var goiDichVu = await _context.GoiDichVus
                .Include(g => g.MaDanhMucNavigation)
                .Include(g => g.MaNhiepAnhGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaGoi == id);

            if (goiDichVu == null) return NotFound();

            // Kiểm tra quyền: Chỉ được xóa gói của chính mình (hoặc Admin)
            var userId = int.Parse(User.FindFirst("UserId").Value);
            if (!User.IsInRole("Admin") && goiDichVu.MaNhiepAnhGia != userId)
            {
                return Forbid();
            }

            return View(goiDichVu);
        }

        // ==========================================
        // 2. THỰC HIỆN XÓA (POST)
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var goiDichVu = await _context.GoiDichVus.FindAsync(id);
            if (goiDichVu == null) return RedirectToAction(nameof(Index));

            try
            {
                _context.GoiDichVus.Remove(goiDichVu);
                await _context.SaveChangesAsync();

                // (Tùy chọn) Nếu muốn xóa luôn ảnh trên Cloudinary cho sạch thì gọi Service ở đây
                // Nhưng tạm thời cứ để đó cũng không sao.

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
             
                TempData["Error"] = "Không thể xóa gói này vì đã có khách hàng đặt lịch! Hãy hủy hoặc hoàn thành các đơn hàng liên quan trước.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool GoiDichVuExists(int id)
        {
            return _context.GoiDichVus.Any(e => e.MaGoi == id);
        }
    }
}
