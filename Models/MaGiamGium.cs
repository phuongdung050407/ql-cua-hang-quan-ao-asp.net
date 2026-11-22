using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class MaGiamGium
{
    public int Id { get; set; }

    public string MaVoucher { get; set; } = null!;

    public string? MoTa { get; set; }

    public string LoaiGiam { get; set; } = null!;

    public decimal GiaTri { get; set; }

    public decimal? GiamToiDa { get; set; }

    public decimal DonToiThieu { get; set; }

    public int SoLuongPhatHanh { get; set; }

    public int SoLuongDaDung { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime NgayKetThuc { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
