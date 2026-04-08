using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoBooking.ViewModels
{
    public class BookingViewModel
    {
        // Thông tin hiển thị (Chỉ đọc)
        public int MaGoi { get; set; }
        public string? TenGoi { get; set; }
        public decimal GiaTien { get; set; }
        public decimal GiaCoc { get; set; }
        public string? AnhBia { get; set; }
        public string? TenNhiepAnhGia { get; set; }

        // 🔥 THÊM DÒNG NÀY VÀO: Bắt buộc phải có để biết lịch của thợ nào
        public int MaNhiepAnhGia { get; set; }

        // Thông tin khách nhập (Form)
        [Required(ErrorMessage = "Vui lòng chọn ngày chụp")]
        public DateTime NgayChup { get; set; } = DateTime.Now.AddDays(1); // Mặc định là ngày mai

        [Required(ErrorMessage = "Vui lòng nhập địa điểm")]
        public string? DiaChiChup { get; set; }

        public string? GhiChu { get; set; }
    }
}