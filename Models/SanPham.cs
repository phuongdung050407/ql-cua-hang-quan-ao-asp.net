using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class SanPham
{
    public int Id { get; set; }

    public string MaSanPham { get; set; } = null!;

    public string TenSanPham { get; set; } = null!;

    public int DanhMucId { get; set; }

    public int? ThuongHieuId { get; set; }

    public int? ChatLieuId { get; set; }

    public string? MoTa { get; set; }

    public decimal GiaGoc { get; set; }

    public decimal GiaBan { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime ThoiGianTao { get; set; }

    public DateTime ThoiGianCapNhat { get; set; }

    public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();

    public virtual ChatLieu? ChatLieu { get; set; }

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual DanhMuc DanhMuc { get; set; } = null!;

    public virtual ICollection<LichSuXemSanPham> LichSuXemSanPhams { get; set; } = new List<LichSuXemSanPham>();

    public virtual ICollection<SanPhamAnh> SanPhamAnhs { get; set; } = new List<SanPhamAnh>();

    public virtual ThuongHieu? ThuongHieu { get; set; }

    public virtual ICollection<MaGiamGium> MaGiamGia { get; set; } = new List<MaGiamGium>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
