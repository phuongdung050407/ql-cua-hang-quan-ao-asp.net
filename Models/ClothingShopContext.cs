using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShopProject.Models;



public partial class ClothingShopContext : DbContext
{

    
    public ClothingShopContext()
    {
    }

    public ClothingShopContext(DbContextOptions<ClothingShopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BannerKhuyenMai> BannerKhuyenMais { get; set; }

    public virtual DbSet<BienTheSanPham> BienTheSanPhams { get; set; }

    public virtual DbSet<ChatLieu> ChatLieus { get; set; }

    public virtual DbSet<ChiTietGiuCho> ChiTietGiuChos { get; set; }

    public virtual DbSet<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; }

    public virtual DbSet<DanhGiaAnh> DanhGiaAnhs { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DiaChi> DiaChis { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<DonHangChiTiet> DonHangChiTiets { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<GioHangChiTiet> GioHangChiTiets { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<Kho> Khos { get; set; }

    public virtual DbSet<KichThuoc> KichThuocs { get; set; }

    public virtual DbSet<LichSuDonHang> LichSuDonHangs { get; set; }

    public virtual DbSet<LichSuXemSanPham> LichSuXemSanPhams { get; set; }

    public virtual DbSet<MaGiamGium> MaGiamGia { get; set; }

    public virtual DbSet<MauSac> MauSacs { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }

    public virtual DbSet<PhieuNhapKho> PhieuNhapKhos { get; set; }

    public virtual DbSet<Quyen> Quyens { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<SanPhamGoiYDto> SanPhamGoiYResults { get; set; }

    public virtual DbSet<SanPhamAnh> SanPhamAnhs { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<ThuongHieu> ThuongHieus { get; set; }

    public virtual DbSet<TonKho> TonKhos { get; set; }

    public virtual DbSet<VSanPhamChiTiet> VSanPhamChiTiets { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<XacThucTaiKhoan> XacThucTaiKhoans { get; set; }

    public virtual DbSet<YeuCauHuyDon> YeuCauHuyDons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BannerKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__banner_k__3213E83F586E0625");

            entity.ToTable("banner_khuyen_mai");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HinhAnhUrl)
                .HasMaxLength(500)
                .HasColumnName("hinh_anh_url");
            entity.Property(e => e.LienKetUrl)
                .HasMaxLength(500)
                .HasColumnName("lien_ket_url");
            entity.Property(e => e.NgayBatDau).HasColumnName("ngay_bat_dau");
            entity.Property(e => e.NgayKetThuc).HasColumnName("ngay_ket_thuc");
            entity.Property(e => e.ThuTu).HasColumnName("thu_tu");
            entity.Property(e => e.TieuDe)
                .HasMaxLength(200)
                .HasColumnName("tieu_de");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trang_thai");
        });

        modelBuilder.Entity<BienTheSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__bien_the__3213E83F579D7CEA");

            entity.ToTable("bien_the_san_pham");

            entity.HasIndex(e => new { e.SanPhamId, e.TrangThai }, "IX_bien_the_san_pham");

            entity.HasIndex(e => e.Sku, "UQ__bien_the__DDDF4BE707FA1FE3").IsUnique();

            entity.HasIndex(e => new { e.SanPhamId, e.KichThuocId, e.MauSacId }, "UQ_bien_the").IsUnique();

            entity.HasIndex(e => new { e.SanPhamId, e.Id }, "UQ_bien_the_san_pham_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GiaBan)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("gia_ban");
            entity.Property(e => e.KichThuocId).HasColumnName("kich_thuoc_id");
            entity.Property(e => e.MauSacId).HasColumnName("mau_sac_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.Sku)
                .HasMaxLength(80)
                .HasColumnName("sku");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.KichThuoc).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.KichThuocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__bien_the___kich___06CD04F7");

            entity.HasOne(d => d.MauSac).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.MauSacId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__bien_the___mau_s__07C12930");

            entity.HasOne(d => d.SanPham).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__bien_the___san_p__05D8E0BE");
        });

        modelBuilder.Entity<ChatLieu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chat_lie__3213E83FB069870F");

            entity.ToTable("chat_lieu");

            entity.HasIndex(e => e.TenChatLieu, "UQ__chat_lie__47FA287200D13DFF").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TenChatLieu)
                .HasMaxLength(100)
                .HasColumnName("ten_chat_lieu");
        });

        modelBuilder.Entity<ChiTietGiuCho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chi_tiet__3213E83F6E6093E9");

            entity.ToTable("chi_tiet_giu_cho");

