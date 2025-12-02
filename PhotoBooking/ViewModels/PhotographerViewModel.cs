using PhotoBooking.Models;

namespace PhotoBooking.ViewModels
{
    public class PhotographerViewModel
    {
        // Thông tin gốc của người dùng
        public NguoiDung User { get; set; }

        // Danh sách link ảnh để chạy slide
        public List<string> SlideImages { get; set; } = new List<string>();

        // Điểm đánh giá trung bình (Ví dụ: 4.5)
        public double AvgRating { get; set; }

        // Tổng số lượng đánh giá
        public int ReviewCount { get; set; }

        // Số lượng gói chụp đang cung cấp
        public int PackageCount { get; set; }
        public int NamKinhNghiem { get; set; }
    }
}
