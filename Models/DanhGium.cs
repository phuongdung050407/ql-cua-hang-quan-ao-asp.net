using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class DanhGium
{
    public long Id { get; set; }

    public int SanPhamId { get; set; }

    public long NguoiDungId { get; set; }

    public long DonHangId { get; set; }

    public byte SoSao { get; set; }

    public string? NoiDung { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? PhanHoiShop { get; set; }

    public DateTime? ThoiGianPhanHoi { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public virtual ICollection<DanhGiaAnh> DanhGiaAnhs { get; set; } = new List<DanhGiaAnh>();

    public virtual DonHang DonHang { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
