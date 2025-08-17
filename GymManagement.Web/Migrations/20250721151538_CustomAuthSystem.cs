using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class CustomAuthSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoiTaps",
                columns: table => new
                {
                    GoiTapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenGoi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ThoiHanThang = table.Column<int>(type: "int", nullable: false),
                    SoBuoiToiDa = table.Column<int>(type: "int", nullable: true),
                    Gia = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiTaps", x => x.GoiTapId);
                });

            migrationBuilder.CreateTable(
                name: "KhuyenMais",
                columns: table => new
                {
                    KhuyenMaiId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PhanTramGiam = table.Column<int>(type: "int", nullable: true),
                    NgayBatDau = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayKetThuc = table.Column<DateOnly>(type: "date", nullable: false),
                    KichHoat = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuyenMais", x => x.KhuyenMaiId);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDungs",
                columns: table => new
                {
                    NguoiDungId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoaiNguoiDung = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ho = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayThamGia = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "GETDATE()"),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDungs", x => x.NguoiDungId);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenVaiTro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CauHinhHoaHongs",
                columns: table => new
                {
                    CauHinhHoaHongId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoiTapId = table.Column<int>(type: "int", nullable: true),
                    PhanTramHoaHong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHinhHoaHongs", x => x.CauHinhHoaHongId);
                    table.ForeignKey(
                        name: "FK_CauHinhHoaHongs_GoiTaps_GoiTapId",
                        column: x => x.GoiTapId,
                        principalTable: "GoiTaps",
                        principalColumn: "GoiTapId");
                });

            migrationBuilder.CreateTable(
                name: "BangLuongs",
                columns: table => new
                {
                    BangLuongId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlvId = table.Column<int>(type: "int", nullable: true),
                    Thang = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    LuongCoBan = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TienHoaHong = table.Column<decimal>(type: "decimal(12,2)", nullable: false, defaultValue: 0m),
                    NgayThanhToan = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BangLuongs", x => x.BangLuongId);
                    table.ForeignKey(
                        name: "FK_BangLuongs_NguoiDungs_HlvId",
                        column: x => x.HlvId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "DiemDanhs",
                columns: table => new
                {
                    DiemDanhId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThanhVienId = table.Column<int>(type: "int", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    KetQuaNhanDang = table.Column<bool>(type: "bit", nullable: true),
                    AnhMinhChung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiemDanhs", x => x.DiemDanhId);
                    table.ForeignKey(
                        name: "FK_DiemDanhs_NguoiDungs_ThanhVienId",
                        column: x => x.ThanhVienId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "LichSuAnhs",
                columns: table => new
                {
                    LichSuAnhId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiDungId = table.Column<int>(type: "int", nullable: true),
                    AnhCu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AnhMoi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LyDo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuAnhs", x => x.LichSuAnhId);
                    table.ForeignKey(
                        name: "FK_LichSuAnhs_NguoiDungs_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "LopHocs",
                columns: table => new
                {
                    LopHocId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLop = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HlvId = table.Column<int>(type: "int", nullable: true),
                    SucChua = table.Column<int>(type: "int", nullable: false),
                    GioBatDau = table.Column<TimeOnly>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeOnly>(type: "time", nullable: false),
                    ThuTrongTuan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GiaTuyChinh = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "OPEN")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LopHocs", x => x.LopHocId);
                    table.ForeignKey(
                        name: "FK_LopHocs_NguoiDungs_HlvId",
                        column: x => x.HlvId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "MauMats",
                columns: table => new
                {
                    MauMatId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiDungId = table.Column<int>(type: "int", nullable: false),
                    Embedding = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ThuatToan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "ArcFace")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauMats", x => x.MauMatId);
                    table.ForeignKey(
                        name: "FK_MauMats_NguoiDungs_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenDangNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MatKhauHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NguoiDungId = table.Column<int>(type: "int", nullable: true),
                    KichHoat = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EmailXacNhan = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LanDangNhapCuoi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_NguoiDungs_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ThongBaos",
                columns: table => new
                {
                    ThongBaoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    NguoiDungId = table.Column<int>(type: "int", nullable: true),
                    Kenh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaos", x => x.ThongBaoId);
                    table.ForeignKey(
                        name: "FK_ThongBaos_NguoiDungs_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "BuoiHlvs",
                columns: table => new
                {
                    BuoiHlvId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlvId = table.Column<int>(type: "int", nullable: true),
                    ThanhVienId = table.Column<int>(type: "int", nullable: true),
                    LopHocId = table.Column<int>(type: "int", nullable: true),
                    NgayTap = table.Column<DateOnly>(type: "date", nullable: false),
                    ThoiLuongPhut = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuoiHlvs", x => x.BuoiHlvId);
                    table.ForeignKey(
                        name: "FK_BuoiHlvs_LopHocs_LopHocId",
                        column: x => x.LopHocId,
                        principalTable: "LopHocs",
                        principalColumn: "LopHocId");
                    table.ForeignKey(
                        name: "FK_BuoiHlvs_NguoiDungs_HlvId",
                        column: x => x.HlvId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                    table.ForeignKey(
                        name: "FK_BuoiHlvs_NguoiDungs_ThanhVienId",
                        column: x => x.ThanhVienId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "BuoiTaps",
                columns: table => new
                {
                    BuoiTapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThanhVienId = table.Column<int>(type: "int", nullable: true),
                    LopHocId = table.Column<int>(type: "int", nullable: true),
                    ThoiGianVao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianRa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuoiTaps", x => x.BuoiTapId);
                    table.ForeignKey(
                        name: "FK_BuoiTaps_LopHocs_LopHocId",
                        column: x => x.LopHocId,
                        principalTable: "LopHocs",
                        principalColumn: "LopHocId");
                    table.ForeignKey(
                        name: "FK_BuoiTaps_NguoiDungs_ThanhVienId",
                        column: x => x.ThanhVienId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "DangKys",
                columns: table => new
                {
                    DangKyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiDungId = table.Column<int>(type: "int", nullable: false),
                    GoiTapId = table.Column<int>(type: "int", nullable: true),
                    LopHocId = table.Column<int>(type: "int", nullable: true),
                    NgayBatDau = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayKetThuc = table.Column<DateOnly>(type: "date", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKys", x => x.DangKyId);
                    table.ForeignKey(
                        name: "FK_DangKys_GoiTaps_GoiTapId",
                        column: x => x.GoiTapId,
                        principalTable: "GoiTaps",
                        principalColumn: "GoiTapId");
                    table.ForeignKey(
                        name: "FK_DangKys_LopHocs_LopHocId",
                        column: x => x.LopHocId,
                        principalTable: "LopHocs",
                        principalColumn: "LopHocId");
                    table.ForeignKey(
                        name: "FK_DangKys_NguoiDungs_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichLops",
                columns: table => new
                {
                    LichLopId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LopHocId = table.Column<int>(type: "int", nullable: false),
                    Ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    GioBatDau = table.Column<TimeOnly>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeOnly>(type: "time", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "SCHEDULED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichLops", x => x.LichLopId);
                    table.ForeignKey(
                        name: "FK_LichLops_LopHocs_LopHocId",
                        column: x => x.LopHocId,
                        principalTable: "LopHocs",
                        principalColumn: "LopHocId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaiKhoanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogins_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoanVaiTros",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaiKhoanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VaiTroId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NgayGan = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanVaiTros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTros_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTros_VaiTro_VaiTroId",
                        column: x => x.VaiTroId,
                        principalTable: "VaiTro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThanhToans",
                columns: table => new
                {
                    ThanhToanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DangKyId = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    PhuongThuc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    GhiChu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhToans", x => x.ThanhToanId);
                    table.ForeignKey(
                        name: "FK_ThanhToans_DangKys_DangKyId",
                        column: x => x.DangKyId,
                        principalTable: "DangKys",
                        principalColumn: "DangKyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThanhVienId = table.Column<int>(type: "int", nullable: true),
                    LopHocId = table.Column<int>(type: "int", nullable: true),
                    LichLopId = table.Column<int>(type: "int", nullable: true),
                    Ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "BOOKED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingId);
                    table.ForeignKey(
                        name: "FK_Bookings_LichLops_LichLopId",
                        column: x => x.LichLopId,
                        principalTable: "LichLops",
                        principalColumn: "LichLopId");
                    table.ForeignKey(
                        name: "FK_Bookings_LopHocs_LopHocId",
                        column: x => x.LopHocId,
                        principalTable: "LopHocs",
                        principalColumn: "LopHocId");
                    table.ForeignKey(
                        name: "FK_Bookings_NguoiDungs_ThanhVienId",
                        column: x => x.ThanhVienId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "ThanhToanGateways",
                columns: table => new
                {
                    GatewayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThanhToanId = table.Column<int>(type: "int", nullable: false),
                    GatewayTen = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "VNPAY"),
                    GatewayTransId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GatewayOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GatewayAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    GatewayRespCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    GatewayMessage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThoiGianCallback = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhToanGateways", x => x.GatewayId);
                    table.ForeignKey(
                        name: "FK_ThanhToanGateways_ThanhToans_ThanhToanId",
                        column: x => x.ThanhToanId,
                        principalTable: "ThanhToans",
                        principalColumn: "ThanhToanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BangLuongs_HlvId",
                table: "BangLuongs",
                column: "HlvId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_LichLopId",
                table: "Bookings",
                column: "LichLopId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_LopHocId",
                table: "Bookings",
                column: "LopHocId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ThanhVienId",
                table: "Bookings",
                column: "ThanhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiHlvs_HlvId",
                table: "BuoiHlvs",
                column: "HlvId");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiHlvs_LopHocId",
                table: "BuoiHlvs",
                column: "LopHocId");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiHlvs_ThanhVienId",
                table: "BuoiHlvs",
                column: "ThanhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiTaps_LopHocId",
                table: "BuoiTaps",
                column: "LopHocId");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiTaps_ThanhVienId",
                table: "BuoiTaps",
                column: "ThanhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_CauHinhHoaHongs_GoiTapId",
                table: "CauHinhHoaHongs",
                column: "GoiTapId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKys_GoiTapId",
                table: "DangKys",
                column: "GoiTapId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKys_LopHocId",
                table: "DangKys",
                column: "LopHocId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKys_NguoiDungId",
                table: "DangKys",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_ThanhVienId",
                table: "DiemDanhs",
                column: "ThanhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ProviderKey",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_TaiKhoanId",
                table: "ExternalLogins",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMais_MaCode",
                table: "KhuyenMais",
                column: "MaCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichLops_LopHocId",
                table: "LichLops",
                column: "LopHocId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuAnhs_NguoiDungId",
                table: "LichSuAnhs",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocs_HlvId",
                table: "LopHocs",
                column: "HlvId");

            migrationBuilder.CreateIndex(
                name: "IX_MauMats_NguoiDungId",
                table: "MauMats",
                column: "NguoiDungId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_Email",
                table: "TaiKhoan",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_NguoiDungId",
                table: "TaiKhoan",
                column: "NguoiDungId",
                unique: true,
                filter: "[NguoiDungId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTros_TaiKhoanId_VaiTroId",
                table: "TaiKhoanVaiTros",
                columns: new[] { "TaiKhoanId", "VaiTroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTros_VaiTroId",
                table: "TaiKhoanVaiTros",
                column: "VaiTroId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToanGateways_ThanhToanId",
                table: "ThanhToanGateways",
                column: "ThanhToanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToans_DangKyId",
                table: "ThanhToans",
                column: "DangKyId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaos_NguoiDungId",
                table: "ThongBaos",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_TenVaiTro",
                table: "VaiTro",
                column: "TenVaiTro",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BangLuongs");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "BuoiHlvs");

            migrationBuilder.DropTable(
                name: "BuoiTaps");

            migrationBuilder.DropTable(
                name: "CauHinhHoaHongs");

            migrationBuilder.DropTable(
                name: "DiemDanhs");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "KhuyenMais");

            migrationBuilder.DropTable(
                name: "LichSuAnhs");

            migrationBuilder.DropTable(
                name: "MauMats");

            migrationBuilder.DropTable(
                name: "TaiKhoanVaiTros");

            migrationBuilder.DropTable(
                name: "ThanhToanGateways");

            migrationBuilder.DropTable(
                name: "ThongBaos");

            migrationBuilder.DropTable(
                name: "LichLops");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "VaiTro");

            migrationBuilder.DropTable(
                name: "ThanhToans");

            migrationBuilder.DropTable(
                name: "DangKys");

            migrationBuilder.DropTable(
                name: "GoiTaps");

            migrationBuilder.DropTable(
                name: "LopHocs");

            migrationBuilder.DropTable(
                name: "NguoiDungs");
        }
    }
}
