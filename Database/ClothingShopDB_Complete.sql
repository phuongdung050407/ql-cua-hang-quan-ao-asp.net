/*
===============================================================================
HE THONG QUAN LY CUA HANG QUAN AO - CLOTHINGSHOPDB_COMPLETE
Nen tang: Microsoft SQL Server 2019/2022 - T-SQL
Phien ban: 1.0 - Co so du lieu day du 15 chuc nang + Seed Data goi y san pham
Luu y:
- Script tao database rieng ClothingShopDB_Complete de KHONG ghi de database cu.
- Chay toan bo file bang SSMS (Execute / F5).
===============================================================================
*/



USE master;
GO

-- Xoa database cu neu da ton tai
IF DB_ID(N'ClothingShopDB_Complete') IS NOT NULL
BEGIN
    ALTER DATABASE ClothingShopDB_Complete
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    DROP DATABASE ClothingShopDB_Complete;
END;
GO

-- Tao database moi
CREATE DATABASE ClothingShopDB_Complete;
GO

USE ClothingShopDB_Complete;
GO


/* 1. TAI KHOAN, VAI TRO, NHAN VIEN */
CREATE TABLE dbo.nguoi_dung(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    email NVARCHAR(255) NOT NULL,
    email_chuan AS LOWER(LTRIM(RTRIM(email))) PERSISTED,
    ten_dang_nhap NVARCHAR(100) NULL,
    mat_khau_hash NVARCHAR(255) NOT NULL,
    ho_ten NVARCHAR(200) NOT NULL,
    so_dien_thoai NVARCHAR(20) NULL,
    anh_dai_dien NVARCHAR(500) NULL,
    ngay_sinh DATE NULL,
    gioi_tinh NVARCHAR(20) NULL CHECK(gioi_tinh IN (N'Nam',N'Nu',N'Khac')),
    da_xac_thuc_email BIT NOT NULL DEFAULT 0,
    trang_thai NVARCHAR(20) NOT NULL DEFAULT N'Hoat_dong'
        CHECK(trang_thai IN (N'Hoat_dong',N'Khoa')),
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    thoi_gian_cap_nhat DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_nguoi_dung_email UNIQUE(email_chuan),
    CONSTRAINT UQ_nguoi_dung_ten_dang_nhap UNIQUE(ten_dang_nhap)
);
GO
CREATE TABLE dbo.vai_tro(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ma_vai_tro NVARCHAR(50) NOT NULL UNIQUE,
    ten_vai_tro NVARCHAR(100) NOT NULL
);
CREATE TABLE dbo.quyen(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ma_quyen NVARCHAR(100) NOT NULL UNIQUE,
    mo_ta NVARCHAR(255) NULL
);
CREATE TABLE dbo.vai_tro_quyen(
    vai_tro_id INT NOT NULL,
    quyen_id INT NOT NULL,
    CONSTRAINT PK_vai_tro_quyen PRIMARY KEY(vai_tro_id,quyen_id),
    FOREIGN KEY(vai_tro_id) REFERENCES dbo.vai_tro(id) ON DELETE CASCADE,
    FOREIGN KEY(quyen_id) REFERENCES dbo.quyen(id) ON DELETE CASCADE
);
CREATE TABLE dbo.nguoi_dung_vai_tro(
    nguoi_dung_id BIGINT NOT NULL,
    vai_tro_id INT NOT NULL,
    CONSTRAINT PK_nguoi_dung_vai_tro PRIMARY KEY(nguoi_dung_id,vai_tro_id),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id) ON DELETE CASCADE,
    FOREIGN KEY(vai_tro_id) REFERENCES dbo.vai_tro(id)
);
CREATE TABLE dbo.nhan_vien(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id BIGINT NOT NULL UNIQUE,
    ma_nhan_vien NVARCHAR(30) NOT NULL UNIQUE,
    chuc_vu NVARCHAR(100) NOT NULL,
    ngay_vao_lam DATE NOT NULL,
    luong DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(luong>=0),
    trang_thai NVARCHAR(20) NOT NULL DEFAULT N'Dang_lam'
        CHECK(trang_thai IN (N'Dang_lam',N'Tam_nghi',N'Da_nghi')),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id)
);
CREATE TABLE dbo.xac_thuc_tai_khoan(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id BIGINT NOT NULL,
    ma_xac_thuc_hash NVARCHAR(255) NOT NULL,
    muc_dich NVARCHAR(30) NOT NULL CHECK(muc_dich IN (N'Xac_thuc_email',N'Dat_lai_mat_khau')),
    het_han DATETIME2 NOT NULL,
    da_su_dung BIT NOT NULL DEFAULT 0,
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id) ON DELETE CASCADE
);
GO

/* 2. DIA CHI */
CREATE TABLE dbo.dia_chi(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id BIGINT NOT NULL,
    ten_nguoi_nhan NVARCHAR(150) NOT NULL,
    so_dien_thoai NVARCHAR(20) NOT NULL,
    dia_chi_chi_tiet NVARCHAR(300) NOT NULL,
    phuong_xa NVARCHAR(100) NULL,
    quan_huyen NVARCHAR(100) NULL,
    tinh_thanh NVARCHAR(100) NOT NULL,
    mac_dinh BIT NOT NULL DEFAULT 0,
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id) ON DELETE CASCADE
);
GO

/* 3. DANH MUC VA THUOC TINH */
CREATE TABLE dbo.danh_muc(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten_danh_muc NVARCHAR(120) NOT NULL UNIQUE,
    mo_ta NVARCHAR(500) NULL,
    trang_thai BIT NOT NULL DEFAULT 1,
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE TABLE dbo.thuong_hieu(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten_thuong_hieu NVARCHAR(120) NOT NULL UNIQUE,
    mo_ta NVARCHAR(500) NULL
);
CREATE TABLE dbo.chat_lieu(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten_chat_lieu NVARCHAR(100) NOT NULL UNIQUE
);
CREATE TABLE dbo.kich_thuoc(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ma_size NVARCHAR(20) NOT NULL UNIQUE,
    thu_tu INT NOT NULL DEFAULT 0
);
CREATE TABLE dbo.mau_sac(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten_mau NVARCHAR(50) NOT NULL UNIQUE,
    ma_hex CHAR(7) NULL
);
CREATE TABLE dbo.tag(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten_tag NVARCHAR(100) NOT NULL,
    loai NVARCHAR(30) NOT NULL CHECK(loai IN (N'Phong_cach',N'Mua',N'Doi_tuong',N'Chu_de')),
    CONSTRAINT UQ_tag UNIQUE(ten_tag,loai)
);
GO

/* 4. SAN PHAM, SKU, ANH, TAG */
CREATE TABLE dbo.san_pham(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ma_san_pham NVARCHAR(40) NOT NULL UNIQUE,
    ten_san_pham NVARCHAR(200) NOT NULL,
    danh_muc_id INT NOT NULL,
    thuong_hieu_id INT NULL,
    chat_lieu_id INT NULL,
    mo_ta NVARCHAR(MAX) NULL,
    gia_goc DECIMAL(18,2) NOT NULL CHECK(gia_goc>=0),
    gia_ban DECIMAL(18,2) NOT NULL CHECK(gia_ban>=0),
    trang_thai NVARCHAR(20) NOT NULL DEFAULT N'Dang_ban'
        CHECK(trang_thai IN (N'Dang_ban',N'An',N'Ngung_ban')),
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    thoi_gian_cap_nhat DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(danh_muc_id) REFERENCES dbo.danh_muc(id),
    FOREIGN KEY(thuong_hieu_id) REFERENCES dbo.thuong_hieu(id),
    FOREIGN KEY(chat_lieu_id) REFERENCES dbo.chat_lieu(id)
);
CREATE TABLE dbo.bien_the_san_pham(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    san_pham_id INT NOT NULL,
    sku NVARCHAR(80) NOT NULL UNIQUE,
    kich_thuoc_id INT NOT NULL,
    mau_sac_id INT NOT NULL,
    gia_ban DECIMAL(18,2) NOT NULL CHECK(gia_ban>=0),
    trang_thai BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_bien_the UNIQUE(san_pham_id,kich_thuoc_id,mau_sac_id),
    FOREIGN KEY(san_pham_id) REFERENCES dbo.san_pham(id),
    FOREIGN KEY(kich_thuoc_id) REFERENCES dbo.kich_thuoc(id),
    FOREIGN KEY(mau_sac_id) REFERENCES dbo.mau_sac(id)
);
CREATE TABLE dbo.san_pham_anh(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    san_pham_id INT NOT NULL,
    bien_the_id BIGINT NULL,
    url NVARCHAR(500) NOT NULL,
    la_anh_dai_dien BIT NOT NULL DEFAULT 0,
    FOREIGN KEY(san_pham_id) REFERENCES dbo.san_pham(id) ON DELETE CASCADE,
    FOREIGN KEY(bien_the_id) REFERENCES dbo.bien_the_san_pham(id)
);
CREATE TABLE dbo.san_pham_tag(
    san_pham_id INT NOT NULL,
    tag_id INT NOT NULL,
    PRIMARY KEY(san_pham_id,tag_id),
    FOREIGN KEY(san_pham_id) REFERENCES dbo.san_pham(id) ON DELETE CASCADE,
    FOREIGN KEY(tag_id) REFERENCES dbo.tag(id) ON DELETE CASCADE
);
GO

/* 5. GIO HANG */
CREATE TABLE dbo.gio_hang(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id BIGINT NOT NULL UNIQUE,
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id) ON DELETE CASCADE
);
CREATE TABLE dbo.gio_hang_chi_tiet(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    gio_hang_id BIGINT NOT NULL,
    bien_the_id BIGINT NOT NULL,
    so_luong INT NOT NULL CHECK(so_luong>0),
    thoi_gian_them DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_gio_hang_sku UNIQUE(gio_hang_id,bien_the_id),
    FOREIGN KEY(gio_hang_id) REFERENCES dbo.gio_hang(id) ON DELETE CASCADE,
    FOREIGN KEY(bien_the_id) REFERENCES dbo.bien_the_san_pham(id)
);
GO

