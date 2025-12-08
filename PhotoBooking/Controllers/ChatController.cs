using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBooking.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoBooking.Controllers
{
    [Authorize] // Phải đăng nhập mới được chat
    public class ChatController : Controller
    {
        private readonly PhotoBookingContext _context;
        public ChatController(PhotoBookingContext context) { _context = context; }

        public async Task<IActionResult> Index(int? userId)
        {
            // Lấy ID người đang đăng nhập
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int currentUserId = int.Parse(userIdClaim.Value);

            // 1. Lấy danh sách những người đã từng nhắn tin
            var listUserIds = await _context.TinNhans
                .Where(m => m.NguoiGuiId == currentUserId || m.NguoiNhanId == currentUserId)
                .Select(m => m.NguoiGuiId == currentUserId ? m.NguoiNhanId : m.NguoiGuiId)
                .Distinct()
                .ToListAsync();

            // Nếu người dùng click nút "Nhắn tin" từ trang Profile, thêm người đó vào list
            if (userId.HasValue && !listUserIds.Contains(userId.Value))
            {
                listUserIds.Add(userId.Value);
            }

            var listUsers = await _context.NguoiDungs
                .Where(u => listUserIds.Contains(u.MaNguoiDung))
                .ToListAsync();

            ViewBag.ListUsers = listUsers;
            ViewBag.CurrentUserId = currentUserId;

            // 2. Load nội dung chat với người được chọn
            if (userId.HasValue)
            {
                var messages = await _context.TinNhans
                    .Where(m => (m.NguoiGuiId == currentUserId && m.NguoiNhanId == userId)
                             || (m.NguoiGuiId == userId && m.NguoiNhanId == currentUserId))
                    .OrderBy(m => m.ThoiGianGui)
                    .ToListAsync();

                ViewBag.Receiver = await _context.NguoiDungs.FindAsync(userId);
                return View(messages);
            }

            return View(new List<TinNhan>());
        }

        // Thêm hàm này vào ChatController
        [HttpGet]
        public async Task<IActionResult> GetConversation(int otherUserId)
        {
            var currentUserIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(currentUserIdStr)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdStr);

            // Lấy tin nhắn
            var messages = await _context.TinNhans
                .Where(m => (m.NguoiGuiId == currentUserId && m.NguoiNhanId == otherUserId)
                         || (m.NguoiGuiId == otherUserId && m.NguoiNhanId == currentUserId))
                .OrderBy(m => m.ThoiGianGui)
                .Select(m => new {
                    m.MaTinNhan,
                    m.NguoiGuiId,
                    m.NoiDung,
                    ThoiGianGui = m.ThoiGianGui.ToString("HH:mm"),
                    m.IsDeleted // Trả về trạng thái xóa
                })
                .ToListAsync();

            // Lấy info người kia để hiển thị lên Header
            var otherUser = await _context.NguoiDungs
                .Where(u => u.MaNguoiDung == otherUserId)
                .Select(u => new { u.MaNguoiDung, u.HoVaTen, u.AnhDaiDien })
                .FirstOrDefaultAsync();

            return Json(new { messages, otherUser });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Ok(0);
            int userId = int.Parse(userIdStr);

            // SỬA LỖI TẠI ĐÂY:
            // Thay "!m.DaXem" thành "m.DaXem == false"
            // Điều này có nghĩa: Chỉ đếm khi DaXem là False (chưa xem). Nếu Null cũng coi như chưa xem.
            int count = await _context.TinNhans
                .Where(m => m.NguoiNhanId == userId
                         && m.DaXem == false // <--- Sửa chỗ này
                         && !m.IsDeleted)
                .CountAsync();

            return Ok(count);
        }
    }
}