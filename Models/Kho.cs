using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class Kho
{
    public int Id { get; set; }

    public string MaKho { get; set; } = null!;

    public string TenKho { get; set; } = null!;

    public string? DiaChi { get; set; }

    public long? NguoiQuanLyId { get; set; }

    public bool TrangThai { get; set; }

    public virtual NhanVien? NguoiQuanLy { get; set; }

    public virtual ICollection<PhieuNhapKho> PhieuNhapKhos { get; set; } = new List<PhieuNhapKho>();

    public virtual ICollection<TonKho> TonKhos { get; set; } = new List<TonKho>();
}