/* 6. KHO, TON KHO, PHIEU NHAP */
CREATE TABLE dbo.kho(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ma_kho NVARCHAR(30) NOT NULL UNIQUE,
    ten_kho NVARCHAR(150) NOT NULL,
    dia_chi NVARCHAR(300) NULL,
    nguoi_quan_ly_id BIGINT NULL,
    trang_thai BIT NOT NULL DEFAULT 1,
    FOREIGN KEY(nguoi_quan_ly_id) REFERENCES dbo.nhan_vien(id)
);
CREATE TABLE dbo.ton_kho(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    kho_id INT NOT NULL,
    bien_the_id BIGINT NOT NULL,
    so_luong_ton INT NOT NULL DEFAULT 0 CHECK(so_luong_ton>=0),
    so_luong_giu_cho INT NOT NULL DEFAULT 0 CHECK(so_luong_giu_cho>=0),
    CONSTRAINT UQ_ton_kho UNIQUE(kho_id,bien_the_id),
    CONSTRAINT CK_ton_kho_giu_cho CHECK(so_luong_giu_cho<=so_luong_ton),
    FOREIGN KEY(kho_id) REFERENCES dbo.kho(id),
    FOREIGN KEY(bien_the_id) REFERENCES dbo.bien_the_san_pham(id)
);
CREATE TABLE dbo.phieu_nhap_kho(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ma_phieu NVARCHAR(40) NOT NULL UNIQUE,
    kho_id INT NOT NULL,
    nhan_vien_id BIGINT NOT NULL,
    nha_cung_cap NVARCHAR(200) NULL,
    ngay_nhap DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ghi_chu NVARCHAR(500) NULL,
    FOREIGN KEY(kho_id) REFERENCES dbo.kho(id),
    FOREIGN KEY(nhan_vien_id) REFERENCES dbo.nhan_vien(id)
);
CREATE TABLE dbo.chi_tiet_phieu_nhap(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    phieu_nhap_id BIGINT NOT NULL,
    bien_the_id BIGINT NOT NULL,
    so_luong INT NOT NULL CHECK(so_luong>0),
    don_gia DECIMAL(18,2) NOT NULL CHECK(don_gia>=0),
    thanh_tien AS (so_luong*don_gia) PERSISTED,
    FOREIGN KEY(phieu_nhap_id) REFERENCES dbo.phieu_nhap_kho(id) ON DELETE CASCADE,
    FOREIGN KEY(bien_the_id) REFERENCES dbo.bien_the_san_pham(id)
);
GO

/* 7. KHUYEN MAI */
CREATE TABLE dbo.ma_giam_gia(
    id INT IDENTITY(1,1) PRIMARY KEY,
    ma_voucher NVARCHAR(40) NOT NULL UNIQUE,
    mo_ta NVARCHAR(300) NULL,
    loai_giam NVARCHAR(20) NOT NULL CHECK(loai_giam IN (N'Phan_tram',N'Tien_mat')),
    gia_tri DECIMAL(18,2) NOT NULL CHECK(gia_tri>0),
    giam_toi_da DECIMAL(18,2) NULL CHECK(giam_toi_da IS NULL OR giam_toi_da>=0),
    don_toi_thieu DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(don_toi_thieu>=0),
    so_luong_phat_hanh INT NOT NULL CHECK(so_luong_phat_hanh>0),
    so_luong_da_dung INT NOT NULL DEFAULT 0 CHECK(so_luong_da_dung>=0),
    ngay_bat_dau DATETIME2 NOT NULL,
    ngay_ket_thuc DATETIME2 NOT NULL,
    trang_thai BIT NOT NULL DEFAULT 1,
    CHECK(ngay_ket_thuc>ngay_bat_dau),
    CHECK(loai_giam<>N'Phan_tram' OR gia_tri<=100),
    CHECK(so_luong_da_dung<=so_luong_phat_hanh)
);
CREATE TABLE dbo.khuyen_mai_san_pham(
    ma_giam_gia_id INT NOT NULL,
    san_pham_id INT NOT NULL,
    PRIMARY KEY(ma_giam_gia_id,san_pham_id),
    FOREIGN KEY(ma_giam_gia_id) REFERENCES dbo.ma_giam_gia(id) ON DELETE CASCADE,
    FOREIGN KEY(san_pham_id) REFERENCES dbo.san_pham(id) ON DELETE CASCADE
);
CREATE TABLE dbo.banner_khuyen_mai(
    id INT IDENTITY(1,1) PRIMARY KEY,
    tieu_de NVARCHAR(200) NOT NULL,
    hinh_anh_url NVARCHAR(500) NOT NULL,
    lien_ket_url NVARCHAR(500) NULL,
    thu_tu INT NOT NULL DEFAULT 0,
    trang_thai BIT NOT NULL DEFAULT 1,
    ngay_bat_dau DATETIME2 NULL,
    ngay_ket_thuc DATETIME2 NULL
);
GO

/* 8. DON HANG, CHI TIET, LICH SU */
CREATE TABLE dbo.don_hang(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ma_don_hang NVARCHAR(40) NOT NULL UNIQUE,
    nguoi_dung_id BIGINT NOT NULL,
    trang_thai NVARCHAR(20) NOT NULL DEFAULT N'Cho_xac_nhan'
        CHECK(trang_thai IN (N'Cho_xac_nhan',N'Da_xac_nhan',N'Dang_giao',N'Da_giao',N'Da_huy')),
    phuong_thuc_thanh_toan NVARCHAR(20) NOT NULL DEFAULT N'COD'
        CHECK(phuong_thuc_thanh_toan IN (N'COD',N'Chuyen_khoan',N'Vi_dien_tu')),
    trang_thai_thanh_toan NVARCHAR(20) NOT NULL DEFAULT N'Chua_thanh_toan'
        CHECK(trang_thai_thanh_toan IN (N'Chua_thanh_toan',N'Da_thanh_toan',N'Hoan_tien')),
    ten_nguoi_nhan NVARCHAR(150) NOT NULL,
    dien_thoai_nhan NVARCHAR(20) NOT NULL,
    dia_chi_giao NVARCHAR(500) NOT NULL,
    tien_hang DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(tien_hang>=0),
    phi_van_chuyen DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(phi_van_chuyen>=0),
    tien_giam DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(tien_giam>=0),
    tong_thanh_toan DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(tong_thanh_toan>=0),
    ma_giam_gia_id INT NULL,
    ghi_chu NVARCHAR(500) NULL,
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    thoi_gian_cap_nhat DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id),
    FOREIGN KEY(ma_giam_gia_id) REFERENCES dbo.ma_giam_gia(id)
);
CREATE TABLE dbo.don_hang_chi_tiet(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    don_hang_id BIGINT NOT NULL,
    bien_the_id BIGINT NOT NULL,
    ten_san_pham NVARCHAR(200) NOT NULL,
    sku NVARCHAR(80) NOT NULL,
    size NVARCHAR(20) NOT NULL,
    mau_sac NVARCHAR(50) NOT NULL,
    don_gia DECIMAL(18,2) NOT NULL CHECK(don_gia>=0),
    so_luong INT NOT NULL CHECK(so_luong>0),
    thanh_tien AS (don_gia*so_luong) PERSISTED,
    FOREIGN KEY(don_hang_id) REFERENCES dbo.don_hang(id) ON DELETE CASCADE,
    FOREIGN KEY(bien_the_id) REFERENCES dbo.bien_the_san_pham(id)
);
CREATE TABLE dbo.chi_tiet_giu_cho(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    don_hang_ct_id BIGINT NOT NULL,
    kho_id INT NOT NULL,
    bien_the_id BIGINT NOT NULL,
    so_luong INT NOT NULL CHECK(so_luong>0),
    CONSTRAINT UQ_chi_tiet_giu_cho UNIQUE(don_hang_ct_id,kho_id),
    FOREIGN KEY(don_hang_ct_id) REFERENCES dbo.don_hang_chi_tiet(id) ON DELETE CASCADE,
    FOREIGN KEY(kho_id,bien_the_id) REFERENCES dbo.ton_kho(kho_id,bien_the_id)
);
CREATE TABLE dbo.lich_su_don_hang(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    don_hang_id BIGINT NOT NULL,
    trang_thai_cu NVARCHAR(20) NULL,
    trang_thai_moi NVARCHAR(20) NOT NULL,
    ghi_chu NVARCHAR(500) NULL,
    nguoi_cap_nhat BIGINT NULL,
    thoi_gian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(don_hang_id) REFERENCES dbo.don_hang(id) ON DELETE CASCADE,
    FOREIGN KEY(nguoi_cap_nhat) REFERENCES dbo.nguoi_dung(id)
);
GO

/* 9. HOA DON, THANH TOAN, YEU CAU HUY */
CREATE TABLE dbo.hoa_don(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ma_hoa_don NVARCHAR(40) NOT NULL UNIQUE,
    don_hang_id BIGINT NOT NULL UNIQUE,
    ngay_lap DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    tong_tien DECIMAL(18,2) NOT NULL CHECK(tong_tien>=0),
    thue DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(thue>=0),
    FOREIGN KEY(don_hang_id) REFERENCES dbo.don_hang(id)
);
CREATE TABLE dbo.thanh_toan(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    don_hang_id BIGINT NOT NULL,
    ma_giao_dich NVARCHAR(100) NULL,
    phuong_thuc NVARCHAR(30) NOT NULL,
    so_tien DECIMAL(18,2) NOT NULL CHECK(so_tien>=0),
    trang_thai NVARCHAR(20) NOT NULL CHECK(trang_thai IN (N'Khoi_tao',N'Thanh_cong',N'That_bai',N'Hoan_tien')),
    thoi_gian_giao_dich DATETIME2 NULL,
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(don_hang_id) REFERENCES dbo.don_hang(id)
);
CREATE TABLE dbo.yeu_cau_huy_don(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    don_hang_id BIGINT NOT NULL,
    nguoi_dung_id BIGINT NOT NULL,
    ly_do NVARCHAR(500) NOT NULL,
    trang_thai NVARCHAR(20) NOT NULL DEFAULT N'Cho_duyet'
        CHECK(trang_thai IN (N'Cho_duyet',N'Chap_nhan',N'Tu_choi')),
    xu_ly_boi BIGINT NULL,
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    thoi_gian_xu_ly DATETIME2 NULL,
    FOREIGN KEY(don_hang_id) REFERENCES dbo.don_hang(id),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id),
    FOREIGN KEY(xu_ly_boi) REFERENCES dbo.nguoi_dung(id)
);
GO

