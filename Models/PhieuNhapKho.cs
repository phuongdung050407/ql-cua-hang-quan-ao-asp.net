using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class PhieuNhapKho
{
    public long Id { get; set; }

    public string MaPhieu { get; set; } = null!;

    public int KhoId { get; set; }

    public long NhanVienId { get; set; }

    public string? NhaCungCap { get; set; }

    public DateTime NgayNhap { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; } = new List<ChiTietPhieuNhap>();

    public virtual Kho Kho { get; set; } = null!;

    public virtual NhanVien NhanVien { get; set; } = null!;
}
