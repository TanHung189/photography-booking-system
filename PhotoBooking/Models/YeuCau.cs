using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class YeuCau
{
    public int MaYeuCau { get; set; }

    public int MaKhachHang { get; set; }

    public string TieuDe { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? DiaChi { get; set; }

    public decimal? NganSach { get; set; }

    public DateTime? NgayCanChup { get; set; }

    public int? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual NguoiDung MaKhachHangNavigation { get; set; } = null!;
}
