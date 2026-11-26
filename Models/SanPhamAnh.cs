using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class SanPhamAnh
{
    public long Id { get; set; }

    public int SanPhamId { get; set; }

    public long? BienTheId { get; set; }

    public string Url { get; set; } = null!;

    public bool LaAnhDaiDien { get; set; }

    public virtual BienTheSanPham? BienThe { get; set; }

    public virtual BienTheSanPham? BienTheSanPham { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;
}