/* 10. DANH GIA, ANH DANH GIA, PHAN HOI CUA SHOP */
CREATE TABLE dbo.danh_gia(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    san_pham_id INT NOT NULL,
    nguoi_dung_id BIGINT NOT NULL,
    don_hang_id BIGINT NOT NULL,
    so_sao TINYINT NOT NULL CHECK(so_sao BETWEEN 1 AND 5),
    noi_dung NVARCHAR(1000) NULL,
    trang_thai NVARCHAR(20) NOT NULL DEFAULT N'Cho_duyet'
        CHECK(trang_thai IN (N'Cho_duyet',N'Da_duyet',N'An')),
    phan_hoi_shop NVARCHAR(1000) NULL,
    thoi_gian_phan_hoi DATETIME2 NULL,
    thoi_gian_tao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_danh_gia UNIQUE(nguoi_dung_id,don_hang_id,san_pham_id),
    FOREIGN KEY(san_pham_id) REFERENCES dbo.san_pham(id),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id),
    FOREIGN KEY(don_hang_id) REFERENCES dbo.don_hang(id)
);
CREATE TABLE dbo.danh_gia_anh(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    danh_gia_id BIGINT NOT NULL,
    url NVARCHAR(500) NOT NULL,
    FOREIGN KEY(danh_gia_id) REFERENCES dbo.danh_gia(id) ON DELETE CASCADE
);
GO

/* 11. LICH SU XEM VA NHAT KY HE THONG - GOI Y SAN PHAM */
CREATE TABLE dbo.lich_su_xem_san_pham(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id BIGINT NOT NULL,
    san_pham_id INT NOT NULL,
    thoi_gian_xem DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id) ON DELETE CASCADE,
    FOREIGN KEY(san_pham_id) REFERENCES dbo.san_pham(id) ON DELETE CASCADE
);
CREATE TABLE dbo.nhat_ky_he_thong(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id BIGINT NULL,
    hanh_dong NVARCHAR(50) NOT NULL,
    doi_tuong NVARCHAR(100) NOT NULL,
    doi_tuong_id BIGINT NULL,
    noi_dung NVARCHAR(1000) NULL,
    thoi_gian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY(nguoi_dung_id) REFERENCES dbo.nguoi_dung(id)
);
GO

/* 12. INDEX */
CREATE INDEX IX_san_pham_ten ON dbo.san_pham(ten_san_pham);
CREATE INDEX IX_san_pham_danh_muc ON dbo.san_pham(danh_muc_id,trang_thai);
CREATE INDEX IX_bien_the_san_pham ON dbo.bien_the_san_pham(san_pham_id,trang_thai);
CREATE INDEX IX_ton_kho_bien_the ON dbo.ton_kho(bien_the_id,kho_id);
CREATE INDEX IX_don_hang_nguoi_dung_ngay ON dbo.don_hang(nguoi_dung_id,thoi_gian_tao DESC);
CREATE INDEX IX_don_hang_trang_thai ON dbo.don_hang(trang_thai,thoi_gian_tao);
CREATE INDEX IX_chi_tiet_don_hang ON dbo.don_hang_chi_tiet(don_hang_id);
CREATE INDEX IX_danh_gia_san_pham ON dbo.danh_gia(san_pham_id,trang_thai);
CREATE INDEX IX_lich_su_xem ON dbo.lich_su_xem_san_pham(nguoi_dung_id,thoi_gian_xem DESC);
GO

/* 13. VIEW */
CREATE OR ALTER VIEW dbo.v_san_pham_chi_tiet
AS
SELECT sp.id AS san_pham_id,sp.ma_san_pham,sp.ten_san_pham,
       dm.ten_danh_muc,th.ten_thuong_hieu,cl.ten_chat_lieu,
       bt.id AS bien_the_id,bt.sku,kt.ma_size,ms.ten_mau,bt.gia_ban,
       ISNULL(tk.so_luong_ton,0) AS tong_ton,
       ISNULL(tk.so_luong_giu_cho,0) AS tong_giu_cho,
       ISNULL(tk.so_luong_ton,0)-ISNULL(tk.so_luong_giu_cho,0) AS co_the_ban,
       sp.trang_thai AS trang_thai_san_pham,bt.trang_thai AS bien_the_dang_ban
FROM dbo.san_pham sp
JOIN dbo.danh_muc dm ON dm.id=sp.danh_muc_id
LEFT JOIN dbo.thuong_hieu th ON th.id=sp.thuong_hieu_id
LEFT JOIN dbo.chat_lieu cl ON cl.id=sp.chat_lieu_id
JOIN dbo.bien_the_san_pham bt ON bt.san_pham_id=sp.id
JOIN dbo.kich_thuoc kt ON kt.id=bt.kich_thuoc_id
JOIN dbo.mau_sac ms ON ms.id=bt.mau_sac_id
OUTER APPLY(
    SELECT SUM(t.so_luong_ton) so_luong_ton,SUM(t.so_luong_giu_cho) so_luong_giu_cho
    FROM dbo.ton_kho t WHERE t.bien_the_id=bt.id
) tk;
GO

/* 14. STORED PROCEDURES: TIM KIEM, BAO CAO, NHAP KHO, DAT HANG */
CREATE OR ALTER PROCEDURE dbo.sp_TimKiemSanPham
    @TuKhoa NVARCHAR(200)=NULL,
    @DanhMucID INT=NULL,
    @ThuongHieuID INT=NULL,
    @GiaTu DECIMAL(18,2)=NULL,
    @GiaDen DECIMAL(18,2)=NULL,
    @SizeID INT=NULL,
    @MauSacID INT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT v.*
    FROM dbo.v_san_pham_chi_tiet v
    JOIN dbo.san_pham sp ON sp.id=v.san_pham_id
    JOIN dbo.bien_the_san_pham bt ON bt.id=v.bien_the_id
    WHERE sp.trang_thai=N'Dang_ban' AND bt.trang_thai=1
      AND (@TuKhoa IS NULL OR sp.ten_san_pham LIKE N'%'+@TuKhoa+N'%' OR sp.ma_san_pham LIKE N'%'+@TuKhoa+N'%')
      AND (@DanhMucID IS NULL OR sp.danh_muc_id=@DanhMucID)
      AND (@ThuongHieuID IS NULL OR sp.thuong_hieu_id=@ThuongHieuID)
      AND (@GiaTu IS NULL OR bt.gia_ban>=@GiaTu)
      AND (@GiaDen IS NULL OR bt.gia_ban<=@GiaDen)
      AND (@SizeID IS NULL OR bt.kich_thuoc_id=@SizeID)
      AND (@MauSacID IS NULL OR bt.mau_sac_id=@MauSacID)
    ORDER BY v.ten_san_pham;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_BaoCaoTonKho
    @KhoID INT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT k.ma_kho,k.ten_kho,sp.ma_san_pham,sp.ten_san_pham,bt.sku,
           kt.ma_size,ms.ten_mau,tk.so_luong_ton,tk.so_luong_giu_cho,
           tk.so_luong_ton-tk.so_luong_giu_cho AS co_the_ban
    FROM dbo.ton_kho tk
    JOIN dbo.kho k ON k.id=tk.kho_id
    JOIN dbo.bien_the_san_pham bt ON bt.id=tk.bien_the_id
    JOIN dbo.san_pham sp ON sp.id=bt.san_pham_id
    JOIN dbo.kich_thuoc kt ON kt.id=bt.kich_thuoc_id
    JOIN dbo.mau_sac ms ON ms.id=bt.mau_sac_id
    WHERE @KhoID IS NULL OR k.id=@KhoID
    ORDER BY sp.ten_san_pham,bt.sku;
END;
GO

/* Nhap kho: cap nhat ton kho va chi tiet phieu nhap trong cung transaction */
CREATE OR ALTER PROCEDURE dbo.sp_NhapKho
    @PhieuNhapID BIGINT,
    @BienTheID BIGINT,
    @SoLuong INT,
    @DonGia DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @SoLuong<=0 OR @DonGia<0 THROW 50002,N'Du lieu nhap kho khong hop le.',1;
    BEGIN TRAN;
    BEGIN TRY
        DECLARE @KhoID INT;
        SELECT @KhoID=kho_id FROM dbo.phieu_nhap_kho WITH(UPDLOCK,HOLDLOCK)
        WHERE id=@PhieuNhapID;
        IF @KhoID IS NULL THROW 50003,N'Khong tim thay phieu nhap.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.bien_the_san_pham WHERE id=@BienTheID)
            THROW 50004,N'Bien the san pham khong ton tai.',1;

        INSERT dbo.chi_tiet_phieu_nhap(phieu_nhap_id,bien_the_id,so_luong,don_gia)
        VALUES(@PhieuNhapID,@BienTheID,@SoLuong,@DonGia);

        IF EXISTS(SELECT 1 FROM dbo.ton_kho WHERE kho_id=@KhoID AND bien_the_id=@BienTheID)
            UPDATE dbo.ton_kho SET so_luong_ton=so_luong_ton+@SoLuong
            WHERE kho_id=@KhoID AND bien_the_id=@BienTheID;
        ELSE
            INSERT dbo.ton_kho(kho_id,bien_the_id,so_luong_ton,so_luong_giu_cho)
            VALUES(@KhoID,@BienTheID,@SoLuong,0);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

