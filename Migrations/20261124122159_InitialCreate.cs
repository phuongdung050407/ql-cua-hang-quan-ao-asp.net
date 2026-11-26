using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopProject.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banner_khuyen_mai",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tieu_de = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    hinh_anh_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    lien_ket_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    thu_tu = table.Column<int>(type: "int", nullable: false),
                    trang_thai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ngay_bat_dau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ngay_ket_thuc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__banner_k__3213E83F586E0625", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_lieu",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_chat_lieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__chat_lie__3213E83FB069870F", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "danh_muc",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_danh_muc = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    mo_ta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    trang_thai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__danh_muc__3213E83F8FBC6C91", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kich_thuoc",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_size = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    thu_tu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__kich_thu__3213E83F1392B676", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ma_giam_gia",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_voucher = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    mo_ta = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    loai_giam = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    gia_tri = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    giam_toi_da = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    don_toi_thieu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    so_luong_phat_hanh = table.Column<int>(type: "int", nullable: false),
                    so_luong_da_dung = table.Column<int>(type: "int", nullable: false),
                    ngay_bat_dau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ngay_ket_thuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    trang_thai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ma_giam___3213E83F979858DA", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mau_sac",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_mau = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ma_hex = table.Column<string>(type: "char(7)", unicode: false, fixedLength: true, maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__mau_sac__3213E83FA522EEAE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nguoi_dung",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email_chuan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, computedColumnSql: "(lower(ltrim(rtrim([email]))))", stored: true),
                    ten_dang_nhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    mat_khau_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ho_ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    so_dien_thoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    anh_dai_dien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ngay_sinh = table.Column<DateOnly>(type: "date", nullable: true),
                    gioi_tinh = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    da_xac_thuc_email = table.Column<bool>(type: "bit", nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Hoat_dong"),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    thoi_gian_cap_nhat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__nguoi_du__3213E83FD064C01B", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quyen",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_quyen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mo_ta = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__quyen__3213E83FA0230FBE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tag",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    loai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tag__3213E83F6F524D2B", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "thuong_hieu",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_thuong_hieu = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    mo_ta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__thuong_h__3213E83F3B43116B", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vai_tro",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_vai_tro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ten_vai_tro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__vai_tro__3213E83F75898AA5", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dia_chi",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    ten_nguoi_nhan = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    so_dien_thoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    dia_chi_chi_tiet = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    phuong_xa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    quan_huyen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tinh_thanh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mac_dinh = table.Column<bool>(type: "bit", nullable: false),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__dia_chi__3213E83F13E9E944", x => x.id);
                    table.ForeignKey(
                        name: "FK__dia_chi__nguoi_d__5EBF139D",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "don_hang",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_don_hang = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Cho_xac_nhan"),
                    phuong_thuc_thanh_toan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "COD"),
                    trang_thai_thanh_toan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Chua_thanh_toan"),
                    ten_nguoi_nhan = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    dien_thoai_nhan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    dia_chi_giao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    tien_hang = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    phi_van_chuyen = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    tien_giam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    tong_thanh_toan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ma_giam_gia_id = table.Column<int>(type: "int", nullable: true),
                    ghi_chu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    thoi_gian_cap_nhat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__don_hang__3213E83F16FD52C6", x => x.id);
                    table.ForeignKey(
                        name: "FK__don_hang__ma_gia__5F7E2DAC",
                        column: x => x.ma_giam_gia_id,
                        principalTable: "ma_giam_gia",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__don_hang__nguoi___5E8A0973",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gio_hang",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__gio_hang__3213E83FC9040A41", x => x.id);
                    table.ForeignKey(
                        name: "FK__gio_hang__nguoi___151B244E",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nhan_vien",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    ma_nhan_vien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    chuc_vu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ngay_vao_lam = table.Column<DateOnly>(type: "date", nullable: false),
                    luong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Dang_lam")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__nhan_vie__3213E83FAE835AAE", x => x.id);
                    table.ForeignKey(
                        name: "FK__nhan_vien__nguoi__5441852A",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "nhat_ky_he_thong",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: true),
                    hanh_dong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    doi_tuong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    doi_tuong_id = table.Column<long>(type: "bigint", nullable: true),
                    noi_dung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    thoi_gian = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__nhat_ky___3213E83F896AB24C", x => x.id);
                    table.ForeignKey(
                        name: "FK__nhat_ky_h__nguoi__1A9EF37A",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "xac_thuc_tai_khoan",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    ma_xac_thuc_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    muc_dich = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    het_han = table.Column<DateTime>(type: "datetime2", nullable: false),
                    da_su_dung = table.Column<bool>(type: "bit", nullable: false),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__xac_thuc__3213E83FFA51478D", x => x.id);
                    table.ForeignKey(
                        name: "FK__xac_thuc___nguoi__59FA5E80",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "san_pham",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_san_pham = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ten_san_pham = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    danh_muc_id = table.Column<int>(type: "int", nullable: false),
                    thuong_hieu_id = table.Column<int>(type: "int", nullable: true),
                    chat_lieu_id = table.Column<int>(type: "int", nullable: true),
                    mo_ta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gia_goc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    gia_ban = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Dang_ban"),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    thoi_gian_cap_nhat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__san_pham__3213E83FF2087CAC", x => x.id);
                    table.ForeignKey(
                        name: "FK__san_pham__chat_l__7F2BE32F",
                        column: x => x.chat_lieu_id,
                        principalTable: "chat_lieu",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__san_pham__danh_m__7D439ABD",
                        column: x => x.danh_muc_id,
                        principalTable: "danh_muc",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__san_pham__thuong__7E37BEF6",
                        column: x => x.thuong_hieu_id,
                        principalTable: "thuong_hieu",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "nguoi_dung_vai_tro",
                columns: table => new
                {
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    vai_tro_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nguoi_dung_vai_tro", x => new { x.nguoi_dung_id, x.vai_tro_id });
                    table.ForeignKey(
                        name: "FK__nguoi_dun__nguoi__4AB81AF0",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__nguoi_dun__vai_t__4BAC3F29",
                        column: x => x.vai_tro_id,
                        principalTable: "vai_tro",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vai_tro_quyen",
                columns: table => new
                {
                    vai_tro_id = table.Column<int>(type: "int", nullable: false),
                    quyen_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vai_tro_quyen", x => new { x.vai_tro_id, x.quyen_id });
                    table.ForeignKey(
                        name: "FK__vai_tro_q__quyen__47DBAE45",
                        column: x => x.quyen_id,
                        principalTable: "quyen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__vai_tro_q__vai_t__46E78A0C",
                        column: x => x.vai_tro_id,
                        principalTable: "vai_tro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hoa_don",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_hoa_don = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    don_hang_id = table.Column<long>(type: "bigint", nullable: false),
                    ngay_lap = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    tong_tien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    thue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__hoa_don__3213E83F4F81EFD9", x => x.id);
                    table.ForeignKey(
                        name: "FK__hoa_don__don_han__7849DB76",
                        column: x => x.don_hang_id,
                        principalTable: "don_hang",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lich_su_don_hang",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    don_hang_id = table.Column<long>(type: "bigint", nullable: false),
                    trang_thai_cu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    trang_thai_moi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ghi_chu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    nguoi_cap_nhat = table.Column<long>(type: "bigint", nullable: true),
                    thoi_gian = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__lich_su___3213E83F1C91E6DC", x => x.id);
                    table.ForeignKey(
                        name: "FK__lich_su_d__don_h__6EC0713C",
                        column: x => x.don_hang_id,
                        principalTable: "don_hang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__lich_su_d__nguoi__6FB49575",
                        column: x => x.nguoi_cap_nhat,
                        principalTable: "nguoi_dung",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "thanh_toan",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    don_hang_id = table.Column<long>(type: "bigint", nullable: false),
                    ma_giao_dich = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    phuong_thuc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    so_tien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    thoi_gian_giao_dich = table.Column<DateTime>(type: "datetime2", nullable: true),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__thanh_to__3213E83F9EC113C3", x => x.id);
                    table.ForeignKey(
                        name: "FK__thanh_toa__don_h__7E02B4CC",
                        column: x => x.don_hang_id,
                        principalTable: "don_hang",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "yeu_cau_huy_don",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    don_hang_id = table.Column<long>(type: "bigint", nullable: false),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    ly_do = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Cho_duyet"),
                    xu_ly_boi = table.Column<long>(type: "bigint", nullable: true),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    thoi_gian_xu_ly = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__yeu_cau___3213E83FB583BFB6", x => x.id);
                    table.ForeignKey(
                        name: "FK__yeu_cau_h__don_h__03BB8E22",
                        column: x => x.don_hang_id,
                        principalTable: "don_hang",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__yeu_cau_h__nguoi__04AFB25B",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__yeu_cau_h__xu_ly__05A3D694",
                        column: x => x.xu_ly_boi,
                        principalTable: "nguoi_dung",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "kho",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_kho = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ten_kho = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    dia_chi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    nguoi_quan_ly_id = table.Column<long>(type: "bigint", nullable: true),
                    trang_thai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__kho__3213E83F97974D2E", x => x.id);
                    table.ForeignKey(
                        name: "FK__kho__nguoi_quan___208CD6FA",
                        column: x => x.nguoi_quan_ly_id,
                        principalTable: "nhan_vien",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "bien_the_san_pham",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    san_pham_id = table.Column<int>(type: "int", nullable: false),
                    sku = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    kich_thuoc_id = table.Column<int>(type: "int", nullable: false),
                    mau_sac_id = table.Column<int>(type: "int", nullable: false),
                    gia_ban = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    trang_thai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__bien_the__3213E83F579D7CEA", x => x.id);
                    table.UniqueConstraint("AK_bien_the_san_pham_san_pham_id_id", x => new { x.san_pham_id, x.id });
                    table.ForeignKey(
                        name: "FK__bien_the___kich___06CD04F7",
                        column: x => x.kich_thuoc_id,
                        principalTable: "kich_thuoc",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__bien_the___mau_s__07C12930",
                        column: x => x.mau_sac_id,
                        principalTable: "mau_sac",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__bien_the___san_p__05D8E0BE",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "danh_gia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    san_pham_id = table.Column<int>(type: "int", nullable: false),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    don_hang_id = table.Column<long>(type: "bigint", nullable: false),
                    so_sao = table.Column<byte>(type: "tinyint", nullable: false),
                    noi_dung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    trang_thai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Cho_duyet"),
                    phan_hoi_shop = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    thoi_gian_phan_hoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__danh_gia__3213E83F56680C4C", x => x.id);
                    table.ForeignKey(
                        name: "FK__danh_gia__don_ha__0F2D40CE",
                        column: x => x.don_hang_id,
                        principalTable: "don_hang",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__danh_gia__nguoi___0E391C95",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__danh_gia__san_ph__0D44F85C",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "khuyen_mai_san_pham",
                columns: table => new
                {
                    ma_giam_gia_id = table.Column<int>(type: "int", nullable: false),
                    san_pham_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__khuyen_m__A7F84C13125803A3", x => new { x.ma_giam_gia_id, x.san_pham_id });
                    table.ForeignKey(
                        name: "FK__khuyen_ma__ma_gi__46B27FE2",
                        column: x => x.ma_giam_gia_id,
                        principalTable: "ma_giam_gia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__khuyen_ma__san_p__47A6A41B",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lich_su_xem_san_pham",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nguoi_dung_id = table.Column<long>(type: "bigint", nullable: false),
                    san_pham_id = table.Column<int>(type: "int", nullable: false),
                    thoi_gian_xem = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__lich_su___3213E83F80717680", x => x.id);
                    table.ForeignKey(
                        name: "FK__lich_su_x__nguoi__15DA3E5D",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__lich_su_x__san_p__16CE6296",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "san_pham_tag",
                columns: table => new
                {
                    san_pham_id = table.Column<int>(type: "int", nullable: false),
                    tag_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__san_pham__3919D760B894BC5B", x => new { x.san_pham_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK__san_pham___san_p__0F624AF8",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__san_pham___tag_i__10566F31",
                        column: x => x.tag_id,
                        principalTable: "tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phieu_nhap_kho",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phieu = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    kho_id = table.Column<int>(type: "int", nullable: false),
                    nhan_vien_id = table.Column<long>(type: "bigint", nullable: false),
                    nha_cung_cap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ngay_nhap = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    ghi_chu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__phieu_nh__3213E83F6FB88AE5", x => x.id);
                    table.ForeignKey(
                        name: "FK__phieu_nha__kho_i__2EDAF651",
                        column: x => x.kho_id,
                        principalTable: "kho",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__phieu_nha__nhan___2FCF1A8A",
                        column: x => x.nhan_vien_id,
                        principalTable: "nhan_vien",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "don_hang_chi_tiet",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    don_hang_id = table.Column<long>(type: "bigint", nullable: false),
                    bien_the_id = table.Column<long>(type: "bigint", nullable: false),
                    ten_san_pham = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    sku = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    size = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    mau_sac = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    don_gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    so_luong = table.Column<int>(type: "int", nullable: false),
                    thanh_tien = table.Column<decimal>(type: "decimal(29,2)", nullable: true, computedColumnSql: "([don_gia]*[so_luong])", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__don_hang__3213E83FC26BD7D1", x => x.id);
                    table.ForeignKey(
                        name: "FK__don_hang___bien___65370702",
                        column: x => x.bien_the_id,
                        principalTable: "bien_the_san_pham",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__don_hang___don_h__6442E2C9",
                        column: x => x.don_hang_id,
                        principalTable: "don_hang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gio_hang_chi_tiet",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    gio_hang_id = table.Column<long>(type: "bigint", nullable: false),
                    bien_the_id = table.Column<long>(type: "bigint", nullable: false),
                    so_luong = table.Column<int>(type: "int", nullable: false),
                    thoi_gian_them = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__gio_hang__3213E83F1BAD7E07", x => x.id);
                    table.ForeignKey(
                        name: "FK__gio_hang___bien___1BC821DD",
                        column: x => x.bien_the_id,
                        principalTable: "bien_the_san_pham",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__gio_hang___gio_h__1AD3FDA4",
                        column: x => x.gio_hang_id,
                        principalTable: "gio_hang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "san_pham_anh",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    san_pham_id = table.Column<int>(type: "int", nullable: false),
                    bien_the_id = table.Column<long>(type: "bigint", nullable: true),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    la_anh_dai_dien = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__san_pham__3213E83FE4CAE708", x => x.id);
                    table.ForeignKey(
                        name: "FK__san_pham___bien___0C85DE4D",
                        column: x => x.bien_the_id,
                        principalTable: "bien_the_san_pham",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__san_pham___san_p__0B91BA14",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_san_pham_anh_bien_the_cung_san_pham",
                        columns: x => new { x.san_pham_id, x.bien_the_id },
                        principalTable: "bien_the_san_pham",
                        principalColumns: new[] { "san_pham_id", "id" });
                });

            migrationBuilder.CreateTable(
                name: "ton_kho",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    kho_id = table.Column<int>(type: "int", nullable: false),
                    bien_the_id = table.Column<long>(type: "bigint", nullable: false),
                    so_luong_ton = table.Column<int>(type: "int", nullable: false),
                    so_luong_giu_cho = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ton_kho__3213E83F443D4EC5", x => x.id);
                    table.UniqueConstraint("AK_ton_kho_kho_id_bien_the_id", x => new { x.kho_id, x.bien_the_id });
                    table.ForeignKey(
                        name: "FK__ton_kho__bien_th__2A164134",
                        column: x => x.bien_the_id,
                        principalTable: "bien_the_san_pham",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__ton_kho__kho_id__29221CFB",
                        column: x => x.kho_id,
                        principalTable: "kho",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "danh_gia_anh",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    danh_gia_id = table.Column<long>(type: "bigint", nullable: false),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__danh_gia__3213E83F903305BD", x => x.id);
                    table.ForeignKey(
                        name: "FK__danh_gia___danh___1209AD79",
                        column: x => x.danh_gia_id,
                        principalTable: "danh_gia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chi_tiet_phieu_nhap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    phieu_nhap_id = table.Column<long>(type: "bigint", nullable: false),
                    bien_the_id = table.Column<long>(type: "bigint", nullable: false),
                    so_luong = table.Column<int>(type: "int", nullable: false),
                    don_gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    thanh_tien = table.Column<decimal>(type: "decimal(29,2)", nullable: true, computedColumnSql: "([so_luong]*[don_gia])", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__chi_tiet__3213E83F4758D493", x => x.id);
                    table.ForeignKey(
                        name: "FK__chi_tiet___bien___3587F3E0",
                        column: x => x.bien_the_id,
                        principalTable: "bien_the_san_pham",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__chi_tiet___phieu__3493CFA7",
                        column: x => x.phieu_nhap_id,
                        principalTable: "phieu_nhap_kho",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chi_tiet_giu_cho",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    don_hang_ct_id = table.Column<long>(type: "bigint", nullable: false),
                    kho_id = table.Column<int>(type: "int", nullable: false),
                    bien_the_id = table.Column<long>(type: "bigint", nullable: false),
                    so_luong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__chi_tiet__3213E83F6E6093E9", x => x.id);
                    table.ForeignKey(
                        name: "FK__chi_tiet___don_h__69FBBC1F",
                        column: x => x.don_hang_ct_id,
                        principalTable: "don_hang_chi_tiet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__chi_tiet_giu_cho__6AEFE058",
                        columns: x => new { x.kho_id, x.bien_the_id },
                        principalTable: "ton_kho",
                        principalColumns: new[] { "kho_id", "bien_the_id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_bien_the_san_pham",
                table: "bien_the_san_pham",
                columns: new[] { "san_pham_id", "trang_thai" });

            migrationBuilder.CreateIndex(
                name: "IX_bien_the_san_pham_kich_thuoc_id",
                table: "bien_the_san_pham",
                column: "kich_thuoc_id");

            migrationBuilder.CreateIndex(
                name: "IX_bien_the_san_pham_mau_sac_id",
                table: "bien_the_san_pham",
                column: "mau_sac_id");

            migrationBuilder.CreateIndex(
                name: "UQ__bien_the__DDDF4BE707FA1FE3",
                table: "bien_the_san_pham",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_bien_the",
                table: "bien_the_san_pham",
                columns: new[] { "san_pham_id", "kich_thuoc_id", "mau_sac_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_bien_the_san_pham_id",
                table: "bien_the_san_pham",
                columns: new[] { "san_pham_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__chat_lie__47FA287200D13DFF",
                table: "chat_lieu",
                column: "ten_chat_lieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_giu_cho_kho_id_bien_the_id",
                table: "chi_tiet_giu_cho",
                columns: new[] { "kho_id", "bien_the_id" });

            migrationBuilder.CreateIndex(
                name: "UQ_chi_tiet_giu_cho",
                table: "chi_tiet_giu_cho",
                columns: new[] { "don_hang_ct_id", "kho_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_phieu_nhap_bien_the_id",
                table: "chi_tiet_phieu_nhap",
                column: "bien_the_id");

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_phieu_nhap_phieu_nhap_id",
                table: "chi_tiet_phieu_nhap",
                column: "phieu_nhap_id");

            migrationBuilder.CreateIndex(
                name: "IX_danh_gia_don_hang_id",
                table: "danh_gia",
                column: "don_hang_id");

            migrationBuilder.CreateIndex(
                name: "IX_danh_gia_san_pham",
                table: "danh_gia",
                columns: new[] { "san_pham_id", "trang_thai" });

            migrationBuilder.CreateIndex(
                name: "UQ_danh_gia",
                table: "danh_gia",
                columns: new[] { "nguoi_dung_id", "don_hang_id", "san_pham_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_danh_gia_anh_danh_gia_id",
                table: "danh_gia_anh",
                column: "danh_gia_id");

            migrationBuilder.CreateIndex(
                name: "UQ__danh_muc__B3D28925F5F8ED23",
                table: "danh_muc",
                column: "ten_danh_muc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_dia_chi_mac_dinh",
                table: "dia_chi",
                column: "nguoi_dung_id",
                unique: true,
                filter: "([mac_dinh]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_don_hang_ma_giam_gia_id",
                table: "don_hang",
                column: "ma_giam_gia_id");

            migrationBuilder.CreateIndex(
                name: "IX_don_hang_nguoi_dung_ngay",
                table: "don_hang",
                columns: new[] { "nguoi_dung_id", "thoi_gian_tao" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_don_hang_trang_thai",
                table: "don_hang",
                columns: new[] { "trang_thai", "thoi_gian_tao" });

            migrationBuilder.CreateIndex(
                name: "UQ__don_hang__0246C5EB2E396E3D",
                table: "don_hang",
                column: "ma_don_hang",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_don_hang",
                table: "don_hang_chi_tiet",
                column: "don_hang_id");

            migrationBuilder.CreateIndex(
                name: "IX_don_hang_chi_tiet_bien_the_id",
                table: "don_hang_chi_tiet",
                column: "bien_the_id");

            migrationBuilder.CreateIndex(
                name: "UQ__gio_hang__3F7CE07654C4D843",
                table: "gio_hang",
                column: "nguoi_dung_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gio_hang_chi_tiet_bien_the_id",
                table: "gio_hang_chi_tiet",
                column: "bien_the_id");

            migrationBuilder.CreateIndex(
                name: "UQ_gio_hang_sku",
                table: "gio_hang_chi_tiet",
                columns: new[] { "gio_hang_id", "bien_the_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__hoa_don__B81FB4F2F14442D5",
                table: "hoa_don",
                column: "don_hang_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__hoa_don__DBE2D9E2E7E82F69",
                table: "hoa_don",
                column: "ma_hoa_don",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kho_nguoi_quan_ly_id",
                table: "kho",
                column: "nguoi_quan_ly_id");

            migrationBuilder.CreateIndex(
                name: "UQ__kho__0A2E13A53C04E53F",
                table: "kho",
                column: "ma_kho",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_khuyen_mai_san_pham_san_pham_id",
                table: "khuyen_mai_san_pham",
                column: "san_pham_id");

            migrationBuilder.CreateIndex(
                name: "UQ__kich_thu__0B396BB0BBE70A7F",
                table: "kich_thuoc",
                column: "ma_size",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_don_hang_don_thoi_gian",
                table: "lich_su_don_hang",
                columns: new[] { "don_hang_id", "thoi_gian" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_don_hang_nguoi_cap_nhat",
                table: "lich_su_don_hang",
                column: "nguoi_cap_nhat");

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_xem",
                table: "lich_su_xem_san_pham",
                columns: new[] { "nguoi_dung_id", "thoi_gian_xem" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_xem_san_pham_san_pham_id",
                table: "lich_su_xem_san_pham",
                column: "san_pham_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_hieu_luc",
                table: "ma_giam_gia",
                columns: new[] { "trang_thai", "ngay_bat_dau", "ngay_ket_thuc" });

            migrationBuilder.CreateIndex(
                name: "UQ__ma_giam___466D17BF7C5B468A",
                table: "ma_giam_gia",
                column: "ma_voucher",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__mau_sac__5120D1BD854DA9EE",
                table: "mau_sac",
                column: "ten_mau",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_nguoi_dung_email",
                table: "nguoi_dung",
                column: "email_chuan",
                unique: true,
                filter: "[email_chuan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_nguoi_dung_ten_dang_nhap",
                table: "nguoi_dung",
                column: "ten_dang_nhap",
                unique: true,
                filter: "[ten_dang_nhap] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_nguoi_dung_vai_tro_vai_tro_id",
                table: "nguoi_dung_vai_tro",
                column: "vai_tro_id");

            migrationBuilder.CreateIndex(
                name: "UQ__nhan_vie__3F7CE076D6B0335F",
                table: "nhan_vien",
                column: "nguoi_dung_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__nhan_vie__6781B7B868FBE5A3",
                table: "nhan_vien",
                column: "ma_nhan_vien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nhat_ky_he_thong_nguoi_dung_id",
                table: "nhat_ky_he_thong",
                column: "nguoi_dung_id");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_nhap_kho_kho_id",
                table: "phieu_nhap_kho",
                column: "kho_id");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_nhap_kho_nhan_vien_id",
                table: "phieu_nhap_kho",
                column: "nhan_vien_id");

            migrationBuilder.CreateIndex(
                name: "UQ__phieu_nh__11D0F07BF1773D65",
                table: "phieu_nhap_kho",
                column: "ma_phieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__quyen__B9A4290E19F9AD03",
                table: "quyen",
                column: "ma_quyen",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_chat_lieu_id",
                table: "san_pham",
                column: "chat_lieu_id");

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_danh_muc",
                table: "san_pham",
                columns: new[] { "danh_muc_id", "trang_thai" });

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_ten",
                table: "san_pham",
                column: "ten_san_pham");

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_thuong_hieu_id",
                table: "san_pham",
                column: "thuong_hieu_id");

            migrationBuilder.CreateIndex(
                name: "UQ__san_pham__9D25990D72ED61E0",
                table: "san_pham",
                column: "ma_san_pham",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_anh_bien_the_id",
                table: "san_pham_anh",
                column: "bien_the_id");

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_anh_san_pham_id_bien_the_id",
                table: "san_pham_anh",
                columns: new[] { "san_pham_id", "bien_the_id" });

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_tag_tag_id",
                table: "san_pham_tag",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "UQ_tag",
                table: "tag",
                columns: new[] { "ten_tag", "loai" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_thanh_toan_don_hang_trang_thai",
                table: "thanh_toan",
                columns: new[] { "don_hang_id", "trang_thai" });

            migrationBuilder.CreateIndex(
                name: "UX_thanh_toan_ma_giao_dich",
                table: "thanh_toan",
                column: "ma_giao_dich",
                unique: true,
                filter: "([ma_giao_dich] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ__thuong_h__2A54B350EE892BC6",
                table: "thuong_hieu",
                column: "ten_thuong_hieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ton_kho_bien_the",
                table: "ton_kho",
                columns: new[] { "bien_the_id", "kho_id" });

            migrationBuilder.CreateIndex(
                name: "UQ_ton_kho",
                table: "ton_kho",
                columns: new[] { "kho_id", "bien_the_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__vai_tro__4AE1754C01D5150D",
                table: "vai_tro",
                column: "ma_vai_tro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vai_tro_quyen_quyen_id",
                table: "vai_tro_quyen",
                column: "quyen_id");

            migrationBuilder.CreateIndex(
                name: "IX_xac_thuc_tai_khoan_nguoi_dung_id",
                table: "xac_thuc_tai_khoan",
                column: "nguoi_dung_id");

            migrationBuilder.CreateIndex(
                name: "IX_yeu_cau_huy_don_don_hang_id",
                table: "yeu_cau_huy_don",
                column: "don_hang_id");

            migrationBuilder.CreateIndex(
                name: "IX_yeu_cau_huy_don_nguoi_dung_id",
                table: "yeu_cau_huy_don",
                column: "nguoi_dung_id");

            migrationBuilder.CreateIndex(
                name: "IX_yeu_cau_huy_don_xu_ly_boi",
                table: "yeu_cau_huy_don",
                column: "xu_ly_boi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banner_khuyen_mai");

            migrationBuilder.DropTable(
                name: "chi_tiet_giu_cho");

            migrationBuilder.DropTable(
                name: "chi_tiet_phieu_nhap");

            migrationBuilder.DropTable(
                name: "danh_gia_anh");

            migrationBuilder.DropTable(
                name: "dia_chi");

            migrationBuilder.DropTable(
                name: "gio_hang_chi_tiet");

            migrationBuilder.DropTable(
                name: "hoa_don");

            migrationBuilder.DropTable(
                name: "khuyen_mai_san_pham");

            migrationBuilder.DropTable(
                name: "lich_su_don_hang");

            migrationBuilder.DropTable(
                name: "lich_su_xem_san_pham");

            migrationBuilder.DropTable(
                name: "nguoi_dung_vai_tro");

            migrationBuilder.DropTable(
                name: "nhat_ky_he_thong");

            migrationBuilder.DropTable(
                name: "san_pham_anh");

            migrationBuilder.DropTable(
                name: "san_pham_tag");

            migrationBuilder.DropTable(
                name: "thanh_toan");

            migrationBuilder.DropTable(
                name: "vai_tro_quyen");

            migrationBuilder.DropTable(
                name: "xac_thuc_tai_khoan");

            migrationBuilder.DropTable(
                name: "yeu_cau_huy_don");

            migrationBuilder.DropTable(
                name: "don_hang_chi_tiet");

            migrationBuilder.DropTable(
                name: "ton_kho");

            migrationBuilder.DropTable(
                name: "phieu_nhap_kho");

            migrationBuilder.DropTable(
                name: "danh_gia");

            migrationBuilder.DropTable(
                name: "gio_hang");

            migrationBuilder.DropTable(
                name: "tag");

            migrationBuilder.DropTable(
                name: "quyen");

            migrationBuilder.DropTable(
                name: "vai_tro");

            migrationBuilder.DropTable(
                name: "bien_the_san_pham");

            migrationBuilder.DropTable(
                name: "kho");

            migrationBuilder.DropTable(
                name: "don_hang");

            migrationBuilder.DropTable(
                name: "kich_thuoc");

            migrationBuilder.DropTable(
                name: "mau_sac");

            migrationBuilder.DropTable(
                name: "san_pham");

            migrationBuilder.DropTable(
                name: "nhan_vien");

            migrationBuilder.DropTable(
                name: "ma_giam_gia");

            migrationBuilder.DropTable(
                name: "chat_lieu");

            migrationBuilder.DropTable(
                name: "danh_muc");

            migrationBuilder.DropTable(
                name: "thuong_hieu");

            migrationBuilder.DropTable(
                name: "nguoi_dung");
        }
    }
}
