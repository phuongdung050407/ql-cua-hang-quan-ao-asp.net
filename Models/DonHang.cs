using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class DonHang
{
    public long Id { get; set; }

    public string MaDonHang { get; set; } = null!;

    public long NguoiDungId { get; set; }

    public string TrangThai { get; set; } = null!;

    public string PhuongThucThanhToan { get; set; } = null!;

    public string TrangThaiThanhToan { get; set; } = null!;

    public string TenNguoiNhan { get; set; } = null!;

    public string DienThoaiNhan { get; set; } = null!;

    public string DiaChiGiao { get; set; } = null!;

    public decimal TienHang { get; set; }

    public decimal PhiVanChuyen { get; set; }

    public decimal TienGiam { get; set; }

    public decimal TongThanhToan { get; set; }

    public int? MaGiamGiaId { get; set; }

    public string? GhiChu { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime ThoiGianCapNhat { get; set; }

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<DonHangChiTiet> DonHangChiTiets { get; set; } = new List<DonHangChiTiet>();

    public virtual HoaDon? HoaDon { get; set; }

    public virtual ICollection<LichSuDonHang> LichSuDonHangs { get; set; } = new List<LichSuDonHang>();

    public virtual MaGiamGium? MaGiamGia { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();

    public virtual ICollection<YeuCauHuyDon> YeuCauHuyDons { get; set; } = new List<YeuCauHuyDon>();
}
