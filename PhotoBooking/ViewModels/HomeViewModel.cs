using PhotoBooking.Models; 

namespace PhotoBooking.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<PhotographerViewModel> Photographers { get; set; }
        // 1. Danh sách các gói chụp nổi bật (Ví dụ: 6 gói mới nhất)
        public List<GoiDichVu> FeaturedPackages { get; set; } = new List<GoiDichVu>();

        // 2. Danh sách các danh mục (Để làm bộ lọc tìm kiếm dịch vụ)
        public List<DanhMuc> Categories { get; set; } = new List<DanhMuc>();

        // 3. Danh sách các địa điểm (Để làm bộ lọc tìm kiếm nhiếp ảnh gia)
        public List<DiaDiem> Locations { get; set; } = new List<DiaDiem>();
    }
}