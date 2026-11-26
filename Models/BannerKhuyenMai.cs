using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class BannerKhuyenMai
{
    public int Id { get; set; }

    public string TieuDe { get; set; } = null!;

    public string HinhAnhUrl { get; set; } = null!;

    public string? LienKetUrl { get; set; }

    public int ThuTu { get; set; }

    public bool TrangThai { get; set; }

    public DateTime? NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }
}
