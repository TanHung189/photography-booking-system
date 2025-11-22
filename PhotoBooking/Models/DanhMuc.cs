using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class DanhMuc
{
    public int MaDanhMuc { get; set; }

    public string TenDanhMuc { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? AnhDaiDien { get; set; }

    public virtual ICollection<GoiDichVu> GoiDichVus { get; set; } = new List<GoiDichVu>();
}