/* Tao don tu gio hang: khoa dong ton kho, kiem tra du hang, tao chi tiet va giu cho */
CREATE OR ALTER PROCEDURE dbo.sp_TaoDonHangTuGioHang
    @NguoiDungID BIGINT,
    @TenNguoiNhan NVARCHAR(150),
    @DienThoai NVARCHAR(20),
    @DiaChiGiao NVARCHAR(500),
    @PhuongThuc NVARCHAR(20)=N'COD',
    @PhiVanChuyen DECIMAL(18,2)=0,
    @DonHangID BIGINT OUTPUT,
    @MaVoucher NVARCHAR(40)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @NguoiDungID IS NULL OR @TenNguoiNhan IS NULL OR LTRIM(RTRIM(@TenNguoiNhan))=N''
       OR @DienThoai IS NULL OR LTRIM(RTRIM(@DienThoai))=N''
       OR @DiaChiGiao IS NULL OR LTRIM(RTRIM(@DiaChiGiao))=N''
        THROW 50005,N'Thong tin nguoi nhan khong duoc de trong.',1;
    IF @PhiVanChuyen IS NULL OR @PhiVanChuyen<0
        THROW 50005,N'Phi van chuyen khong hop le.',1;
    IF @PhuongThuc IS NULL OR @PhuongThuc NOT IN(N'COD',N'Chuyen_khoan',N'Vi_dien_tu')
        THROW 50006,N'Phuong thuc thanh toan khong hop le.',1;
    BEGIN TRAN;
    BEGIN TRY
        IF NOT EXISTS(
            SELECT 1 FROM dbo.nguoi_dung
            WHERE id=@NguoiDungID AND trang_thai=N'Hoat_dong'
        )
            THROW 50047,N'Tai khoan khong ton tai hoac dang bi khoa.',1;

        DECLARE @GioHangID BIGINT;
        SELECT @GioHangID=id FROM dbo.gio_hang WITH(UPDLOCK,HOLDLOCK)
        WHERE nguoi_dung_id=@NguoiDungID;
        IF @GioHangID IS NULL THROW 50007,N'Khong tim thay gio hang.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.gio_hang_chi_tiet WHERE gio_hang_id=@GioHangID)
            THROW 50008,N'Gio hang dang trong.',1;

        DECLARE @Hang TABLE(bien_the_id BIGINT PRIMARY KEY,so_luong INT,don_gia DECIMAL(18,2));
        INSERT @Hang
        SELECT ghct.bien_the_id,SUM(ghct.so_luong),MAX(bt.gia_ban)
        FROM dbo.gio_hang_chi_tiet ghct
        JOIN dbo.bien_the_san_pham bt ON bt.id=ghct.bien_the_id AND bt.trang_thai=1
        JOIN dbo.san_pham sp ON sp.id=bt.san_pham_id AND sp.trang_thai=N'Dang_ban'
        WHERE ghct.gio_hang_id=@GioHangID
        GROUP BY ghct.bien_the_id;

        IF (SELECT COUNT(*) FROM @Hang)<>(SELECT COUNT(*) FROM dbo.gio_hang_chi_tiet WHERE gio_hang_id=@GioHangID)
            THROW 50009,N'Co san pham khong con kinh doanh.',1;

        DECLARE @KhoPhanBo TABLE(bien_the_id BIGINT,kho_id INT,so_luong INT,
            PRIMARY KEY(bien_the_id,kho_id));
        DECLARE @BT BIGINT,@Can INT,@Kho INT,@Co INT;
        DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT bien_the_id,so_luong FROM @Hang;
        OPEN cur; FETCH NEXT FROM cur INTO @BT,@Can;
        WHILE @@FETCH_STATUS=0
        BEGIN
            DECLARE @ConLai INT=@Can;
            DECLARE curKho CURSOR LOCAL FAST_FORWARD FOR
                SELECT tk.kho_id,tk.so_luong_ton-tk.so_luong_giu_cho
                FROM dbo.ton_kho tk WITH(UPDLOCK,HOLDLOCK)
                JOIN dbo.kho k ON k.id=tk.kho_id AND k.trang_thai=1
                WHERE tk.bien_the_id=@BT AND tk.so_luong_ton>tk.so_luong_giu_cho
                ORDER BY tk.kho_id;
            OPEN curKho; FETCH NEXT FROM curKho INTO @Kho,@Co;
            WHILE @@FETCH_STATUS=0 AND @ConLai>0
            BEGIN
                DECLARE @Lay INT=CASE WHEN @Co<@ConLai THEN @Co ELSE @ConLai END;
                INSERT @KhoPhanBo VALUES(@BT,@Kho,@Lay);
                SET @ConLai=@ConLai-@Lay;
                FETCH NEXT FROM curKho INTO @Kho,@Co;
            END
            CLOSE curKho; DEALLOCATE curKho;
            IF @ConLai>0 THROW 50010,N'San pham khong du ton kho.',1;
            FETCH NEXT FROM cur INTO @BT,@Can;
        END
        CLOSE cur; DEALLOCATE cur;

        DECLARE @MaDon NVARCHAR(40)=N'DH'+CONVERT(NVARCHAR(8),GETDATE(),112)
            +REPLACE(CONVERT(NVARCHAR(8),GETDATE(),108),N':',N'')
            +RIGHT(REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N''),6);
        DECLARE @TienHang DECIMAL(18,2);
        DECLARE @TienGiam DECIMAL(18,2)=0;
        DECLARE @VoucherID INT=NULL;
        DECLARE @TienDuocGiam DECIMAL(18,2)=0;
        SELECT @TienHang=SUM(so_luong*don_gia) FROM @Hang;

        /* Kiem tra va giu luot su dung voucher trong cung transaction */
        IF @MaVoucher IS NOT NULL AND LTRIM(RTRIM(@MaVoucher))<>N''
        BEGIN
            SELECT @VoucherID=id
            FROM dbo.ma_giam_gia WITH(UPDLOCK,HOLDLOCK)
            WHERE ma_voucher=@MaVoucher
              AND trang_thai=1
              AND SYSDATETIME()>=ngay_bat_dau
              AND SYSDATETIME()<=ngay_ket_thuc
              AND so_luong_da_dung<so_luong_phat_hanh
              AND @TienHang>=don_toi_thieu;

            IF @VoucherID IS NULL
                THROW 50016,N'Ma giam gia khong hop le, het han hoac khong du dieu kien.',1;

            SELECT @TienDuocGiam=SUM(h.so_luong*h.don_gia)
            FROM @Hang h
            JOIN dbo.bien_the_san_pham bt ON bt.id=h.bien_the_id
            WHERE NOT EXISTS(
                SELECT 1 FROM dbo.khuyen_mai_san_pham km
                WHERE km.ma_giam_gia_id=@VoucherID
            )
            OR EXISTS(
                SELECT 1 FROM dbo.khuyen_mai_san_pham km
                WHERE km.ma_giam_gia_id=@VoucherID AND km.san_pham_id=bt.san_pham_id
            );

            SET @TienDuocGiam=ISNULL(@TienDuocGiam,0);
            IF @TienDuocGiam<=0
                THROW 50017,N'Voucher khong ap dung cho san pham trong gio hang.',1;

            SELECT @TienGiam=
                CASE WHEN loai_giam=N'Phan_tram'
                     THEN @TienDuocGiam*gia_tri/100.0
                     ELSE gia_tri END
            FROM dbo.ma_giam_gia WHERE id=@VoucherID;

            SELECT @TienGiam=CASE
                WHEN giam_toi_da IS NOT NULL AND @TienGiam>giam_toi_da THEN giam_toi_da
                ELSE @TienGiam END
            FROM dbo.ma_giam_gia WHERE id=@VoucherID;

            IF @TienGiam>@TienDuocGiam SET @TienGiam=@TienDuocGiam;

            UPDATE dbo.ma_giam_gia
            SET so_luong_da_dung=so_luong_da_dung+1
            WHERE id=@VoucherID AND trang_thai=1
              AND so_luong_da_dung<so_luong_phat_hanh;
            IF @@ROWCOUNT<>1
                THROW 50018,N'Voucher vua het luot su dung. Vui long thu lai.',1;
        END;

        INSERT dbo.don_hang(ma_don_hang,nguoi_dung_id,phuong_thuc_thanh_toan,
            ten_nguoi_nhan,dien_thoai_nhan,dia_chi_giao,tien_hang,phi_van_chuyen,
            tien_giam,tong_thanh_toan,ma_giam_gia_id)
        VALUES(@MaDon,@NguoiDungID,@PhuongThuc,@TenNguoiNhan,@DienThoai,@DiaChiGiao,
            @TienHang,@PhiVanChuyen,@TienGiam,@TienHang+@PhiVanChuyen-@TienGiam,@VoucherID);
        SET @DonHangID=SCOPE_IDENTITY();

        INSERT dbo.thanh_toan(don_hang_id,phuong_thuc,so_tien,trang_thai)
        VALUES(@DonHangID,@PhuongThuc,@TienHang+@PhiVanChuyen-@TienGiam,N'Khoi_tao');

        INSERT dbo.don_hang_chi_tiet(don_hang_id,bien_the_id,ten_san_pham,sku,size,mau_sac,don_gia,so_luong)
        SELECT @DonHangID,h.bien_the_id,sp.ten_san_pham,bt.sku,kt.ma_size,ms.ten_mau,h.don_gia,h.so_luong
        FROM @Hang h JOIN dbo.bien_the_san_pham bt ON bt.id=h.bien_the_id
        JOIN dbo.san_pham sp ON sp.id=bt.san_pham_id
        JOIN dbo.kich_thuoc kt ON kt.id=bt.kich_thuoc_id
        JOIN dbo.mau_sac ms ON ms.id=bt.mau_sac_id;

        INSERT dbo.chi_tiet_giu_cho(don_hang_ct_id,kho_id,bien_the_id,so_luong)
        SELECT ct.id,p.kho_id,p.bien_the_id,p.so_luong
        FROM @KhoPhanBo p
        JOIN dbo.don_hang_chi_tiet ct
          ON ct.don_hang_id=@DonHangID AND ct.bien_the_id=p.bien_the_id;

        UPDATE tk SET so_luong_giu_cho=tk.so_luong_giu_cho+p.so_luong
        FROM dbo.ton_kho tk
        JOIN @KhoPhanBo p ON p.kho_id=tk.kho_id AND p.bien_the_id=tk.bien_the_id;

        DELETE dbo.gio_hang_chi_tiet WHERE gio_hang_id=@GioHangID;
        INSERT dbo.lich_su_don_hang(don_hang_id,trang_thai_cu,trang_thai_moi,ghi_chu)
        VALUES(@DonHangID,NULL,N'Cho_xac_nhan',N'Tao don tu gio hang');
        COMMIT;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local','curKho')>=-1 BEGIN TRY CLOSE curKho; DEALLOCATE curKho; END TRY BEGIN CATCH END CATCH;
        IF CURSOR_STATUS('local','cur')>=-1 BEGIN TRY CLOSE cur; DEALLOCATE cur; END TRY BEGIN CATCH END CATCH;
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

