using System.ComponentModel.DataAnnotations;

namespace PhotoBooking.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        public string TieuDe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn")]
        public string NoiDung { get; set; }
    }
}