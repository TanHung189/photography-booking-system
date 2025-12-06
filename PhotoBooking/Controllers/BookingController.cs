using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;

namespace PhotoBooking.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly PhotoBookingContext _context;

        public BookingController(PhotoBookingContext context)
        {
            _context = context;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Payment(int id)
        {
            // lấy thông tin đơn hàng theo mã , kèm theo thông tin nhiếp ảnh gia và gói nếu có 
            var donHang = _context.DonDatLiches
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaNhiepAnhGiaNavigation)
                .Include(d => d.MaGoiNavigation)
                .FirstOrDefault(d => d.MaDon == id);

            if (donHang == null) return NotFound();

            var userId = int.Parse(User.FindFirst("UserId").Value);
            if(donHang.MaKhachHang != userId && !User.IsInRole("Admin"))
            {
                return Forbid(); // trả về mã lỗi 403 forbidden
            }

            // nếu đơn hàng đã thanh toán thành công thì về trang chủ không cho thanh toán nữa

            if(donHang.TrangThaiThanhToan > 0)
            {
                return RedirectToAction("MyBookings", "Home");

            }



            return View(donHang);
        }

        // xác nhận đã chuyển khoản 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var donHang = await _context.DonDatLiches.FindAsync(id);
            if (donHang == null) return NotFound();

            // Cập nhật trạng thái
            donHang.TrangThaiThanhToan = 1; // 1 = Đã cọc (Chờ Admin duyệt)
            donHang.TrangThai = 1;          // 1 = Đã xác nhận (Lịch đã được chốt)

            _context.Update(donHang);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xác nhận thanh toán thành công! Nhiếp ảnh gia sẽ liên hệ với bạn sớm.";

            // Chuyển hướng về trang Quản lý đơn hàng của khách
            return RedirectToAction("MyBookings", "Home");
        }
    }
}
