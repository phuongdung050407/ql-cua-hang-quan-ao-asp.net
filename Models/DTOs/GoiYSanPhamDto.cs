using System.ComponentModel.DataAnnotations.Schema;

namespace ShopProject.Models.DTOs;

public class GoiYSanPhamDto
{
    [Column("id")]
    public int Id { get; set; }

    [Column("ma_san_pham")]
    public string MaSanPham { get; set; } = "";

    [Column("ten_san_pham")]
    public string TenSanPham { get; set; } = "";

    [Column("gia_ban")]
    public decimal GiaBan { get; set; }

    [Column("ten_danh_muc")]
    public string TenDanhMuc { get; set; } = "";
}