            entity.HasIndex(e => new { e.DonHangCtId, e.KhoId }, "UQ_chi_tiet_giu_cho").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BienTheId).HasColumnName("bien_the_id");
            entity.Property(e => e.DonHangCtId).HasColumnName("don_hang_ct_id");
            entity.Property(e => e.KhoId).HasColumnName("kho_id");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");

            entity.HasOne(d => d.DonHangCt).WithMany(p => p.ChiTietGiuChos)
                .HasForeignKey(d => d.DonHangCtId)
                .HasConstraintName("FK__chi_tiet___don_h__69FBBC1F");

            entity.HasOne(d => d.TonKho).WithMany(p => p.ChiTietGiuChos)
                .HasPrincipalKey(p => new { p.KhoId, p.BienTheId })
                .HasForeignKey(d => new { d.KhoId, d.BienTheId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__chi_tiet_giu_cho__6AEFE058");
        });

        modelBuilder.Entity<ChiTietPhieuNhap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chi_tiet__3213E83F4758D493");

            entity.ToTable("chi_tiet_phieu_nhap");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BienTheId).HasColumnName("bien_the_id");
            entity.Property(e => e.DonGia)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("don_gia");
            entity.Property(e => e.PhieuNhapId).HasColumnName("phieu_nhap_id");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.ThanhTien)
                .HasComputedColumnSql("([so_luong]*[don_gia])", true)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("thanh_tien");

            entity.HasOne(d => d.BienThe).WithMany(p => p.ChiTietPhieuNhaps)
                .HasForeignKey(d => d.BienTheId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__chi_tiet___bien___3587F3E0");

            entity.HasOne(d => d.PhieuNhap).WithMany(p => p.ChiTietPhieuNhaps)
                .HasForeignKey(d => d.PhieuNhapId)
                .HasConstraintName("FK__chi_tiet___phieu__3493CFA7");
        });

        modelBuilder.Entity<DanhGiaAnh>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__danh_gia__3213E83F903305BD");

            entity.ToTable("danh_gia_anh");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DanhGiaId).HasColumnName("danh_gia_id");
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");

            entity.HasOne(d => d.DanhGia).WithMany(p => p.DanhGiaAnhs)
                .HasForeignKey(d => d.DanhGiaId)
                .HasConstraintName("FK__danh_gia___danh___1209AD79");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__danh_gia__3213E83F56680C4C");

            entity.ToTable("danh_gia", tb => tb.HasTrigger("trg_danh_gia_kiem_tra_don_hang"));

            entity.HasIndex(e => new { e.SanPhamId, e.TrangThai }, "IX_danh_gia_san_pham");

            entity.HasIndex(e => new { e.NguoiDungId, e.DonHangId, e.SanPhamId }, "UQ_danh_gia").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonHangId).HasColumnName("don_hang_id");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.NoiDung)
                .HasMaxLength(1000)
                .HasColumnName("noi_dung");
            entity.Property(e => e.PhanHoiShop)
                .HasMaxLength(1000)
                .HasColumnName("phan_hoi_shop");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.SoSao).HasColumnName("so_sao");
            entity.Property(e => e.ThoiGianPhanHoi).HasColumnName("thoi_gian_phan_hoi");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Cho_duyet")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.DonHang).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.DonHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__danh_gia__don_ha__0F2D40CE");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__danh_gia__nguoi___0E391C95");

            entity.HasOne(d => d.SanPham).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__danh_gia__san_ph__0D44F85C");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__danh_muc__3213E83F8FBC6C91");

            entity.ToTable("danh_muc");

            entity.HasIndex(e => e.TenDanhMuc, "UQ__danh_muc__B3D28925F5F8ED23").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.TenDanhMuc)
                .HasMaxLength(120)
                .HasColumnName("ten_danh_muc");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trang_thai");
        });

        modelBuilder.Entity<DiaChi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__dia_chi__3213E83F13E9E944");

            entity.ToTable("dia_chi");

            entity.HasIndex(e => e.NguoiDungId, "UX_dia_chi_mac_dinh")
                .IsUnique()
                .HasFilter("([mac_dinh]=(1))");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiaChiChiTiet)
                .HasMaxLength(300)
                .HasColumnName("dia_chi_chi_tiet");
            entity.Property(e => e.MacDinh).HasColumnName("mac_dinh");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.PhuongXa)
                .HasMaxLength(100)
                .HasColumnName("phuong_xa");
            entity.Property(e => e.QuanHuyen)
                .HasMaxLength(100)
                .HasColumnName("quan_huyen");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.TenNguoiNhan)
                .HasMaxLength(150)
                .HasColumnName("ten_nguoi_nhan");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");

            entity.HasOne(d => d.NguoiDung).WithOne(p => p.DiaChi)
                .HasForeignKey<DiaChi>(d => d.NguoiDungId)
                .HasConstraintName("FK__dia_chi__nguoi_d__5EBF139D");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__don_hang__3213E83F16FD52C6");

            entity.ToTable("don_hang");

            entity.HasIndex(e => new { e.NguoiDungId, e.ThoiGianTao }, "IX_don_hang_nguoi_dung_ngay").IsDescending(false, true);

            entity.HasIndex(e => new { e.TrangThai, e.ThoiGianTao }, "IX_don_hang_trang_thai");

            entity.HasIndex(e => e.MaDonHang, "UQ__don_hang__0246C5EB2E396E3D").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiaChiGiao)
                .HasMaxLength(500)
                .HasColumnName("dia_chi_giao");
            entity.Property(e => e.DienThoaiNhan)
                .HasMaxLength(20)
                .HasColumnName("dien_thoai_nhan");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(500)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.MaDonHang)
                .HasMaxLength(40)
                .HasColumnName("ma_don_hang");
            entity.Property(e => e.MaGiamGiaId).HasColumnName("ma_giam_gia_id");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.PhiVanChuyen)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("phi_van_chuyen");
            entity.Property(e => e.PhuongThucThanhToan)
                .HasMaxLength(20)
                .HasDefaultValue("COD")
                .HasColumnName("phuong_thuc_thanh_toan");
            entity.Property(e => e.TenNguoiNhan)
                .HasMaxLength(150)
                .HasColumnName("ten_nguoi_nhan");
            entity.Property(e => e.ThoiGianCapNhat)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_cap_nhat");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.TienGiam)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tien_giam");
            entity.Property(e => e.TienHang)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tien_hang");
            entity.Property(e => e.TongThanhToan)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tong_thanh_toan");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Cho_xac_nhan")
                .HasColumnName("trang_thai");
            entity.Property(e => e.TrangThaiThanhToan)
                .HasMaxLength(20)
                .HasDefaultValue("Chua_thanh_toan")
                .HasColumnName("trang_thai_thanh_toan");

            entity.HasOne(d => d.MaGiamGia).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.MaGiamGiaId)
                .HasConstraintName("FK__don_hang__ma_gia__5F7E2DAC");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__don_hang__nguoi___5E8A0973");
        });

        modelBuilder.Entity<DonHangChiTiet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__don_hang__3213E83FC26BD7D1");

            entity.ToTable("don_hang_chi_tiet");

            entity.HasIndex(e => e.DonHangId, "IX_chi_tiet_don_hang");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BienTheId).HasColumnName("bien_the_id");
            entity.Property(e => e.DonGia)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("don_gia");
            entity.Property(e => e.DonHangId).HasColumnName("don_hang_id");
            entity.Property(e => e.MauSac)
                .HasMaxLength(50)
                .HasColumnName("mau_sac");
            entity.Property(e => e.Size)
                .HasMaxLength(20)
                .HasColumnName("size");
            entity.Property(e => e.Sku)
                .HasMaxLength(80)
                .HasColumnName("sku");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("ten_san_pham");
            entity.Property(e => e.ThanhTien)
                .HasComputedColumnSql("([don_gia]*[so_luong])", true)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("thanh_tien");

            entity.HasOne(d => d.BienThe).WithMany(p => p.DonHangChiTiets)
                .HasForeignKey(d => d.BienTheId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__don_hang___bien___65370702");

            entity.HasOne(d => d.DonHang).WithMany(p => p.DonHangChiTiets)
                .HasForeignKey(d => d.DonHangId)
                .HasConstraintName("FK__don_hang___don_h__6442E2C9");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__gio_hang__3213E83FC9040A41");

            entity.ToTable("gio_hang");

            entity.HasIndex(e => e.NguoiDungId, "UQ__gio_hang__3F7CE07654C4D843").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");

            entity.HasOne(d => d.NguoiDung).WithOne(p => p.GioHang)
                .HasForeignKey<GioHang>(d => d.NguoiDungId)
                .HasConstraintName("FK__gio_hang__nguoi___151B244E");
        });

        modelBuilder.Entity<GioHangChiTiet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__gio_hang__3213E83F1BAD7E07");

            entity.ToTable("gio_hang_chi_tiet");

            entity.HasIndex(e => new { e.GioHangId, e.BienTheId }, "UQ_gio_hang_sku").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BienTheId).HasColumnName("bien_the_id");
            entity.Property(e => e.GioHangId).HasColumnName("gio_hang_id");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.ThoiGianThem)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_them");

            entity.HasOne(d => d.BienThe).WithMany(p => p.GioHangChiTiets)
                .HasForeignKey(d => d.BienTheId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__gio_hang___bien___1BC821DD");

            entity.HasOne(d => d.GioHang).WithMany(p => p.GioHangChiTiets)
                .HasForeignKey(d => d.GioHangId)
                .HasConstraintName("FK__gio_hang___gio_h__1AD3FDA4");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__hoa_don__3213E83F4F81EFD9");

            entity.ToTable("hoa_don");

            entity.HasIndex(e => e.DonHangId, "UQ__hoa_don__B81FB4F2F14442D5").IsUnique();

            entity.HasIndex(e => e.MaHoaDon, "UQ__hoa_don__DBE2D9E2E7E82F69").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonHangId).HasColumnName("don_hang_id");
            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(40)
                .HasColumnName("ma_hoa_don");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.Thue)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("thue");
            entity.Property(e => e.TongTien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tong_tien");

            entity.HasOne(d => d.DonHang).WithOne(p => p.HoaDon)
                .HasForeignKey<HoaDon>(d => d.DonHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__hoa_don__don_han__7849DB76");
        });

        modelBuilder.Entity<Kho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__kho__3213E83F97974D2E");

            entity.ToTable("kho");

            entity.HasIndex(e => e.MaKho, "UQ__kho__0A2E13A53C04E53F").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(300)
                .HasColumnName("dia_chi");
            entity.Property(e => e.MaKho)
                .HasMaxLength(30)
                .HasColumnName("ma_kho");
            entity.Property(e => e.NguoiQuanLyId).HasColumnName("nguoi_quan_ly_id");
            entity.Property(e => e.TenKho)
                .HasMaxLength(150)
                .HasColumnName("ten_kho");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiQuanLy).WithMany(p => p.Khos)
                .HasForeignKey(d => d.NguoiQuanLyId)
                .HasConstraintName("FK__kho__nguoi_quan___208CD6FA");
        });

        modelBuilder.Entity<KichThuoc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__kich_thu__3213E83F1392B676");

            entity.ToTable("kich_thuoc");

            entity.HasIndex(e => e.MaSize, "UQ__kich_thu__0B396BB0BBE70A7F").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaSize)
                .HasMaxLength(20)
                .HasColumnName("ma_size");
            entity.Property(e => e.ThuTu).HasColumnName("thu_tu");
        });

        modelBuilder.Entity<LichSuDonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lich_su___3213E83F1C91E6DC");

            entity.ToTable("lich_su_don_hang");

            entity.HasIndex(e => new { e.DonHangId, e.ThoiGian }, "IX_lich_su_don_hang_don_thoi_gian").IsDescending(false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonHangId).HasColumnName("don_hang_id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(500)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.NguoiCapNhat).HasColumnName("nguoi_cap_nhat");
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian");
            entity.Property(e => e.TrangThaiCu)
                .HasMaxLength(20)
                .HasColumnName("trang_thai_cu");
            entity.Property(e => e.TrangThaiMoi)
                .HasMaxLength(20)
                .HasColumnName("trang_thai_moi");

            entity.HasOne(d => d.DonHang).WithMany(p => p.LichSuDonHangs)
                .HasForeignKey(d => d.DonHangId)
                .HasConstraintName("FK__lich_su_d__don_h__6EC0713C");

            entity.HasOne(d => d.NguoiCapNhatNavigation).WithMany(p => p.LichSuDonHangs)
                .HasForeignKey(d => d.NguoiCapNhat)
                .HasConstraintName("FK__lich_su_d__nguoi__6FB49575");
        });

        modelBuilder.Entity<LichSuXemSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lich_su___3213E83F80717680");

            entity.ToTable("lich_su_xem_san_pham");

            entity.HasIndex(e => new { e.NguoiDungId, e.ThoiGianXem }, "IX_lich_su_xem").IsDescending(false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.ThoiGianXem)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_xem");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.LichSuXemSanPhams)
                .HasForeignKey(d => d.NguoiDungId)
                .HasConstraintName("FK__lich_su_x__nguoi__15DA3E5D");

            entity.HasOne(d => d.SanPham).WithMany(p => p.LichSuXemSanPhams)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__lich_su_x__san_p__16CE6296");
        });

        modelBuilder.Entity<MaGiamGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ma_giam___3213E83F979858DA");

            entity.ToTable("ma_giam_gia");

            entity.HasIndex(e => new { e.TrangThai, e.NgayBatDau, e.NgayKetThuc }, "IX_voucher_hieu_luc");

            entity.HasIndex(e => e.MaVoucher, "UQ__ma_giam___466D17BF7C5B468A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonToiThieu)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("don_toi_thieu");
            entity.Property(e => e.GiaTri)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("gia_tri");
            entity.Property(e => e.GiamToiDa)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("giam_toi_da");
            entity.Property(e => e.LoaiGiam)
                .HasMaxLength(20)
                .HasColumnName("loai_giam");
            entity.Property(e => e.MaVoucher)
                .HasMaxLength(40)
                .HasColumnName("ma_voucher");
            entity.Property(e => e.MoTa)
                .HasMaxLength(300)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NgayBatDau).HasColumnName("ngay_bat_dau");
            entity.Property(e => e.NgayKetThuc).HasColumnName("ngay_ket_thuc");
            entity.Property(e => e.SoLuongDaDung).HasColumnName("so_luong_da_dung");
            entity.Property(e => e.SoLuongPhatHanh).HasColumnName("so_luong_phat_hanh");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trang_thai");

            entity.HasMany(d => d.SanPhams).WithMany(p => p.MaGiamGia)
                .UsingEntity<Dictionary<string, object>>(
                    "KhuyenMaiSanPham",
                    r => r.HasOne<SanPham>().WithMany()
                        .HasForeignKey("SanPhamId")
                        .HasConstraintName("FK__khuyen_ma__san_p__47A6A41B"),
                    l => l.HasOne<MaGiamGium>().WithMany()
                        .HasForeignKey("MaGiamGiaId")
                        .HasConstraintName("FK__khuyen_ma__ma_gi__46B27FE2"),
                    j =>
                    {
                        j.HasKey("MaGiamGiaId", "SanPhamId").HasName("PK__khuyen_m__A7F84C13125803A3");
                        j.ToTable("khuyen_mai_san_pham");
                        j.IndexerProperty<int>("MaGiamGiaId").HasColumnName("ma_giam_gia_id");
                        j.IndexerProperty<int>("SanPhamId").HasColumnName("san_pham_id");
                    });
        });

        modelBuilder.Entity<MauSac>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__mau_sac__3213E83FA522EEAE");

            entity.ToTable("mau_sac");

            entity.HasIndex(e => e.TenMau, "UQ__mau_sac__5120D1BD854DA9EE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaHex)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ma_hex");
            entity.Property(e => e.TenMau)
                .HasMaxLength(50)
                .HasColumnName("ten_mau");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nguoi_du__3213E83FD064C01B");

            entity.ToTable("nguoi_dung");

            entity.HasIndex(e => e.EmailChuan, "UQ_nguoi_dung_email").IsUnique();

            entity.HasIndex(e => e.TenDangNhap, "UQ_nguoi_dung_ten_dang_nhap").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnhDaiDien)
                .HasMaxLength(500)
                .HasColumnName("anh_dai_dien");
            entity.Property(e => e.DaXacThucEmail).HasColumnName("da_xac_thuc_email");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailChuan)
                .HasMaxLength(255)
                .HasComputedColumnSql("(lower(ltrim(rtrim([email]))))", true)
                .HasColumnName("email_chuan");
            entity.Property(e => e.GioiTinh)
                .HasMaxLength(20)
                .HasColumnName("gioi_tinh");
            entity.Property(e => e.HoTen)
                .HasMaxLength(200)
                .HasColumnName("ho_ten");
            entity.Property(e => e.MatKhauHash)
                .HasMaxLength(255)
                .HasColumnName("mat_khau_hash");
            entity.Property(e => e.NgaySinh).HasColumnName("ngay_sinh");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.TenDangNhap)
                .HasMaxLength(100)
                .HasColumnName("ten_dang_nhap");
            entity.Property(e => e.ThoiGianCapNhat)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_cap_nhat");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Hoat_dong")
                .HasColumnName("trang_thai");

            entity.HasMany(d => d.VaiTros).WithMany(p => p.NguoiDungs)
                .UsingEntity<Dictionary<string, object>>(
                    "NguoiDungVaiTro",
                    r => r.HasOne<VaiTro>().WithMany()
                        .HasForeignKey("VaiTroId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__nguoi_dun__vai_t__4BAC3F29"),
                    l => l.HasOne<NguoiDung>().WithMany()
                        .HasForeignKey("NguoiDungId")
                        .HasConstraintName("FK__nguoi_dun__nguoi__4AB81AF0"),
                    j =>
                    {
                        j.HasKey("NguoiDungId", "VaiTroId");
                        j.ToTable("nguoi_dung_vai_tro");
                        j.IndexerProperty<long>("NguoiDungId").HasColumnName("nguoi_dung_id");
                        j.IndexerProperty<int>("VaiTroId").HasColumnName("vai_tro_id");
                    });
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nhan_vie__3213E83FAE835AAE");

            entity.ToTable("nhan_vien");

            entity.HasIndex(e => e.NguoiDungId, "UQ__nhan_vie__3F7CE076D6B0335F").IsUnique();

            entity.HasIndex(e => e.MaNhanVien, "UQ__nhan_vie__6781B7B868FBE5A3").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChucVu)
                .HasMaxLength(100)
                .HasColumnName("chuc_vu");
            entity.Property(e => e.Luong)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("luong");
            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(30)
                .HasColumnName("ma_nhan_vien");
            entity.Property(e => e.NgayVaoLam).HasColumnName("ngay_vao_lam");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Dang_lam")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiDung).WithOne(p => p.NhanVien)
                .HasForeignKey<NhanVien>(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__nhan_vien__nguoi__5441852A");
        });

        modelBuilder.Entity<NhatKyHeThong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nhat_ky___3213E83F896AB24C");

            entity.ToTable("nhat_ky_he_thong");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DoiTuong)
                .HasMaxLength(100)
                .HasColumnName("doi_tuong");
            entity.Property(e => e.DoiTuongId).HasColumnName("doi_tuong_id");
            entity.Property(e => e.HanhDong)
                .HasMaxLength(50)
                .HasColumnName("hanh_dong");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.NoiDung)
                .HasMaxLength(1000)
                .HasColumnName("noi_dung");
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.NhatKyHeThongs)
                .HasForeignKey(d => d.NguoiDungId)
                .HasConstraintName("FK__nhat_ky_h__nguoi__1A9EF37A");
        });

        modelBuilder.Entity<PhieuNhapKho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__phieu_nh__3213E83F6FB88AE5");

            entity.ToTable("phieu_nhap_kho");

            entity.HasIndex(e => e.MaPhieu, "UQ__phieu_nh__11D0F07BF1773D65").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(500)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.KhoId).HasColumnName("kho_id");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(40)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayNhap)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("ngay_nhap");
            entity.Property(e => e.NhaCungCap)
                .HasMaxLength(200)
                .HasColumnName("nha_cung_cap");
            entity.Property(e => e.NhanVienId).HasColumnName("nhan_vien_id");

            entity.HasOne(d => d.Kho).WithMany(p => p.PhieuNhapKhos)
                .HasForeignKey(d => d.KhoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__phieu_nha__kho_i__2EDAF651");

            entity.HasOne(d => d.NhanVien).WithMany(p => p.PhieuNhapKhos)
                .HasForeignKey(d => d.NhanVienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__phieu_nha__nhan___2FCF1A8A");
        });

        modelBuilder.Entity<Quyen>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__quyen__3213E83FA0230FBE");

            entity.ToTable("quyen");

            entity.HasIndex(e => e.MaQuyen, "UQ__quyen__B9A4290E19F9AD03").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaQuyen)
                .HasMaxLength(100)
                .HasColumnName("ma_quyen");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__san_pham__3213E83FF2087CAC");

            entity.ToTable("san_pham");

            entity.HasIndex(e => new { e.DanhMucId, e.TrangThai }, "IX_san_pham_danh_muc");

            entity.HasIndex(e => e.TenSanPham, "IX_san_pham_ten");

            entity.HasIndex(e => e.MaSanPham, "UQ__san_pham__9D25990D72ED61E0").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChatLieuId).HasColumnName("chat_lieu_id");
            entity.Property(e => e.DanhMucId).HasColumnName("danh_muc_id");
            entity.Property(e => e.GiaBan)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("gia_ban");
            entity.Property(e => e.GiaGoc)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("gia_goc");
            entity.Property(e => e.MaSanPham)
                .HasMaxLength(40)
                .HasColumnName("ma_san_pham");
            entity.Property(e => e.MoTa).HasColumnName("mo_ta");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("ten_san_pham");
            entity.Property(e => e.ThoiGianCapNhat)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_cap_nhat");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.ThuongHieuId).HasColumnName("thuong_hieu_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Dang_ban")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.ChatLieu).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.ChatLieuId)
                .HasConstraintName("FK__san_pham__chat_l__7F2BE32F");

            entity.HasOne(d => d.DanhMuc).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.DanhMucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__san_pham__danh_m__7D439ABD");

            entity.HasOne(d => d.ThuongHieu).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.ThuongHieuId)
                .HasConstraintName("FK__san_pham__thuong__7E37BEF6");

            entity.HasMany(d => d.Tags).WithMany(p => p.SanPhams)
                .UsingEntity<Dictionary<string, object>>(
                    "SanPhamTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("FK__san_pham___tag_i__10566F31"),
                    l => l.HasOne<SanPham>().WithMany()
                        .HasForeignKey("SanPhamId")
                        .HasConstraintName("FK__san_pham___san_p__0F624AF8"),
                    j =>
                    {
                        j.HasKey("SanPhamId", "TagId").HasName("PK__san_pham__3919D760B894BC5B");
                        j.ToTable("san_pham_tag");
                        j.IndexerProperty<int>("SanPhamId").HasColumnName("san_pham_id");
                        j.IndexerProperty<int>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<SanPhamAnh>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__san_pham__3213E83FE4CAE708");

            entity.ToTable("san_pham_anh");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BienTheId).HasColumnName("bien_the_id");
            entity.Property(e => e.LaAnhDaiDien).HasColumnName("la_anh_dai_dien");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");

            entity.HasOne(d => d.BienThe).WithMany(p => p.SanPhamAnhBienThes)
                .HasForeignKey(d => d.BienTheId)
                .HasConstraintName("FK__san_pham___bien___0C85DE4D");

            entity.HasOne(d => d.SanPham).WithMany(p => p.SanPhamAnhs)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__san_pham___san_p__0B91BA14");

            entity.HasOne(d => d.BienTheSanPham).WithMany(p => p.SanPhamAnhBienTheSanPhams)
                .HasPrincipalKey(p => new { p.SanPhamId, p.Id })
                .HasForeignKey(d => new { d.SanPhamId, d.BienTheId })
                .HasConstraintName("FK_san_pham_anh_bien_the_cung_san_pham");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tag__3213E83F6F524D2B");

            entity.ToTable("tag");

            entity.HasIndex(e => new { e.TenTag, e.Loai }, "UQ_tag").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Loai)
                .HasMaxLength(30)
                .HasColumnName("loai");
            entity.Property(e => e.TenTag)
                .HasMaxLength(100)
                .HasColumnName("ten_tag");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__thanh_to__3213E83F9EC113C3");

            entity.ToTable("thanh_toan");

            entity.HasIndex(e => new { e.DonHangId, e.TrangThai }, "IX_thanh_toan_don_hang_trang_thai");

            entity.HasIndex(e => e.MaGiaoDich, "UX_thanh_toan_ma_giao_dich")
                .IsUnique()
                .HasFilter("([ma_giao_dich] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonHangId).HasColumnName("don_hang_id");
            entity.Property(e => e.MaGiaoDich)
                .HasMaxLength(100)
                .HasColumnName("ma_giao_dich");
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(30)
                .HasColumnName("phuong_thuc");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("so_tien");
            entity.Property(e => e.ThoiGianGiaoDich).HasColumnName("thoi_gian_giao_dich");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.DonHang).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.DonHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__thanh_toa__don_h__7E02B4CC");
        });

        modelBuilder.Entity<ThuongHieu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__thuong_h__3213E83F3B43116B");

            entity.ToTable("thuong_hieu");

            entity.HasIndex(e => e.TenThuongHieu, "UQ__thuong_h__2A54B350EE892BC6").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.TenThuongHieu)
                .HasMaxLength(120)
                .HasColumnName("ten_thuong_hieu");
        });

        modelBuilder.Entity<TonKho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ton_kho__3213E83F443D4EC5");

            entity.ToTable("ton_kho");

            entity.HasIndex(e => new { e.BienTheId, e.KhoId }, "IX_ton_kho_bien_the");

            entity.HasIndex(e => new { e.KhoId, e.BienTheId }, "UQ_ton_kho").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BienTheId).HasColumnName("bien_the_id");
            entity.Property(e => e.KhoId).HasColumnName("kho_id");
            entity.Property(e => e.SoLuongGiuCho).HasColumnName("so_luong_giu_cho");
            entity.Property(e => e.SoLuongTon).HasColumnName("so_luong_ton");

            entity.HasOne(d => d.BienThe).WithMany(p => p.TonKhos)
                .HasForeignKey(d => d.BienTheId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ton_kho__bien_th__2A164134");

            entity.HasOne(d => d.Kho).WithMany(p => p.TonKhos)
                .HasForeignKey(d => d.KhoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ton_kho__kho_id__29221CFB");
        });

        modelBuilder.Entity<VSanPhamChiTiet>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_san_pham_chi_tiet");

            entity.Property(e => e.BienTheDangBan).HasColumnName("bien_the_dang_ban");
            entity.Property(e => e.BienTheId).HasColumnName("bien_the_id");
            entity.Property(e => e.CoTheBan).HasColumnName("co_the_ban");
            entity.Property(e => e.GiaBan)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("gia_ban");
            entity.Property(e => e.MaSanPham)
                .HasMaxLength(40)
                .HasColumnName("ma_san_pham");
            entity.Property(e => e.MaSize)
                .HasMaxLength(20)
                .HasColumnName("ma_size");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.Sku)
                .HasMaxLength(80)
                .HasColumnName("sku");
            entity.Property(e => e.TenChatLieu)
                .HasMaxLength(100)
                .HasColumnName("ten_chat_lieu");
            entity.Property(e => e.TenDanhMuc)
                .HasMaxLength(120)
                .HasColumnName("ten_danh_muc");
            entity.Property(e => e.TenMau)
                .HasMaxLength(50)
                .HasColumnName("ten_mau");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("ten_san_pham");
            entity.Property(e => e.TenThuongHieu)
                .HasMaxLength(120)
                .HasColumnName("ten_thuong_hieu");
            entity.Property(e => e.TongGiuCho).HasColumnName("tong_giu_cho");
            entity.Property(e => e.TongTon).HasColumnName("tong_ton");
            entity.Property(e => e.TrangThaiSanPham)
                .HasMaxLength(20)
                .HasColumnName("trang_thai_san_pham");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__vai_tro__3213E83F75898AA5");

            entity.ToTable("vai_tro");

            entity.HasIndex(e => e.MaVaiTro, "UQ__vai_tro__4AE1754C01D5150D").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaVaiTro)
                .HasMaxLength(50)
                .HasColumnName("ma_vai_tro");
            entity.Property(e => e.TenVaiTro)
                .HasMaxLength(100)
                .HasColumnName("ten_vai_tro");

            entity.HasMany(d => d.Quyens).WithMany(p => p.VaiTros)
                .UsingEntity<Dictionary<string, object>>(
                    "VaiTroQuyen",
                    r => r.HasOne<Quyen>().WithMany()
                        .HasForeignKey("QuyenId")
                        .HasConstraintName("FK__vai_tro_q__quyen__47DBAE45"),
                    l => l.HasOne<VaiTro>().WithMany()
                        .HasForeignKey("VaiTroId")
                        .HasConstraintName("FK__vai_tro_q__vai_t__46E78A0C"),
                    j =>
                    {
                        j.HasKey("VaiTroId", "QuyenId");
                        j.ToTable("vai_tro_quyen");
                        j.IndexerProperty<int>("VaiTroId").HasColumnName("vai_tro_id");
                        j.IndexerProperty<int>("QuyenId").HasColumnName("quyen_id");
                    });
        });

        modelBuilder.Entity<XacThucTaiKhoan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__xac_thuc__3213E83FFA51478D");

            entity.ToTable("xac_thuc_tai_khoan");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DaSuDung).HasColumnName("da_su_dung");
            entity.Property(e => e.HetHan).HasColumnName("het_han");
            entity.Property(e => e.MaXacThucHash)
                .HasMaxLength(255)
                .HasColumnName("ma_xac_thuc_hash");
            entity.Property(e => e.MucDich)
                .HasMaxLength(30)
                .HasColumnName("muc_dich");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.XacThucTaiKhoans)
                .HasForeignKey(d => d.NguoiDungId)
                .HasConstraintName("FK__xac_thuc___nguoi__59FA5E80");
        });

        modelBuilder.Entity<YeuCauHuyDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__yeu_cau___3213E83FB583BFB6");

            entity.ToTable("yeu_cau_huy_don");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonHangId).HasColumnName("don_hang_id");
            entity.Property(e => e.LyDo)
                .HasMaxLength(500)
                .HasColumnName("ly_do");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.ThoiGianXuLy).HasColumnName("thoi_gian_xu_ly");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Cho_duyet")
                .HasColumnName("trang_thai");
            entity.Property(e => e.XuLyBoi).HasColumnName("xu_ly_boi");

            entity.HasOne(d => d.DonHang).WithMany(p => p.YeuCauHuyDons)
                .HasForeignKey(d => d.DonHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__yeu_cau_h__don_h__03BB8E22");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.YeuCauHuyDonNguoiDungs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__yeu_cau_h__nguoi__04AFB25B");

            entity.HasOne(d => d.XuLyBoiNavigation).WithMany(p => p.YeuCauHuyDonXuLyBoiNavigations)
                .HasForeignKey(d => d.XuLyBoi)
                .HasConstraintName("FK__yeu_cau_h__xu_ly__05A3D694");
        });
        modelBuilder.Entity<SanPhamGoiYDto>().HasNoKey().ToView(null);
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
