using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;

namespace ShopProject.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
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

    public virtual DbSet<SanPhamAnh> SanPhamAnhs { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<ThuongHieu> ThuongHieus { get; set; }

    public virtual DbSet<TonKho> TonKhos { get; set; }

    public virtual DbSet<VSanPhamChiTiet> VSanPhamChiTiets { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<XacThucTaiKhoan> XacThucTaiKhoans { get; set; }

    public virtual DbSet<YeuCauHuyDon> YeuCauHuyDons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LAPTOP-1I777SIS;Initial Catalog=ClothingShopDB_Complete;Integrated Security=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BannerKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__banner_k__3213E83F92057457");

            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<BienTheSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__bien_the__3213E83F01C71807");

            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.KichThuoc).WithMany(p => p.BienTheSanPhams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__bien_the___kich___18EBB532");

            entity.HasOne(d => d.MauSac).WithMany(p => p.BienTheSanPhams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__bien_the___mau_s__19DFD96B");

            entity.HasOne(d => d.SanPham).WithMany(p => p.BienTheSanPhams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__bien_the___san_p__17F790F9");
        });

        modelBuilder.Entity<ChatLieu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chat_lie__3213E83FA9A0ECD0");
        });

        modelBuilder.Entity<ChiTietGiuCho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chi_tiet__3213E83FB2E9CD37");

            entity.HasOne(d => d.DonHangCt).WithMany(p => p.ChiTietGiuChos).HasConstraintName("FK__chi_tiet___don_h__7C1A6C5A");

            entity.HasOne(d => d.TonKho).WithMany(p => p.ChiTietGiuChos)
                .HasPrincipalKey(p => new { p.KhoId, p.BienTheId })
                .HasForeignKey(d => new { d.KhoId, d.BienTheId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__chi_tiet_giu_cho__7D0E9093");
        });

        modelBuilder.Entity<ChiTietPhieuNhap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chi_tiet__3213E83F60D329BE");

            entity.Property(e => e.ThanhTien).HasComputedColumnSql("([so_luong]*[don_gia])", true);

            entity.HasOne(d => d.BienThe).WithMany(p => p.ChiTietPhieuNhaps)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__chi_tiet___bien___47A6A41B");

            entity.HasOne(d => d.PhieuNhap).WithMany(p => p.ChiTietPhieuNhaps).HasConstraintName("FK__chi_tiet___phieu__46B27FE2");
        });

        modelBuilder.Entity<DanhGiaAnh>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__danh_gia__3213E83F588C8C09");

            entity.HasOne(d => d.DanhGia).WithMany(p => p.DanhGiaAnhs).HasConstraintName("FK__danh_gia___danh___24285DB4");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__danh_gia__3213E83F8546C372");

            entity.ToTable("danh_gia", tb => tb.HasTrigger("trg_danh_gia_kiem_tra_don_hang"));

            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TrangThai).HasDefaultValue("Cho_duyet");

            entity.HasOne(d => d.DonHang).WithMany(p => p.DanhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__danh_gia__don_ha__214BF109");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.DanhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__danh_gia__nguoi___2057CCD0");

            entity.HasOne(d => d.SanPham).WithMany(p => p.DanhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__danh_gia__san_ph__1F63A897");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__danh_muc__3213E83F7B1664B8");

            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<DiaChi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__dia_chi__3213E83F153351CC");

            entity.HasIndex(e => e.NguoiDungId, "UX_dia_chi_mac_dinh")
                .IsUnique()
                .HasFilter("([mac_dinh]=(1))");

            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.NguoiDung).WithOne(p => p.DiaChi).HasConstraintName("FK__dia_chi__nguoi_d__70DDC3D8");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__don_hang__3213E83F732A1688");

            entity.Property(e => e.PhuongThucThanhToan).HasDefaultValue("COD");
            entity.Property(e => e.ThoiGianCapNhat).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TrangThai).HasDefaultValue("Cho_xac_nhan");
            entity.Property(e => e.TrangThaiThanhToan).HasDefaultValue("Chua_thanh_toan");

            entity.HasOne(d => d.MaGiamGia).WithMany(p => p.DonHangs).HasConstraintName("FK__don_hang__ma_gia__719CDDE7");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.DonHangs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__don_hang__nguoi___70A8B9AE");
        });

        modelBuilder.Entity<DonHangChiTiet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__don_hang__3213E83F9D54512B");

            entity.Property(e => e.ThanhTien).HasComputedColumnSql("([don_gia]*[so_luong])", true);

            entity.HasOne(d => d.BienThe).WithMany(p => p.DonHangChiTiets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__don_hang___bien___7755B73D");

            entity.HasOne(d => d.DonHang).WithMany(p => p.DonHangChiTiets).HasConstraintName("FK__don_hang___don_h__76619304");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__gio_hang__3213E83F0453A349");

            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.NguoiDung).WithOne(p => p.GioHang).HasConstraintName("FK__gio_hang__nguoi___2739D489");
        });

        modelBuilder.Entity<GioHangChiTiet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__gio_hang__3213E83F2CCFEFCC");

            entity.Property(e => e.ThoiGianThem).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.BienThe).WithMany(p => p.GioHangChiTiets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__gio_hang___bien___2DE6D218");

            entity.HasOne(d => d.GioHang).WithMany(p => p.GioHangChiTiets).HasConstraintName("FK__gio_hang___gio_h__2CF2ADDF");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__hoa_don__3213E83F7619A79D");

            entity.Property(e => e.NgayLap).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.DonHang).WithOne(p => p.HoaDon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__hoa_don__don_han__0A688BB1");
        });

        modelBuilder.Entity<Kho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__kho__3213E83FC01A5DCB");

            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.NguoiQuanLy).WithMany(p => p.Khos).HasConstraintName("FK__kho__nguoi_quan___32AB8735");
        });

        modelBuilder.Entity<KichThuoc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__kich_thu__3213E83F18A402DC");
        });

        modelBuilder.Entity<LichSuDonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lich_su___3213E83F47897CCB");

            entity.Property(e => e.ThoiGian).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.DonHang).WithMany(p => p.LichSuDonHangs).HasConstraintName("FK__lich_su_d__don_h__00DF2177");

            entity.HasOne(d => d.NguoiCapNhatNavigation).WithMany(p => p.LichSuDonHangs).HasConstraintName("FK__lich_su_d__nguoi__01D345B0");
        });

        modelBuilder.Entity<LichSuXemSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lich_su___3213E83F343C8F9E");

            entity.Property(e => e.ThoiGianXem).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.LichSuXemSanPhams).HasConstraintName("FK__lich_su_x__nguoi__27F8EE98");

            entity.HasOne(d => d.SanPham).WithMany(p => p.LichSuXemSanPhams).HasConstraintName("FK__lich_su_x__san_p__28ED12D1");
        });

        modelBuilder.Entity<MaGiamGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ma_giam___3213E83F3B32E3F8");

            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasMany(d => d.SanPhams).WithMany(p => p.MaGiamGia)
                .UsingEntity<Dictionary<string, object>>(
                    "KhuyenMaiSanPham",
                    r => r.HasOne<SanPham>().WithMany()
                        .HasForeignKey("SanPhamId")
                        .HasConstraintName("FK__khuyen_ma__san_p__59C55456"),
                    l => l.HasOne<MaGiamGium>().WithMany()
                        .HasForeignKey("MaGiamGiaId")
                        .HasConstraintName("FK__khuyen_ma__ma_gi__58D1301D"),
                    j =>
                    {
                        j.HasKey("MaGiamGiaId", "SanPhamId").HasName("PK__khuyen_m__A7F84C1326718085");
                        j.ToTable("khuyen_mai_san_pham");
                        j.IndexerProperty<int>("MaGiamGiaId").HasColumnName("ma_giam_gia_id");
                        j.IndexerProperty<int>("SanPhamId").HasColumnName("san_pham_id");
                    });
        });

        modelBuilder.Entity<MauSac>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__mau_sac__3213E83FEE003D4C");

            entity.Property(e => e.MaHex).IsFixedLength();
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nguoi_du__3213E83FD908C4D7");

            entity.Property(e => e.EmailChuan).HasComputedColumnSql("(lower(ltrim(rtrim([email]))))", true);
            entity.Property(e => e.ThoiGianCapNhat).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TrangThai).HasDefaultValue("Hoat_dong");

            entity.HasMany(d => d.VaiTros).WithMany(p => p.NguoiDungs)
                .UsingEntity<Dictionary<string, object>>(
                    "NguoiDungVaiTro",
                    r => r.HasOne<VaiTro>().WithMany()
                        .HasForeignKey("VaiTroId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__nguoi_dun__vai_t__5DCAEF64"),
                    l => l.HasOne<NguoiDung>().WithMany()
                        .HasForeignKey("NguoiDungId")
                        .HasConstraintName("FK__nguoi_dun__nguoi__5CD6CB2B"),
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
            entity.HasKey(e => e.Id).HasName("PK__nhan_vie__3213E83FD2C07661");

            entity.Property(e => e.TrangThai).HasDefaultValue("Dang_lam");

            entity.HasOne(d => d.NguoiDung).WithOne(p => p.NhanVien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__nhan_vien__nguoi__66603565");
        });

        modelBuilder.Entity<NhatKyHeThong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nhat_ky___3213E83FA74B055D");

            entity.Property(e => e.ThoiGian).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.NhatKyHeThongs).HasConstraintName("FK__nhat_ky_h__nguoi__2CBDA3B5");
        });

        modelBuilder.Entity<PhieuNhapKho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__phieu_nh__3213E83F9D49DF57");

            entity.Property(e => e.NgayNhap).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Kho).WithMany(p => p.PhieuNhapKhos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__phieu_nha__kho_i__40F9A68C");

            entity.HasOne(d => d.NhanVien).WithMany(p => p.PhieuNhapKhos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__phieu_nha__nhan___41EDCAC5");
        });

        modelBuilder.Entity<Quyen>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__quyen__3213E83F06D19912");
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__san_pham__3213E83F04A9F711");

            entity.Property(e => e.ThoiGianCapNhat).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TrangThai).HasDefaultValue("Dang_ban");

            entity.HasOne(d => d.ChatLieu).WithMany(p => p.SanPhams).HasConstraintName("FK__san_pham__chat_l__114A936A");

            entity.HasOne(d => d.DanhMuc).WithMany(p => p.SanPhams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__san_pham__danh_m__0F624AF8");

            entity.HasOne(d => d.ThuongHieu).WithMany(p => p.SanPhams).HasConstraintName("FK__san_pham__thuong__10566F31");

            entity.HasMany(d => d.Tags).WithMany(p => p.SanPhams)
                .UsingEntity<Dictionary<string, object>>(
                    "SanPhamTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("FK__san_pham___tag_i__22751F6C"),
                    l => l.HasOne<SanPham>().WithMany()
                        .HasForeignKey("SanPhamId")
                        .HasConstraintName("FK__san_pham___san_p__2180FB33"),
                    j =>
                    {
                        j.HasKey("SanPhamId", "TagId").HasName("PK__san_pham__3919D760537E19A6");
                        j.ToTable("san_pham_tag");
                        j.IndexerProperty<int>("SanPhamId").HasColumnName("san_pham_id");
                        j.IndexerProperty<int>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<SanPhamAnh>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__san_pham__3213E83F13200BCE");

            entity.HasOne(d => d.BienThe).WithMany(p => p.SanPhamAnhBienThes).HasConstraintName("FK__san_pham___bien___1EA48E88");

            entity.HasOne(d => d.SanPham).WithMany(p => p.SanPhamAnhs).HasConstraintName("FK__san_pham___san_p__1DB06A4F");

            entity.HasOne(d => d.BienTheSanPham).WithMany(p => p.SanPhamAnhBienTheSanPhams)
                .HasPrincipalKey(p => new { p.SanPhamId, p.Id })
                .HasForeignKey(d => new { d.SanPhamId, d.BienTheId })
                .HasConstraintName("FK_san_pham_anh_bien_the_cung_san_pham");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tag__3213E83FA2F68015");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__thanh_to__3213E83F4A088CBB");

            entity.HasIndex(e => e.MaGiaoDich, "UX_thanh_toan_ma_giao_dich")
                .IsUnique()
                .HasFilter("([ma_giao_dich] IS NOT NULL)");

            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.DonHang).WithMany(p => p.ThanhToans)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__thanh_toa__don_h__10216507");
        });

        modelBuilder.Entity<ThuongHieu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__thuong_h__3213E83F24CB440F");
        });

        modelBuilder.Entity<TonKho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ton_kho__3213E83FB139871B");

            entity.HasOne(d => d.BienThe).WithMany(p => p.TonKhos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ton_kho__bien_th__3C34F16F");

            entity.HasOne(d => d.Kho).WithMany(p => p.TonKhos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ton_kho__kho_id__3B40CD36");
        });

        modelBuilder.Entity<VSanPhamChiTiet>(entity =>
        {
            entity.ToView("v_san_pham_chi_tiet");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__vai_tro__3213E83F00C40ACF");

            entity.HasMany(d => d.Quyens).WithMany(p => p.VaiTros)
                .UsingEntity<Dictionary<string, object>>(
                    "VaiTroQuyen",
                    r => r.HasOne<Quyen>().WithMany()
                        .HasForeignKey("QuyenId")
                        .HasConstraintName("FK__vai_tro_q__quyen__59FA5E80"),
                    l => l.HasOne<VaiTro>().WithMany()
                        .HasForeignKey("VaiTroId")
                        .HasConstraintName("FK__vai_tro_q__vai_t__59063A47"),
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
            entity.HasKey(e => e.Id).HasName("PK__xac_thuc__3213E83F175007B5");

            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.XacThucTaiKhoans).HasConstraintName("FK__xac_thuc___nguoi__6C190EBB");
        });

        modelBuilder.Entity<YeuCauHuyDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__yeu_cau___3213E83F3B7C38B1");

            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TrangThai).HasDefaultValue("Cho_duyet");

            entity.HasOne(d => d.DonHang).WithMany(p => p.YeuCauHuyDons)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__yeu_cau_h__don_h__15DA3E5D");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.YeuCauHuyDonNguoiDungs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__yeu_cau_h__nguoi__16CE6296");

            entity.HasOne(d => d.XuLyBoiNavigation).WithMany(p => p.YeuCauHuyDonXuLyBoiNavigations).HasConstraintName("FK__yeu_cau_h__xu_ly__17C286CF");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
