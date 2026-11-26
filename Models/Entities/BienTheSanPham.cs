using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class BienTheSanPham
{
    public long Id { get; set; }

    public int SanPhamId { get; set; }

    public string Sku { get; set; } = null!;

    public int KichThuocId { get; set; }

    public int MauSacId { get; set; }

    public decimal GiaBan { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; } = new List<ChiTietPhieuNhap>();

    public virtual ICollection<DonHangChiTiet> DonHangChiTiets { get; set; } = new List<DonHangChiTiet>();

    public virtual ICollection<GioHangChiTiet> GioHangChiTiets { get; set; } = new List<GioHangChiTiet>();

    public virtual KichThuoc KichThuoc { get; set; } = null!;

    public virtual MauSac MauSac { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;

    public virtual ICollection<SanPhamAnh> SanPhamAnhBienTheSanPhams { get; set; } = new List<SanPhamAnh>();

    public virtual ICollection<SanPhamAnh> SanPhamAnhBienThes { get; set; } = new List<SanPhamAnh>();

    public virtual ICollection<TonKho> TonKhos { get; set; } = new List<TonKho>();
}
