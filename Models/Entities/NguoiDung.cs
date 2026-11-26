using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class NguoiDung
{
    public long Id { get; set; }

    public string Email { get; set; } = null!;

    public string? EmailChuan { get; set; }

    public string? TenDangNhap { get; set; }

    public string MatKhauHash { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string? AnhDaiDien { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public bool DaXacThucEmail { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime ThoiGianTao { get; set; }

    public DateTime ThoiGianCapNhat { get; set; }

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual DiaChi? DiaChi { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual GioHang? GioHang { get; set; }

    public virtual ICollection<LichSuDonHang> LichSuDonHangs { get; set; } = new List<LichSuDonHang>();

    public virtual ICollection<LichSuXemSanPham> LichSuXemSanPhams { get; set; } = new List<LichSuXemSanPham>();

    public virtual NhanVien? NhanVien { get; set; }

    public virtual ICollection<NhatKyHeThong> NhatKyHeThongs { get; set; } = new List<NhatKyHeThong>();

    public virtual ICollection<XacThucTaiKhoan> XacThucTaiKhoans { get; set; } = new List<XacThucTaiKhoan>();

    public virtual ICollection<YeuCauHuyDon> YeuCauHuyDonNguoiDungs { get; set; } = new List<YeuCauHuyDon>();

    public virtual ICollection<YeuCauHuyDon> YeuCauHuyDonXuLyBoiNavigations { get; set; } = new List<YeuCauHuyDon>();

    public virtual ICollection<VaiTro> VaiTros { get; set; } = new List<VaiTro>();
}
