using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class ChiTietPhieuNhap
{
    public long Id { get; set; }

    public long PhieuNhapId { get; set; }

    public long BienTheId { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal? ThanhTien { get; set; }

    public virtual BienTheSanPham BienThe { get; set; } = null!;

    public virtual PhieuNhapKho PhieuNhap { get; set; } = null!;
}