/* Cap nhat trang thai don */
CREATE OR ALTER PROCEDURE dbo.sp_CapNhatTrangThaiDon
    @DonHangID BIGINT,
    @TrangThaiMoi NVARCHAR(20),
    @NguoiCapNhat BIGINT=NULL,
    @GhiChu NVARCHAR(500)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
    BEGIN TRY
        DECLARE @Cu NVARCHAR(20);
        SELECT @Cu=trang_thai FROM dbo.don_hang WITH(UPDLOCK,HOLDLOCK) WHERE id=@DonHangID;
        IF @Cu IS NULL THROW 50011,N'Khong tim thay don hang.',1;
        IF @TrangThaiMoi NOT IN(N'Cho_xac_nhan',N'Da_xac_nhan',N'Dang_giao',N'Da_giao',N'Da_huy')
            THROW 50012,N'Trang thai khong hop le.',1;
        IF @Cu IN(N'Da_giao',N'Da_huy') THROW 50013,N'Don hang da ket thuc, khong the chuyen trang thai.',1;

        IF NOT (
             (@Cu=N'Cho_xac_nhan' AND @TrangThaiMoi IN(N'Da_xac_nhan',N'Da_huy'))
          OR (@Cu=N'Da_xac_nhan' AND @TrangThaiMoi IN(N'Dang_giao',N'Da_huy'))
          OR (@Cu=N'Dang_giao' AND @TrangThaiMoi=N'Da_giao')
        )
            THROW 50014,N'Khong cho phep chuyen trang thai theo luong xu ly don hang.',1;

        IF @TrangThaiMoi=N'Da_giao'
        BEGIN
            IF EXISTS(
                SELECT 1 FROM dbo.don_hang dh
                WHERE dh.id=@DonHangID
                  AND dh.phuong_thuc_thanh_toan<>N'COD'
                  AND dh.trang_thai_thanh_toan<>N'Da_thanh_toan'
            )
                THROW 50019,N'Don online chua duoc xac nhan thanh toan thanh cong.',1;
            IF EXISTS(
                SELECT 1
                FROM dbo.chi_tiet_giu_cho gc
                JOIN dbo.ton_kho tk ON tk.kho_id=gc.kho_id AND tk.bien_the_id=gc.bien_the_id
                JOIN dbo.don_hang_chi_tiet ct ON ct.id=gc.don_hang_ct_id
                WHERE ct.don_hang_id=@DonHangID
                  AND (tk.so_luong_ton<gc.so_luong OR tk.so_luong_giu_cho<gc.so_luong)
            )
                THROW 50015,N'Ton kho hoac so luong giu cho khong hop le de hoan tat don.',1;

            UPDATE tk
            SET tk.so_luong_ton=tk.so_luong_ton-x.TongSoLuong,
                tk.so_luong_giu_cho=tk.so_luong_giu_cho-x.TongSoLuong
            FROM dbo.ton_kho tk
            JOIN(
                SELECT gc.kho_id,gc.bien_the_id,SUM(gc.so_luong) TongSoLuong
                FROM dbo.chi_tiet_giu_cho gc
                JOIN dbo.don_hang_chi_tiet ct ON ct.id=gc.don_hang_ct_id
                WHERE ct.don_hang_id=@DonHangID
                GROUP BY gc.kho_id,gc.bien_the_id
            ) x ON x.kho_id=tk.kho_id AND x.bien_the_id=tk.bien_the_id;

            DELETE gc
            FROM dbo.chi_tiet_giu_cho gc
            JOIN dbo.don_hang_chi_tiet ct ON ct.id=gc.don_hang_ct_id
            WHERE ct.don_hang_id=@DonHangID;

            UPDATE dbo.don_hang SET trang_thai_thanh_toan=N'Da_thanh_toan'
            WHERE id=@DonHangID AND phuong_thuc_thanh_toan=N'COD';

            UPDATE dbo.thanh_toan
            SET trang_thai=N'Thanh_cong',thoi_gian_giao_dich=SYSDATETIME()
            WHERE don_hang_id=@DonHangID AND phuong_thuc=N'COD'
              AND trang_thai=N'Khoi_tao';

            IF NOT EXISTS(SELECT 1 FROM dbo.hoa_don WHERE don_hang_id=@DonHangID)
            BEGIN
                DECLARE @MaHoaDon NVARCHAR(40)=N'HD'+CONVERT(NVARCHAR(8),GETDATE(),112)
                    +RIGHT(REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N''),10);
                INSERT dbo.hoa_don(ma_hoa_don,don_hang_id,tong_tien,thue)
                SELECT @MaHoaDon,id,tong_thanh_toan,0
                FROM dbo.don_hang
                WHERE id=@DonHangID AND trang_thai_thanh_toan=N'Da_thanh_toan';
            END
        END
        ELSE IF @TrangThaiMoi=N'Da_huy'
        BEGIN
            UPDATE tk
            SET tk.so_luong_giu_cho=tk.so_luong_giu_cho-x.TongSoLuong
            FROM dbo.ton_kho tk
            JOIN(
                SELECT gc.kho_id,gc.bien_the_id,SUM(gc.so_luong) TongSoLuong
                FROM dbo.chi_tiet_giu_cho gc
                JOIN dbo.don_hang_chi_tiet ct ON ct.id=gc.don_hang_ct_id
                WHERE ct.don_hang_id=@DonHangID
                GROUP BY gc.kho_id,gc.bien_the_id
            ) x ON x.kho_id=tk.kho_id AND x.bien_the_id=tk.bien_the_id;

            DELETE gc
            FROM dbo.chi_tiet_giu_cho gc
            JOIN dbo.don_hang_chi_tiet ct ON ct.id=gc.don_hang_ct_id
            WHERE ct.don_hang_id=@DonHangID;
        END

        UPDATE dbo.don_hang SET trang_thai=@TrangThaiMoi,thoi_gian_cap_nhat=SYSDATETIME()
        WHERE id=@DonHangID;
        INSERT dbo.lich_su_don_hang(don_hang_id,trang_thai_cu,trang_thai_moi,ghi_chu,nguoi_cap_nhat)
        VALUES(@DonHangID,@Cu,@TrangThaiMoi,@GhiChu,@NguoiCapNhat);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

/* ============================================================================
   PHAN BO SUNG / HARDENING - RANG BUOC, TRIGGER, BAO CAO, GOI Y, THANH TOAN
   ============================================================================ */
CREATE UNIQUE INDEX UX_dia_chi_mac_dinh
ON dbo.dia_chi(nguoi_dung_id)
WHERE mac_dinh=1;
GO

ALTER TABLE dbo.bien_the_san_pham
ADD CONSTRAINT UQ_bien_the_san_pham_id UNIQUE(san_pham_id,id);
GO
ALTER TABLE dbo.san_pham_anh
ADD CONSTRAINT FK_san_pham_anh_bien_the_cung_san_pham
FOREIGN KEY(san_pham_id,bien_the_id)
REFERENCES dbo.bien_the_san_pham(san_pham_id,id);
GO

CREATE UNIQUE INDEX UX_thanh_toan_ma_giao_dich
ON dbo.thanh_toan(ma_giao_dich)
WHERE ma_giao_dich IS NOT NULL;
GO

CREATE INDEX IX_thanh_toan_don_hang_trang_thai
ON dbo.thanh_toan(don_hang_id,trang_thai);
CREATE INDEX IX_voucher_hieu_luc
ON dbo.ma_giam_gia(trang_thai,ngay_bat_dau,ngay_ket_thuc);
CREATE INDEX IX_lich_su_don_hang_don_thoi_gian
ON dbo.lich_su_don_hang(don_hang_id,thoi_gian DESC);
GO

CREATE OR ALTER TRIGGER dbo.trg_danh_gia_kiem_tra_don_hang
ON dbo.danh_gia
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(
        SELECT 1
        FROM inserted i
        LEFT JOIN dbo.don_hang dh ON dh.id=i.don_hang_id
        WHERE dh.id IS NULL
           OR dh.nguoi_dung_id<>i.nguoi_dung_id
           OR dh.trang_thai<>N'Da_giao'
           OR NOT EXISTS(
                SELECT 1 FROM dbo.don_hang_chi_tiet ct
                JOIN dbo.bien_the_san_pham bt ON bt.id=ct.bien_the_id
                WHERE ct.don_hang_id=i.don_hang_id
                  AND bt.san_pham_id=i.san_pham_id
           )
    )
    BEGIN
        THROW 50030,N'Chi duoc danh gia san pham da mua trong don hang da giao cua chinh ban.',1;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ThongKeSanPhamBanChay
    @TuNgay DATE,
    @DenNgay DATE,
    @Top INT=10
AS
BEGIN
    SET NOCOUNT ON;
    IF @TuNgay IS NULL OR @DenNgay IS NULL OR @DenNgay<@TuNgay
        THROW 50031,N'Khoang ngay thong ke khong hop le.',1;
    IF @Top IS NULL OR @Top<1 OR @Top>500
        THROW 50032,N'So luong Top phai tu 1 den 500.',1;

    SELECT TOP (@Top) ct.bien_the_id,ct.sku,ct.ten_san_pham,
           SUM(ct.so_luong) tong_so_luong,SUM(ct.thanh_tien) doanh_so
    FROM dbo.don_hang_chi_tiet ct
    JOIN dbo.don_hang dh ON dh.id=ct.don_hang_id
    WHERE dh.trang_thai=N'Da_giao'
      AND dh.trang_thai_thanh_toan=N'Da_thanh_toan'
      AND dh.thoi_gian_tao>=@TuNgay
      AND dh.thoi_gian_tao<DATEADD(DAY,1,@DenNgay)
    GROUP BY ct.bien_the_id,ct.sku,ct.ten_san_pham
    ORDER BY SUM(ct.so_luong) DESC,SUM(ct.thanh_tien) DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GoiYSanPham
    @NguoiDungID BIGINT,
    @SoLuong INT=8
