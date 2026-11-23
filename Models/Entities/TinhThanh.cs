using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopProject.Models.Entities;

[Table("tinh_thanh")]
public class TinhThanh
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("ten_tinh_thanh")]
    public string TenTinhThanh { get; set; } = null!;

    [Column("trang_thai")]
    public bool TrangThai { get; set; } = true;
}