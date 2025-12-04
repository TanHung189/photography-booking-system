using Microsoft.AspNetCore.SignalR;

namespace PhotoBooking.Web.Hubs
{
    public class BookingHub : Hub
    {
        // Hàm này để Client gọi lên (nếu cần), nhưng ở đây ta chủ yếu dùng Server bắn xuống
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
