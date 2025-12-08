using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoBooking.Models
{
    [Table("TinNhan")]
    public partial class TinNhan
    {
        [Key]
        public int MaTinNhan { get; set; }
        public int NguoiGuiId { get; set; }
        public int NguoiNhanId { get; set; }
        public string NoiDung { get; set; } = null!;
        public DateTime ThoiGianGui { get; set; }
        public bool? DaXem { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey("NguoiGuiId")]
        public virtual NguoiDung NguoiGui { get; set; } = null!;
        [ForeignKey("NguoiNhanId")]
        public virtual NguoiDung NguoiNhan { get; set; } = null!;
    }
}