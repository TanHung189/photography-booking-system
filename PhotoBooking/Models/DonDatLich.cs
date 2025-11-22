using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class DonDatLich
{
    public int MaDon { get; set; }

    public DateTime NgayChup { get; set; }

    public decimal? TongTien { get; set; }

    public decimal? TienDaCoc { get; set; }

    public string? GhiChu { get; set; }

    public string? DiaChiChup { get; set; }

    public int TrangThai { get; set; }

    public int TrangThaiThanhToan { get; set; }

    public DateTime? NgayTao { get; set; }

    public int MaKhachHang { get; set; }

    public int MaGoi { get; set; }

    public virtual DanhGium? DanhGium { get; set; }

    public virtual GoiDichVu MaGoiNavigation { get; set; } = null!;

    public virtual NguoiDung MaKhachHangNavigation { get; set; } = null!;
}