AS
BEGIN
    SET NOCOUNT ON;
    IF @SoLuong IS NULL OR @SoLuong<1 OR @SoLuong>100
        THROW 50048,N'So luong goi y phai tu 1 den 100.',1;

    ;WITH DaXem AS(
        SELECT TOP (5) san_pham_id
        FROM dbo.lich_su_xem_san_pham
        WHERE nguoi_dung_id=@NguoiDungID
        ORDER BY thoi_gian_xem DESC
    ),
    DanhMucQuanTam AS(
        SELECT TOP (3) sp.danh_muc_id
        FROM DaXem x
        JOIN dbo.san_pham sp ON sp.id=x.san_pham_id
        GROUP BY sp.danh_muc_id
        ORDER BY COUNT(*) DESC
    )
    SELECT TOP (@SoLuong) sp.id,sp.ma_san_pham,sp.ten_san_pham,
           ISNULL(gia.gia_tham_khao,sp.gia_ban) AS gia_ban,
           dm.ten_danh_muc
    FROM dbo.san_pham sp
    JOIN dbo.danh_muc dm ON dm.id=sp.danh_muc_id
    OUTER APPLY(
        SELECT MIN(bt.gia_ban) AS gia_tham_khao
        FROM dbo.bien_the_san_pham bt
        WHERE bt.san_pham_id=sp.id AND bt.trang_thai=1
    ) gia
    WHERE sp.trang_thai=N'Dang_ban'
      AND NOT EXISTS(SELECT 1 FROM DaXem x WHERE x.san_pham_id=sp.id)
      AND (sp.danh_muc_id IN(SELECT danh_muc_id FROM DanhMucQuanTam)
           OR NOT EXISTS(SELECT 1 FROM DanhMucQuanTam))
    ORDER BY sp.thoi_gian_tao DESC;
END;
GO

/* Da sua loi dau cham phay (;) truoc THROW 50037 */
CREATE OR ALTER PROCEDURE dbo.sp_GhiNhanKetQuaThanhToan
    @DonHangID BIGINT,
    @MaGiaoDich NVARCHAR(100),
    @SoTien DECIMAL(18,2),
    @ThanhCong BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @DonHangID IS NULL OR @MaGiaoDich IS NULL OR LTRIM(RTRIM(@MaGiaoDich))=N''
       OR @SoTien IS NULL OR @SoTien<=0 OR @ThanhCong IS NULL
        THROW 50033,N'Du lieu ket qua thanh toan khong hop le.',1;

    BEGIN TRAN;
    BEGIN TRY
        DECLARE @TongTien DECIMAL(18,2), @TrangThaiDon NVARCHAR(20), @TrangThaiTT NVARCHAR(20);
        SELECT @TongTien=tong_thanh_toan,@TrangThaiDon=trang_thai,@TrangThaiTT=trang_thai_thanh_toan
        FROM dbo.don_hang WITH(UPDLOCK,HOLDLOCK)
        WHERE id=@DonHangID;

        IF @TongTien IS NULL THROW 50034,N'Khong tim thay don hang.',1;
        IF @TrangThaiDon=N'Da_huy' THROW 50035,N'Don hang da huy, khong the ghi nhan thanh toan.',1;
        IF ABS(@TongTien-@SoTien)>0.01 THROW 50036,N'So tien callback khong khop tong thanh toan don hang.',1;

        IF EXISTS(SELECT 1 FROM dbo.thanh_toan WHERE ma_giao_dich=@MaGiaoDich)
        BEGIN
            IF EXISTS(SELECT 1 FROM dbo.thanh_toan WHERE ma_giao_dich=@MaGiaoDich
                      AND don_hang_id=@DonHangID AND so_tien=@SoTien
                      AND trang_thai=CASE WHEN @ThanhCong=1 THEN N'Thanh_cong' ELSE N'That_bai' END)
            BEGIN
                COMMIT;
                RETURN;
            END;
            THROW 50037,N'Ma giao dich da duoc su dung cho giao dich khac.',1;
        END;

        IF @ThanhCong=1
        BEGIN
            IF @TrangThaiTT=N'Da_thanh_toan'
                THROW 50038,N'Don hang da duoc thanh toan truoc do.',1;

            INSERT dbo.thanh_toan(don_hang_id,ma_giao_dich,phuong_thuc,so_tien,trang_thai,thoi_gian_giao_dich)
            SELECT id,@MaGiaoDich,phuong_thuc_thanh_toan,@SoTien,N'Thanh_cong',SYSDATETIME()
            FROM dbo.don_hang WHERE id=@DonHangID;

            UPDATE dbo.don_hang
            SET trang_thai_thanh_toan=N'Da_thanh_toan',thoi_gian_cap_nhat=SYSDATETIME()
            WHERE id=@DonHangID;
        END
        ELSE
        BEGIN
            INSERT dbo.thanh_toan(don_hang_id,ma_giao_dich,phuong_thuc,so_tien,trang_thai,thoi_gian_giao_dich)
            SELECT id,@MaGiaoDich,phuong_thuc_thanh_toan,@SoTien,N'That_bai',SYSDATETIME()
            FROM dbo.don_hang WHERE id=@DonHangID;
        END;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_TaoYeuCauHuyDon
    @DonHangID BIGINT,
    @NguoiDungID BIGINT,
    @LyDo NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    IF @LyDo IS NULL OR LTRIM(RTRIM(@LyDo))=N''
        THROW 50039,N'Ly do huy don khong duoc de trong.',1;

    IF NOT EXISTS(
        SELECT 1 FROM dbo.don_hang
        WHERE id=@DonHangID AND nguoi_dung_id=@NguoiDungID
          AND trang_thai IN(N'Cho_xac_nhan',N'Da_xac_nhan')
    )
        THROW 50040,N'Don hang khong ton tai, khong thuoc tai khoan hoac khong con du dieu kien yeu cau huy.',1;

    IF EXISTS(SELECT 1 FROM dbo.yeu_cau_huy_don WHERE don_hang_id=@DonHangID AND trang_thai=N'Cho_duyet')
        THROW 50041,N'Don hang da co yeu cau huy dang cho duyet.',1;

    INSERT dbo.yeu_cau_huy_don(don_hang_id,nguoi_dung_id,ly_do)
    VALUES(@DonHangID,@NguoiDungID,@LyDo);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_XuLyYeuCauHuyDon
    @YeuCauID BIGINT,
    @NguoiXuLy BIGINT,
    @ChapNhan BIT,
    @GhiChu NVARCHAR(500)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
    BEGIN TRY
        DECLARE @DonHangID BIGINT,@TrangThaiYC NVARCHAR(20),@TrangThaiDon NVARCHAR(20);
        SELECT @DonHangID=don_hang_id,@TrangThaiYC=trang_thai
        FROM dbo.yeu_cau_huy_don WITH(UPDLOCK,HOLDLOCK)
        WHERE id=@YeuCauID;

        IF @DonHangID IS NULL THROW 50042,N'Khong tim thay yeu cau huy.',1;
        IF @TrangThaiYC<>N'Cho_duyet' THROW 50043,N'Yeu cau huy da duoc xu ly.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.nguoi_dung WHERE id=@NguoiXuLy AND trang_thai=N'Hoat_dong')
            THROW 50044,N'Tai khoan nguoi xu ly khong hop le.',1;

        SELECT @TrangThaiDon=trang_thai
        FROM dbo.don_hang WITH(UPDLOCK,HOLDLOCK)
        WHERE id=@DonHangID;

        IF @ChapNhan=1
        BEGIN
            IF @TrangThaiDon NOT IN(N'Cho_xac_nhan',N'Da_xac_nhan')
                THROW 50045,N'Don hang khong con du dieu kien huy.',1;

            EXEC dbo.sp_CapNhatTrangThaiDon
                @DonHangID=@DonHangID,
                @TrangThaiMoi=N'Da_huy',
                @NguoiCapNhat=@NguoiXuLy,
                @GhiChu=@GhiChu;
        END;

        UPDATE dbo.yeu_cau_huy_don
        SET trang_thai=CASE WHEN @ChapNhan=1 THEN N'Chap_nhan' ELSE N'Tu_choi' END,
            xu_ly_boi=@NguoiXuLy,thoi_gian_xu_ly=SYSDATETIME()
        WHERE id=@YeuCauID;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_BaoCaoDoanhThu
    @TuNgay DATE,
    @DenNgay DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF @TuNgay IS NULL OR @DenNgay IS NULL OR @DenNgay<@TuNgay
        THROW 50046,N'Khoang ngay bao cao khong hop le.',1;

    SELECT CAST(thoi_gian_tao AS DATE) ngay,
           COUNT(*) tong_don,
           SUM(CASE WHEN trang_thai=N'Da_giao' THEN 1 ELSE 0 END) don_thanh_cong,
           SUM(CASE WHEN trang_thai=N'Da_huy' THEN 1 ELSE 0 END) don_da_huy,
           SUM(CASE WHEN trang_thai=N'Da_giao' AND trang_thai_thanh_toan=N'Da_thanh_toan'
                    THEN tong_thanh_toan ELSE 0 END) doanh_thu
    FROM dbo.don_hang
    WHERE thoi_gian_tao>=@TuNgay AND thoi_gian_tao<DATEADD(DAY,1,@DenNgay)
    GROUP BY CAST(thoi_gian_tao AS DATE)
    ORDER BY ngay DESC;
END;
GO

