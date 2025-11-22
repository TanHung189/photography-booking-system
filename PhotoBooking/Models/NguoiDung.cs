using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class NguoiDung
{
    public int MaNguoiDung { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string HoVaTen { get; set; } = null!;

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public string VaiTro { get; set; } = null!;

    public string? AnhDaiDien { get; set; }

    public string? AnhBia { get; set; }

    public string? GioiThieu { get; set; }

    public int? MaDiaDiem { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual ICollection<AlbumAnh> AlbumAnhs { get; set; } = new List<AlbumAnh>();

    public virtual ICollection<DonDatLich> DonDatLiches { get; set; } = new List<DonDatLich>();

    public virtual ICollection<GoiDichVu> GoiDichVus { get; set; } = new List<GoiDichVu>();

    public virtual DiaDiem? MaDiaDiemNavigation { get; set; }
}
