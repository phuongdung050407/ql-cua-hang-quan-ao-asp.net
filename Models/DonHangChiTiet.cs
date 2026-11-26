using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class DonHangChiTiet
{
    public long Id { get; set; }

    public long DonHangId { get; set; }

    public long BienTheId { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public string Size { get; set; } = null!;

    public string MauSac { get; set; } = null!;

    public decimal DonGia { get; set; }

    public int SoLuong { get; set; }

    public decimal? ThanhTien { get; set; }

    public virtual BienTheSanPham BienThe { get; set; } = null!;

    public virtual ICollection<ChiTietGiuCho> ChiTietGiuChos { get; set; } = new List<ChiTietGiuCho>();

    public virtual DonHang DonHang { get; set; } = null!;
}
