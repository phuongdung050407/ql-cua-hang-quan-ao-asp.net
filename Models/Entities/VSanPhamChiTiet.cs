using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class VSanPhamChiTiet
{
    public int SanPhamId { get; set; }

    public string MaSanPham { get; set; } = null!;

    public string TenSanPham { get; set; } = null!;

    public string TenDanhMuc { get; set; } = null!;

    public string? TenThuongHieu { get; set; }

    public string? TenChatLieu { get; set; }

    public long BienTheId { get; set; }

    public string Sku { get; set; } = null!;

    public string MaSize { get; set; } = null!;

    public string TenMau { get; set; } = null!;

    public decimal GiaBan { get; set; }

    public int TongTon { get; set; }

    public int TongGiuCho { get; set; }

    public int? CoTheBan { get; set; }

    public string TrangThaiSanPham { get; set; } = null!;

    public bool BienTheDangBan { get; set; }
}
