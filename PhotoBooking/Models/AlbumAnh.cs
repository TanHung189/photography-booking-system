using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class AlbumAnh
{
    public int MaAlbum { get; set; }

    public string TieuDe { get; set; } = null!;

    public string? MoTa { get; set; }

    public int MaNhiepAnhGia { get; set; }

    public virtual ICollection<AnhChiTiet> AnhChiTiets { get; set; } = new List<AnhChiTiet>();

    public virtual NguoiDung MaNhiepAnhGiaNavigation { get; set; } = null!;
}
