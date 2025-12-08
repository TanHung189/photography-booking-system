using Microsoft.AspNetCore.SignalR;

namespace PhotoBooking.Services
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Lấy giá trị từ Claim "UserId" mà bạn đã lưu lúc Đăng nhập
            return connection.User?.FindFirst("UserId")?.Value;
        }
    }
}
