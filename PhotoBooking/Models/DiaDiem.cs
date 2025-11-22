using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class DiaDiem
{
    public int MaDiaDiem { get; set; }

    public string TenThanhPho { get; set; } = null!;

    public string? Slug { get; set; }

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