/* ============================================================================
   15. SEED DATA CO BAN & DU LIEU MAU PHONG PHU CHO GOI Y SAN PHAM
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRAN;
BEGIN TRY
    /* Du lieu danh muc co ban */
    INSERT dbo.vai_tro(ma_vai_tro,ten_vai_tro) VALUES
    (N'ADMIN',N'Quản trị viên'),(N'NHAN_VIEN',N'Nhân viên'),(N'KHACH_HANG',N'Khách hàng'),
    (N'QL_KHO',N'Quản lý kho'),(N'QL_SAN_PHAM',N'Quản lý sản phẩm');

    INSERT dbo.quyen(ma_quyen,mo_ta) VALUES
    (N'product.view',N'Xem sản phẩm'),(N'product.manage',N'Quản lý sản phẩm'),
    (N'order.view',N'Xem đơn hàng'),(N'order.manage',N'Quản lý đơn hàng'),
    (N'stock.manage',N'Quản lý kho'),(N'user.manage',N'Quản lý người dùng'),
    (N'report.view',N'Xem báo cáo');

    INSERT dbo.vai_tro_quyen(vai_tro_id,quyen_id)
    SELECT v.id,q.id FROM dbo.vai_tro v CROSS JOIN dbo.quyen q WHERE v.ma_vai_tro=N'ADMIN';

    INSERT dbo.danh_muc(ten_danh_muc,mo_ta) VALUES
    (N'Áo thun',N'Áo thun nam nữ'),(N'Quần jean',N'Quần jean'),(N'Áo sơ mi',N'Áo sơ mi'),(N'Áo khoác',N'Áo khoác');

    INSERT dbo.thuong_hieu(ten_thuong_hieu,mo_ta) VALUES
    (N'Local Brand',N'Thương hiệu thời trang'),(N'Basic Wear',N'Thời trang cơ bản');

    INSERT dbo.chat_lieu(ten_chat_lieu) VALUES(N'Cotton'),(N'Denim'),(N'Polyester');
    INSERT dbo.kich_thuoc(ma_size,thu_tu) VALUES(N'S',1),(N'M',2),(N'L',3),(N'XL',4),(N'XXL',5);
    INSERT dbo.mau_sac(ten_mau,ma_hex) VALUES(N'Đen',N'#000000'),(N'Trắng',N'#FFFFFF'),(N'Xanh',N'#0000FF');
    INSERT dbo.kho(ma_kho,ten_kho,dia_chi) VALUES(N'KHO-01',N'Kho chính',N'Kho cửa hàng');
    INSERT dbo.tag(ten_tag,loai) VALUES(N'Đi làm',N'Phong_cach'),(N'Dạo phố',N'Phong_cach'),(N'Mùa hè',N'Mua');

    /* Tai khoan Admin, Nhan vien va 5 Khach hang mau */
    INSERT INTO dbo.nguoi_dung (email, ten_dang_nhap, mat_khau_hash, ho_ten, so_dien_thoai, gioi_tinh, da_xac_thuc_email)
    VALUES
    (N'admin@shop.vn',      N'admin',      N'$2a$11$DummyHashForSeedAdmin12345678901234567890123456', N'Quản Trị Hệ Thống', N'0901000001', N'Nam', 1),
    (N'nhanvien@shop.vn',   N'nhanvien1',  N'$2a$11$DummyHashForSeedStaff12345678901234567890123456', N'Trần Thị Thu Hà',   N'0901000002', N'Nu',  1),
    (N'khach1@gmail.com',   N'minhanh',    N'$2a$11$DummyHashForSeedUser112345678901234567890123456', N'Nguyễn Minh Anh',   N'0912000001', N'Nu',  1),
    (N'khach2@gmail.com',   N'hoangnam',   N'$2a$11$DummyHashForSeedUser212345678901234567890123456', N'Lê Hoàng Nam',      N'0912000002', N'Nam', 1),
    (N'khach3@gmail.com',   N'thaochi',    N'$2a$11$DummyHashForSeedUser312345678901234567890123456', N'Phạm Thảo Chi',     N'0912000003', N'Nu',  1),
    (N'khach4@gmail.com',   N'quocbao',    N'$2a$11$DummyHashForSeedUser412345678901234567890123456', N'Võ Quốc Bảo',       N'0912000004', N'Nam', 1),
    (N'khach5@gmail.com',   N'lanhuong',   N'$2a$11$DummyHashForSeedUser512345678901234567890123456', N'Đặng Lan Hương',    N'0912000005', N'Nu',  1);

    INSERT INTO dbo.nguoi_dung_vai_tro (nguoi_dung_id, vai_tro_id)
    SELECT nd.id, vt.id FROM dbo.nguoi_dung nd JOIN dbo.vai_tro vt ON vt.ma_vai_tro = N'ADMIN' WHERE nd.ten_dang_nhap = N'admin';

    INSERT INTO dbo.nguoi_dung_vai_tro (nguoi_dung_id, vai_tro_id)
    SELECT nd.id, vt.id FROM dbo.nguoi_dung nd JOIN dbo.vai_tro vt ON vt.ma_vai_tro = N'NHAN_VIEN' WHERE nd.ten_dang_nhap = N'nhanvien1';

    INSERT INTO dbo.nguoi_dung_vai_tro (nguoi_dung_id, vai_tro_id)
    SELECT nd.id, vt.id FROM dbo.nguoi_dung nd CROSS JOIN dbo.vai_tro vt
    WHERE nd.ten_dang_nhap IN (N'minhanh', N'hoangnam', N'thaochi', N'quocbao', N'lanhuong') AND vt.ma_vai_tro = N'KHACH_HANG';

    INSERT INTO dbo.nhan_vien (nguoi_dung_id, ma_nhan_vien, chuc_vu, ngay_vao_lam, luong)
    SELECT id, N'NV-001', N'Nhân viên bán hàng & Kho', '2025-06-01', 9500000
    FROM dbo.nguoi_dung WHERE ten_dang_nhap = N'nhanvien1';

    INSERT INTO dbo.dia_chi (nguoi_dung_id, ten_nguoi_nhan, so_dien_thoai, dia_chi_chi_tiet, phuong_xa, quan_huyen, tinh_thanh, mac_dinh)
    SELECT id, ho_ten, so_dien_thoai, N'123 Đường Nguyễn Huệ', N'Phường Bến Nghé', N'Quận 1', N'TP. Hồ Chí Minh', 1
    FROM dbo.nguoi_dung WHERE ten_dang_nhap IN (N'minhanh', N'hoangnam', N'thaochi', N'quocbao', N'lanhuong');

    /* 16 San pham trai deu 4 danh muc */
    DECLARE @dmAoThun INT = (SELECT id FROM dbo.danh_muc WHERE ten_danh_muc = N'Áo thun');
    DECLARE @dmQuanJean INT = (SELECT id FROM dbo.danh_muc WHERE ten_danh_muc = N'Quần jean');
    DECLARE @dmAoSoMi INT = (SELECT id FROM dbo.danh_muc WHERE ten_danh_muc = N'Áo sơ mi');
    DECLARE @dmAoKhoac INT = (SELECT id FROM dbo.danh_muc WHERE ten_danh_muc = N'Áo khoác');

    DECLARE @thLocal INT = (SELECT id FROM dbo.thuong_hieu WHERE ten_thuong_hieu = N'Local Brand');
    DECLARE @thBasic INT = (SELECT id FROM dbo.thuong_hieu WHERE ten_thuong_hieu = N'Basic Wear');

    DECLARE @clCotton INT = (SELECT id FROM dbo.chat_lieu WHERE ten_chat_lieu = N'Cotton');
    DECLARE @clDenim INT = (SELECT id FROM dbo.chat_lieu WHERE ten_chat_lieu = N'Denim');
    DECLARE @clPoly INT = (SELECT id FROM dbo.chat_lieu WHERE ten_chat_lieu = N'Polyester');

    INSERT INTO dbo.san_pham (ma_san_pham, ten_san_pham, danh_muc_id, thuong_hieu_id, chat_lieu_id, mo_ta, gia_goc, gia_ban)
    VALUES
    (N'SP-AT01', N'Áo Thun Cổ Tròn Basic Cotton 250gsm',      @dmAoThun,   @thBasic, @clCotton, N'Form rộng thoáng mát, thấm hút mồ hôi tốt.', 220000, 189000),
    (N'SP-AT02', N'Áo Thun Oversize In Hình Streetwear',      @dmAoThun,   @thLocal, @clCotton, N'Phong cách trẻ trung năng động dạo phố.',     290000, 249000),
    (N'SP-AT03', N'Áo Polo Nam Nữ Phối Cổ dệt Cao Cấp',        @dmAoThun,   @thBasic, @clCotton, N'Lịch sự, phù hợp đi làm và đi chơi.',         320000, 279000),
    (N'SP-AT04', N'Áo Thun Tay Lỡ Wash Loang Vintage',         @dmAoThun,   @thLocal, @clCotton, N'Màu wash cá tính, chất vải dày dặn.',         310000, 265000),

    (N'SP-QJ01', N'Quần Jean Nam Ống Đứng Straight Fit',       @dmQuanJean, @thBasic, @clDenim,  N'Denim co giãn nhẹ, tôn dáng dễ phối đồ.',     480000, 420000),
    (N'SP-QJ02', N'Quần Jean Ống Rộng Baggy Unisex',           @dmQuanJean, @thLocal, @clDenim,  N'Form baggy chuẩn streetwear Hàn Quốc.',       520000, 459000),
    (N'SP-QJ03', N'Quần Jean Đen Slimfit Không Phai Màu',      @dmQuanJean, @thBasic, @clDenim,  N'Vải jean đen tuyền bền màu sau nhiều lần giặt.',490000, 430000),
    (N'SP-QJ04', N'Quần Short Jean Rách Nhẹ Mùa Hè',           @dmQuanJean, @thLocal, @clDenim,  N'Năng động cho những chuyến du lịch hè.',      350000, 299000),

    (N'SP-SM01', N'Áo Sơ Mi Trắng Công Sở Chống Nhăn',         @dmAoSoMi,   @thBasic, @clCotton, N'Sợi cotton pha chống nhăn, đứng form.',       380000, 329000),
    (N'SP-SM02', N'Áo Sơ Mi Oxford Tay Dài Xanh Pastel',       @dmAoSoMi,   @thBasic, @clCotton, N'Chất vải Oxford mềm mại, thanh lịch.',        410000, 359000),
    (N'SP-SM03', N'Áo Sơ Mi Kẻ Sọc Form Rộng Hàn Quốc',        @dmAoSoMi,   @thLocal, @clCotton, N'Có thể mặc khoác ngoài áo thun basic.',       390000, 340000),
    (N'SP-SM04', N'Áo Sơ Mi Cuban Cổ Vest Ngắn Tay',           @dmAoSoMi,   @thLocal, @clPoly,   N'Vải lụa poly rũ nhẹ, thoáng mát mùa hè.',     340000, 289000),

    (N'SP-AK01', N'Áo Khoác Dù Chống Nắng Trượt Nước',         @dmAoKhoac,  @thLocal, @clPoly,   N'2 lớp dày dặn, chống tia UV và mưa nhẹ.',     450000, 389000),
    (N'SP-AK02', N'Áo Khoác Jean Denim Jacket Cổ Điển',        @dmAoKhoac,  @thLocal, @clDenim,  N'Áo khoác jean bụi bặm vượt thời gian.',       620000, 549000),
    (N'SP-AK03', N'Áo Khoác Bomber Nỉ Bông Phối Tay',          @dmAoKhoac,  @thLocal, @clCotton, N'Giữ ấm tốt, bo viền chắc chắn.',              580000, 499000),
    (N'SP-AK04', N'Áo Khoác Hoodie Zip Basic Unisex',          @dmAoKhoac,  @thBasic, @clCotton, N'Nỉ chân cua 380gsm không xù lông.',           490000, 429000);

    /* Bien the size/mau + Ton kho */
    DECLARE @khoChinh INT = (SELECT id FROM dbo.kho WHERE ma_kho = N'KHO-01');

    INSERT INTO dbo.bien_the_san_pham (san_pham_id, sku, kich_thuoc_id, mau_sac_id, gia_ban)
    SELECT sp.id,
           sp.ma_san_pham + N'-' + kt.ma_size + N'-' + CASE WHEN ms.ten_mau = N'Đen' THEN N'BLK' ELSE N'WHT' END,
           kt.id,
           ms.id,
           sp.gia_ban
    FROM dbo.san_pham sp
    CROSS JOIN dbo.kich_thuoc kt
    CROSS JOIN dbo.mau_sac ms
    WHERE kt.ma_size IN (N'M', N'L')
      AND ms.ten_mau IN (N'Đen', N'Trắng');

    INSERT INTO dbo.ton_kho (kho_id, bien_the_id, so_luong_ton, so_luong_giu_cho)
    SELECT @khoChinh, id, 100, 0 FROM dbo.bien_the_san_pham;

    INSERT INTO dbo.san_pham_anh (san_pham_id, url, la_anh_dai_dien)
    SELECT id, N'/images/products/' + LOWER(ma_san_pham) + N'.jpg', 1 FROM dbo.san_pham;

    INSERT INTO dbo.san_pham_tag (san_pham_id, tag_id)
    SELECT sp.id, t.id FROM dbo.san_pham sp CROSS JOIN dbo.tag t
    WHERE (sp.ma_san_pham LIKE N'SP-SM%' AND t.ten_tag = N'Đi làm')
       OR (sp.ma_san_pham LIKE N'SP-AT%' AND t.ten_tag = N'Dạo phố')
       OR (sp.ma_san_pham IN (N'SP-QJ04', N'SP-SM04') AND t.ten_tag = N'Mùa hè');

    /* Lich su xem san pham cho he thong goi y */
    DECLARE @u1 BIGINT = (SELECT id FROM dbo.nguoi_dung WHERE ten_dang_nhap = N'minhanh');
    DECLARE @u2 BIGINT = (SELECT id FROM dbo.nguoi_dung WHERE ten_dang_nhap = N'hoangnam');
    DECLARE @u3 BIGINT = (SELECT id FROM dbo.nguoi_dung WHERE ten_dang_nhap = N'thaochi');
    DECLARE @u4 BIGINT = (SELECT id FROM dbo.nguoi_dung WHERE ten_dang_nhap = N'quocbao');

    INSERT INTO dbo.lich_su_xem_san_pham (nguoi_dung_id, san_pham_id, thoi_gian_xem)
    SELECT @u1, id, DATEADD(MINUTE, -id*10, SYSDATETIME())
    FROM dbo.san_pham WHERE ma_san_pham IN (N'SP-AT01', N'SP-AT02', N'SP-SM01', N'SP-SM02');

    INSERT INTO dbo.lich_su_xem_san_pham (nguoi_dung_id, san_pham_id, thoi_gian_xem)
    SELECT @u2, id, DATEADD(MINUTE, -id*12, SYSDATETIME())
    FROM dbo.san_pham WHERE ma_san_pham IN (N'SP-QJ01', N'SP-QJ02', N'SP-AK01', N'SP-AK02');

    INSERT INTO dbo.lich_su_xem_san_pham (nguoi_dung_id, san_pham_id, thoi_gian_xem)
    SELECT @u3, id, DATEADD(MINUTE, -id*8, SYSDATETIME())
    FROM dbo.san_pham WHERE ma_san_pham IN (N'SP-SM01', N'SP-SM02', N'SP-SM03');

    /* 12 Don hang da giao & Danh gia 5 sao */
    DECLARE @i INT = 1;
    DECLARE @curUser BIGINT, @outDonID BIGINT, @bt1 BIGINT, @bt2 BIGINT;

    WHILE @i <= 12
    BEGIN
        SET @curUser = CASE (@i % 4)
            WHEN 1 THEN @u1
            WHEN 2 THEN @u2
            WHEN 3 THEN @u3
            ELSE @u4
        END;

        SELECT TOP 1 @bt1 = id FROM dbo.bien_the_san_pham
        WHERE sku LIKE CASE WHEN @i % 2 = 1 THEN N'SP-AT01-L-BLK' ELSE N'SP-SM01-L-WHT' END;

        SELECT TOP 1 @bt2 = id FROM dbo.bien_the_san_pham
        WHERE sku LIKE CASE WHEN @i % 2 = 1 THEN N'SP-QJ01-L-BLK' ELSE N'SP-AK01-L-BLK' END;

        IF NOT EXISTS (SELECT 1 FROM dbo.gio_hang WHERE nguoi_dung_id = @curUser)
            INSERT INTO dbo.gio_hang (nguoi_dung_id) VALUES (@curUser);

        DECLARE @ghID BIGINT = (SELECT id FROM dbo.gio_hang WHERE nguoi_dung_id = @curUser);
        INSERT INTO dbo.gio_hang_chi_tiet (gio_hang_id, bien_the_id, so_luong) VALUES (@ghID, @bt1, 1);
        INSERT INTO dbo.gio_hang_chi_tiet (gio_hang_id, bien_the_id, so_luong) VALUES (@ghID, @bt2, 1);

        EXEC dbo.sp_TaoDonHangTuGioHang
            @NguoiDungID = @curUser,
            @TenNguoiNhan = N'Khách Hàng Mẫu',
            @DienThoai = N'0912345678',
            @DiaChiGiao = N'123 Đường Nguyễn Huệ, Quận 1, TP.HCM',
            @PhuongThuc = N'COD',
            @PhiVanChuyen = 30000,
            @DonHangID = @outDonID OUTPUT;

        EXEC dbo.sp_CapNhatTrangThaiDon @DonHangID = @outDonID, @TrangThaiMoi = N'Da_xac_nhan', @GhiChu = N'Xác nhận tự động dữ liệu mẫu';
        EXEC dbo.sp_CapNhatTrangThaiDon @DonHangID = @outDonID, @TrangThaiMoi = N'Dang_giao',   @GhiChu = N'Bàn giao đơn vị vận chuyển';
        EXEC dbo.sp_CapNhatTrangThaiDon @DonHangID = @outDonID, @TrangThaiMoi = N'Da_giao',     @GhiChu = N'Giao thành công';

        UPDATE dbo.don_hang
        SET thoi_gian_tao = DATEADD(DAY, -@i, SYSDATETIME())
        WHERE id = @outDonID;

        SET @i = @i + 1;
    END;

    INSERT INTO dbo.danh_gia (san_pham_id, nguoi_dung_id, don_hang_id, so_sao, noi_dung, trang_thai)
    SELECT DISTINCT bt.san_pham_id, dh.nguoi_dung_id, dh.id, 5, N'Vải đẹp, mặc rất vừa vặn, giao hàng nhanh!', N'Da_duyet'
    FROM dbo.don_hang dh
    JOIN dbo.don_hang_chi_tiet ct ON ct.don_hang_id = dh.id
    JOIN dbo.bien_the_san_pham bt ON bt.id = ct.bien_the_id
    WHERE dh.trang_thai = N'Da_giao'
      AND dh.id <= 4;

    COMMIT;
    PRINT N'=== ĐÃ TẠO XONG TOÀN BỘ DATABASE CLOTHINGSHOPDB_COMPLETE VÀ DỮ LIỆU MẪU ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO




