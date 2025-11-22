using System;
using System.Collections.Generic;

namespace PhotoBooking.Models;

public partial class AnhChiTiet
{
    public int MaAnh { get; set; }

    public string DuongDanAnh { get; set; } = null!;

    public int MaAlbum { get; set; }

    public virtual AlbumAnh MaAlbumNavigation { get; set; } = null!;
}
