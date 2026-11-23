using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class NhanVien
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public string MaNhanVien { get; set; } = null!;

    public string ChucVu { get; set; } = null!;

    public DateOnly NgayVaoLam { get; set; }

    public decimal Luong { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<Kho> Khos { get; set; } = new List<Kho>();

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual ICollection<PhieuNhapKho> PhieuNhapKhos { get; set; } = new List<PhieuNhapKho>();
}