USE ClothingShopDB_Complete;
GO

-- 1. Tạo tài khoản Admin
IF NOT EXISTS (
    SELECT 1 FROM dbo.nguoi_dung
    WHERE email = 'admin@fashion.vn'
)
BEGIN
    INSERT INTO dbo.nguoi_dung
        (email, ten_dang_nhap, mat_khau_hash, ho_ten)
    VALUES
        ('admin@fashion.vn', 'admin',
         'Admin123@', N'Quản trị viên');
END;
GO

-- 2. Tạo tài khoản Nhân viên
IF NOT EXISTS (
    SELECT 1 FROM dbo.nguoi_dung
    WHERE email = 'nhanvien@fashion.vn'
)
BEGIN
    INSERT INTO dbo.nguoi_dung
        (email, ten_dang_nhap, mat_khau_hash, ho_ten)
    VALUES
        ('nhanvien@fashion.vn', 'nhanvien',
         'Nhanvien123@', N'Nhân viên');
END;
GO

-- 3. Tạo tài khoản Khách hàng
IF NOT EXISTS (
    SELECT 1 FROM dbo.nguoi_dung
    WHERE email = 'khachhang@gmail.com'
)
BEGIN
    INSERT INTO dbo.nguoi_dung
        (email, ten_dang_nhap, mat_khau_hash, ho_ten)
    VALUES
        ('khachhang@gmail.com', 'khachhang',
         'Khachhang123@', N'Khách hàng');
END;
GO

-- Kiểm tra tài khoản đã tạo
SELECT id, email, ten_dang_nhap, ho_ten
FROM dbo.nguoi_dung
WHERE email IN (
    'admin@fashion.vn',
    'nhanvien@fashion.vn',
    'khachhang@gmail.com'
);
GO


UPDATE dbo.nguoi_dung
SET email = 'admin@fashion.vn',
    mat_khau_hash = 'Admin123@',
    ho_ten = N'Quản trị viên'
WHERE ten_dang_nhap = 'admin';