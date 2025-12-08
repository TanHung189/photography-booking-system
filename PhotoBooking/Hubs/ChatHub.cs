using Microsoft.AspNetCore.SignalR;
using PhotoBooking.Models;
using System;
using System.Threading.Tasks;

namespace PhotoBooking.Hubs
{
    public class ChatHub : Hub
    {
        private readonly PhotoBookingContext _context;
        public ChatHub(PhotoBookingContext context) { _context = context; }

        public async Task SendMessage(string receiverIdStr, string message)
        {
            var senderIdStr = Context.UserIdentifier; // Lấy ID người gửi từ Provider
            if (string.IsNullOrEmpty(senderIdStr) || string.IsNullOrEmpty(receiverIdStr)) return;

            // 1. Lưu vào Database
            var tinNhan = new TinNhan
            {
                NguoiGuiId = int.Parse(senderIdStr),
                NguoiNhanId = int.Parse(receiverIdStr),
                NoiDung = message,
                ThoiGianGui = DateTime.Now,
                DaXem = false
            };
            _context.TinNhans.Add(tinNhan);
            await _context.SaveChangesAsync();

            // 2. Gửi tin nhắn cho Người nhận (Real-time)
            await Clients.User(receiverIdStr).SendAsync("ReceiveMessage", senderIdStr, message, DateTime.Now.ToString("HH:mm"));

            // 3. Gửi lại cho Người gửi (để hiện lên màn hình của mình)
            await Clients.Caller.SendAsync("ReceiveMessage", senderIdStr, message, DateTime.Now.ToString("HH:mm"));
        }

        // Trong ChatHub.cs
        public async Task UnsendMessage(int messageId)
        {
            var senderIdStr = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderIdStr)) return;
            int senderId = int.Parse(senderIdStr);

            var msg = await _context.TinNhans.FindAsync(messageId);

            // Chỉ cho phép xóa nếu mình là người gửi
            if (msg != null && msg.NguoiGuiId == senderId)
            {
                msg.IsDeleted = true; // Đánh dấu đã xóa
                await _context.SaveChangesAsync();

                // Báo cho cả 2 bên (Người gửi & Người nhận) biết là tin này đã xóa
                // Gửi về ID của tin nhắn để giao diện tìm và ẩn đi
                await Clients.User(msg.NguoiNhanId.ToString()).SendAsync("MessageUnsent", messageId);
                await Clients.Caller.SendAsync("MessageUnsent", messageId);
            }
        }
    }
}