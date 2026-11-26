using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class GioHangChiTiet
{
    public long Id { get; set; }

    public long GioHangId { get; set; }

    public long BienTheId { get; set; }

    public int SoLuong { get; set; }

    public DateTime ThoiGianThem { get; set; }

    public virtual BienTheSanPham BienThe { get; set; } = null!;

    public virtual GioHang GioHang { get; set; } = null!;
}
