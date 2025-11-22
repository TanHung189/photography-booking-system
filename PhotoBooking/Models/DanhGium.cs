using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class DanhGium
{
    public int MaDanhGia { get; set; }

    public int SoSao { get; set; }

    public string? BinhLuan { get; set; }

    public string? PhanHoi { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public int MaDon { get; set; }

    public virtual DonDatLich MaDonNavigation { get; set; } = null!;
}
