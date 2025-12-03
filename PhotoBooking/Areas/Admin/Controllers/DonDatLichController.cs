using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; // thư viện xuất báo cáo excel
using OfficeOpenXml.Style;
using PhotoBooking.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using X.PagedList.Extensions;// dùng để phân trang

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
        public async Task<IActionResult> Index(int? page, string searchString)
        {
            // 1. Lấy thông tin người dùng (như cũ)
            var userIdStr = User.FindFirst("UserId")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            // 2. Query cơ bản
            var query = _context.DonDatLiches
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaGoiNavigation)
                .AsQueryable();

            // 3. Phân quyền (như cũ)
            if (userRole == "Photographer")
            {
                query = query.Where(d => d.MaGoiNavigation.MaNhiepAnhGia == userId);
            }

            // --- 4. XỬ LÝ TÌM KIẾM (Mới) ---
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm theo Tên khách hàng HOẶC Mã đơn
                query = query.Where(d => d.MaKhachHangNavigation.HoVaTen.Contains(searchString)
                                      || d.MaDon.ToString() == searchString);

                // Lưu lại từ khóa để hiện lại trên ô tìm kiếm
                ViewData["CurrentFilter"] = searchString;
            }

            // 5. Sắp xếp: Mới nhất lên đầu
            query = query.OrderByDescending(d => d.NgayTao);

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

        // ==========================================
        // ACTION: XUẤT EXCEL
        // ==========================================
        public async Task<IActionResult> ExportExcel()
        {
            // 1. Lấy dữ liệu (Logic giống hệt hàm Index)
            var userIdStr = User.FindFirst("UserId")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            var query = _context.DonDatLiches
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaGoiNavigation)
                .AsQueryable();

            if (userRole == "Photographer")
            {
                query = query.Where(d => d.MaGoiNavigation.MaNhiepAnhGia == userId);
            }

            var listDon = await query.OrderByDescending(d => d.NgayTao).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Tạo Sheet mới
                var worksheet = package.Workbook.Worksheets.Add("Danh sách đơn hàng");

                // --- TẠO HEADER (DÒNG 1) ---
                worksheet.Cells[1, 1].Value = "Mã Đơn";
                worksheet.Cells[1, 2].Value = "Khách Hàng";
                worksheet.Cells[1, 3].Value = "SĐT";
                worksheet.Cells[1, 4].Value = "Gói Dịch Vụ";
                worksheet.Cells[1, 5].Value = "Ngày Chụp";
                worksheet.Cells[1, 6].Value = "Tổng Tiền";
                worksheet.Cells[1, 7].Value = "Trạng Thái";

                // Style cho Header (In đậm, nền xanh nhạt, căn giữa)
                using (var range = worksheet.Cells["A1:G1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // --- ĐỔ DỮ LIỆU VÀO CÁC DÒNG TIẾP THEO ---
                int row = 2;
                foreach (var item in listDon)
                {
                    worksheet.Cells[row, 1].Value = item.MaDon;
                    worksheet.Cells[row, 2].Value = item.MaKhachHangNavigation?.HoVaTen;
                    worksheet.Cells[row, 3].Value = item.MaKhachHangNavigation?.SoDienThoai;
                    worksheet.Cells[row, 4].Value = item.MaGoiNavigation?.TenGoi ?? "Đặt riêng";
                    worksheet.Cells[row, 5].Value = item.NgayChup.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[row, 6].Value = item.TongTien;

                    // Chuyển trạng thái số sang chữ
                    string trangThaiStr = "";
                    switch (item.TrangThai)
                    {
                        case 0: trangThaiStr = "Chờ duyệt"; break;
                        case 1: trangThaiStr = "Đã duyệt"; break;
                        case 2: trangThaiStr = "Hoàn thành"; break;
                        case 3: trangThaiStr = "Đã hủy"; break;
                    }
                    worksheet.Cells[row, 7].Value = trangThaiStr;

                    row++;
                }

                // Tự động giãn cột cho đẹp
                worksheet.Cells.AutoFitColumns();

                // 3. Xuất file ra trình duyệt (Download)
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"DonHang_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}
