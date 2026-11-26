using System.ComponentModel.DataAnnotations.Schema;

namespace ShopProject.Models;

public class SanPhamGoiYDto
{
    [Column("id")]
    public int Id { get; set; }

    [Column("ma_san_pham")]
    public string MaSanPham { get; set; } = string.Empty;

    [Column("ten_san_pham")]
    public string TenSanPham { get; set; } = string.Empty;

    [Column("gia_ban")]
    public decimal GiaBan { get; set; }

    [Column("ten_danh_muc")]
    public string TenDanhMuc { get; set; } = string.Empty;
}