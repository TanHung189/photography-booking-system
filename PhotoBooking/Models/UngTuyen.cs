using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class UngTuyen
{
    public int MaUngTuyen { get; set; }

    public int MaYeuCau { get; set; }

    public int MaNhiepAnhGia { get; set; }

    public decimal GiaBao { get; set; }

    public string? LoiNhan { get; set; }

    public int? TrangThai { get; set; }

    public DateTime? NgayUngTuyen { get; set; }

    public virtual NguoiDung MaNhiepAnhGiaNavigation { get; set; } = null!;

    public virtual YeuCau MaYeuCauNavigation { get; set; } = null!;
}
