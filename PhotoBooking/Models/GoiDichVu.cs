using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class GoiDichVu
{
    public int MaGoi { get; set; }

    public string TenGoi { get; set; } = null!;

    public decimal GiaTien { get; set; }

    public decimal? GiaCoc { get; set; }

    public int ThoiLuong { get; set; }

    public int? SoNguoiToiDa { get; set; }

    public string? MoTaChiTiet { get; set; }

    public string? SanPhamBanGiao { get; set; }

    public string? AnhDaiDien { get; set; }

    public int MaDanhMuc { get; set; }

    public int MaNhiepAnhGia { get; set; }

    public virtual ICollection<DonDatLich> DonDatLiches { get; set; } = new List<DonDatLich>();

    public virtual DanhMuc MaDanhMucNavigation { get; set; } = null!;

    public virtual NguoiDung MaNhiepAnhGiaNavigation { get; set; } = null!;
}
