using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLTN.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateThanhToanNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LichSuCheckIns_ThanhViens_MaTV",
                table: "LichSuCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_ThongBaos_TaiKhoans_TaiKhoanMaTK",
                table: "ThongBaos");

            migrationBuilder.DropForeignKey(
                name: "FK_ThongBaos_ThanhViens_MaTV",
                table: "ThongBaos");

            migrationBuilder.DropTable(
                name: "DoanhThus");

            migrationBuilder.DropTable(
                name: "PT_GoiTap");

            migrationBuilder.DropTable(
                name: "PT_LopHoc");

            migrationBuilder.DropColumn(
                name: "NgayDangKy",
                table: "ThanhViens");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "ThanhViens");

            migrationBuilder.DropColumn(
                name: "LoaiGoiTap",
                table: "GoiTap");

            migrationBuilder.RenameColumn(
                name: "TaiKhoanMaTK",
                table: "ThongBaos",
                newName: "ThanhVienMaTV");

            migrationBuilder.RenameColumn(
                name: "MaTV",
                table: "ThongBaos",
                newName: "MaTK");

            migrationBuilder.RenameIndex(
                name: "IX_ThongBaos_TaiKhoanMaTK",
                table: "ThongBaos",
                newName: "IX_ThongBaos_ThanhVienMaTV");

            migrationBuilder.RenameIndex(
                name: "IX_ThongBaos_MaTV",
                table: "ThongBaos",
                newName: "IX_ThongBaos_MaTK");

            migrationBuilder.RenameColumn(
                name: "MaTV",
                table: "LichSuCheckIns",
                newName: "ThanhVienMaTV");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuCheckIns_MaTV",
                table: "LichSuCheckIns",
                newName: "IX_LichSuCheckIns_ThanhVienMaTV");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "TinTucs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "MaKVL_NguoiDung",
                table: "ThanhToans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaTK_NguoiDung",
                table: "ThanhToans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NguoiDung_KhachVangLaiMaKVL",
                table: "ThanhToans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NguoiDung_TaiKhoanMaTK",
                table: "ThanhToans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaTK",
                table: "LichSuCheckIns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CapNhatAnhNhanDiens",
                columns: table => new
                {
                    MaCapNhat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    AnhCu = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    AnhMoi = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ThoiGianCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiCapNhat = table.Column<int>(type: "int", nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapNhatAnhNhanDiens", x => x.MaCapNhat);
                    table.ForeignKey(
                        name: "FK_CapNhatAnhNhanDiens_TaiKhoans_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CapNhatAnhNhanDiens_TaiKhoans_NguoiCapNhat",
                        column: x => x.NguoiCapNhat,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PT_PhanCongHoaHongs",
                columns: table => new
                {
                    MaPhanCong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPT = table.Column<int>(type: "int", nullable: false),
                    MaGoiTap = table.Column<int>(type: "int", nullable: true),
                    MaLopHoc = table.Column<int>(type: "int", nullable: true),
                    PhanTramHoaHong = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PT_PhanCongHoaHongs", x => x.MaPhanCong);
                    table.ForeignKey(
                        name: "FK_PT_PhanCongHoaHongs_GoiTap_MaGoiTap",
                        column: x => x.MaGoiTap,
                        principalTable: "GoiTap",
                        principalColumn: "MaGoi");
                    table.ForeignKey(
                        name: "FK_PT_PhanCongHoaHongs_HuanLuyenViens_MaPT",
                        column: x => x.MaPT,
                        principalTable: "HuanLuyenViens",
                        principalColumn: "MaPT",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PT_PhanCongHoaHongs_LopHoc_MaLopHoc",
                        column: x => x.MaLopHoc,
                        principalTable: "LopHoc",
                        principalColumn: "MaLop");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToans_NguoiDung_KhachVangLaiMaKVL",
                table: "ThanhToans",
                column: "NguoiDung_KhachVangLaiMaKVL");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToans_NguoiDung_TaiKhoanMaTK",
                table: "ThanhToans",
                column: "NguoiDung_TaiKhoanMaTK");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuCheckIns_MaTK",
                table: "LichSuCheckIns",
                column: "MaTK");

            migrationBuilder.CreateIndex(
                name: "IX_CapNhatAnhNhanDiens_MaTK",
                table: "CapNhatAnhNhanDiens",
                column: "MaTK");

            migrationBuilder.CreateIndex(
                name: "IX_CapNhatAnhNhanDiens_NguoiCapNhat",
                table: "CapNhatAnhNhanDiens",
                column: "NguoiCapNhat");

            migrationBuilder.CreateIndex(
                name: "IX_PT_PhanCongHoaHongs_MaGoiTap",
                table: "PT_PhanCongHoaHongs",
                column: "MaGoiTap");

            migrationBuilder.CreateIndex(
                name: "IX_PT_PhanCongHoaHongs_MaLopHoc",
                table: "PT_PhanCongHoaHongs",
                column: "MaLopHoc");

            migrationBuilder.CreateIndex(
                name: "IX_PT_PhanCongHoaHongs_MaPT_MaGoiTap",
                table: "PT_PhanCongHoaHongs",
                columns: new[] { "MaPT", "MaGoiTap" },
                unique: true,
                filter: "[MaGoiTap] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PT_PhanCongHoaHongs_MaPT_MaLopHoc",
                table: "PT_PhanCongHoaHongs",
                columns: new[] { "MaPT", "MaLopHoc" },
                unique: true,
                filter: "[MaLopHoc] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuCheckIns_TaiKhoans_MaTK",
                table: "LichSuCheckIns",
                column: "MaTK",
                principalTable: "TaiKhoans",
                principalColumn: "MaTK");

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuCheckIns_ThanhViens_ThanhVienMaTV",
                table: "LichSuCheckIns",
                column: "ThanhVienMaTV",
                principalTable: "ThanhViens",
                principalColumn: "MaTV");

            migrationBuilder.AddForeignKey(
                name: "FK_ThanhToans_KhachVangLais_NguoiDung_KhachVangLaiMaKVL",
                table: "ThanhToans",
                column: "NguoiDung_KhachVangLaiMaKVL",
                principalTable: "KhachVangLais",
                principalColumn: "MaKVL");

            migrationBuilder.AddForeignKey(
                name: "FK_ThanhToans_TaiKhoans_NguoiDung_TaiKhoanMaTK",
                table: "ThanhToans",
                column: "NguoiDung_TaiKhoanMaTK",
                principalTable: "TaiKhoans",
                principalColumn: "MaTK");

            migrationBuilder.AddForeignKey(
                name: "FK_ThongBaos_TaiKhoans_MaTK",
                table: "ThongBaos",
                column: "MaTK",
                principalTable: "TaiKhoans",
                principalColumn: "MaTK",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ThongBaos_ThanhViens_ThanhVienMaTV",
                table: "ThongBaos",
                column: "ThanhVienMaTV",
                principalTable: "ThanhViens",
                principalColumn: "MaTV");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LichSuCheckIns_TaiKhoans_MaTK",
                table: "LichSuCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuCheckIns_ThanhViens_ThanhVienMaTV",
                table: "LichSuCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_ThanhToans_KhachVangLais_NguoiDung_KhachVangLaiMaKVL",
                table: "ThanhToans");

            migrationBuilder.DropForeignKey(
                name: "FK_ThanhToans_TaiKhoans_NguoiDung_TaiKhoanMaTK",
                table: "ThanhToans");

            migrationBuilder.DropForeignKey(
                name: "FK_ThongBaos_TaiKhoans_MaTK",
                table: "ThongBaos");

            migrationBuilder.DropForeignKey(
                name: "FK_ThongBaos_ThanhViens_ThanhVienMaTV",
                table: "ThongBaos");

            migrationBuilder.DropTable(
                name: "CapNhatAnhNhanDiens");

            migrationBuilder.DropTable(
                name: "PT_PhanCongHoaHongs");

            migrationBuilder.DropIndex(
                name: "IX_ThanhToans_NguoiDung_KhachVangLaiMaKVL",
                table: "ThanhToans");

            migrationBuilder.DropIndex(
                name: "IX_ThanhToans_NguoiDung_TaiKhoanMaTK",
                table: "ThanhToans");

            migrationBuilder.DropIndex(
                name: "IX_LichSuCheckIns_MaTK",
                table: "LichSuCheckIns");

            migrationBuilder.DropColumn(
                name: "MaKVL_NguoiDung",
                table: "ThanhToans");

            migrationBuilder.DropColumn(
                name: "MaTK_NguoiDung",
                table: "ThanhToans");

            migrationBuilder.DropColumn(
                name: "NguoiDung_KhachVangLaiMaKVL",
                table: "ThanhToans");

            migrationBuilder.DropColumn(
                name: "NguoiDung_TaiKhoanMaTK",
                table: "ThanhToans");

            migrationBuilder.DropColumn(
                name: "MaTK",
                table: "LichSuCheckIns");

            migrationBuilder.RenameColumn(
                name: "ThanhVienMaTV",
                table: "ThongBaos",
                newName: "TaiKhoanMaTK");

            migrationBuilder.RenameColumn(
                name: "MaTK",
                table: "ThongBaos",
                newName: "MaTV");

            migrationBuilder.RenameIndex(
                name: "IX_ThongBaos_ThanhVienMaTV",
                table: "ThongBaos",
                newName: "IX_ThongBaos_TaiKhoanMaTK");

            migrationBuilder.RenameIndex(
                name: "IX_ThongBaos_MaTK",
                table: "ThongBaos",
                newName: "IX_ThongBaos_MaTV");

            migrationBuilder.RenameColumn(
                name: "ThanhVienMaTV",
                table: "LichSuCheckIns",
                newName: "MaTV");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuCheckIns_ThanhVienMaTV",
                table: "LichSuCheckIns",
                newName: "IX_LichSuCheckIns_MaTV");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "TinTucs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDangKy",
                table: "ThanhViens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "ThanhViens",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiGoiTap",
                table: "GoiTap",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DoanhThus",
                columns: table => new
                {
                    MaThu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKVL = table.Column<int>(type: "int", nullable: true),
                    MaNguoiThu = table.Column<int>(type: "int", nullable: true),
                    MaThanhToan = table.Column<int>(type: "int", nullable: true),
                    MaTV = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LoaiThu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayThu = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoanhThus", x => x.MaThu);
                    table.ForeignKey(
                        name: "FK_DoanhThus_KhachVangLais_MaKVL",
                        column: x => x.MaKVL,
                        principalTable: "KhachVangLais",
                        principalColumn: "MaKVL");
                    table.ForeignKey(
                        name: "FK_DoanhThus_TaiKhoans_MaNguoiThu",
                        column: x => x.MaNguoiThu,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTK");
                    table.ForeignKey(
                        name: "FK_DoanhThus_ThanhToans_MaThanhToan",
                        column: x => x.MaThanhToan,
                        principalTable: "ThanhToans",
                        principalColumn: "MaThanhToan");
                    table.ForeignKey(
                        name: "FK_DoanhThus_ThanhViens_MaTV",
                        column: x => x.MaTV,
                        principalTable: "ThanhViens",
                        principalColumn: "MaTV");
                });

            migrationBuilder.CreateTable(
                name: "PT_GoiTap",
                columns: table => new
                {
                    MaPT_GoiTap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGoiTap = table.Column<int>(type: "int", nullable: false),
                    MaPT = table.Column<int>(type: "int", nullable: false),
                    PhanTramHoaHong = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PT_GoiTap", x => x.MaPT_GoiTap);
                    table.ForeignKey(
                        name: "FK_PT_GoiTap_GoiTap_MaGoiTap",
                        column: x => x.MaGoiTap,
                        principalTable: "GoiTap",
                        principalColumn: "MaGoi",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PT_GoiTap_HuanLuyenViens_MaPT",
                        column: x => x.MaPT,
                        principalTable: "HuanLuyenViens",
                        principalColumn: "MaPT",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PT_LopHoc",
                columns: table => new
                {
                    MaPT_LopHoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLopHoc = table.Column<int>(type: "int", nullable: false),
                    MaPT = table.Column<int>(type: "int", nullable: false),
                    PhanTramHoaHong = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PT_LopHoc", x => x.MaPT_LopHoc);
                    table.ForeignKey(
                        name: "FK_PT_LopHoc_HuanLuyenViens_MaPT",
                        column: x => x.MaPT,
                        principalTable: "HuanLuyenViens",
                        principalColumn: "MaPT",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PT_LopHoc_LopHoc_MaLopHoc",
                        column: x => x.MaLopHoc,
                        principalTable: "LopHoc",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoanhThus_MaKVL",
                table: "DoanhThus",
                column: "MaKVL");

            migrationBuilder.CreateIndex(
                name: "IX_DoanhThus_MaNguoiThu",
                table: "DoanhThus",
                column: "MaNguoiThu");

            migrationBuilder.CreateIndex(
                name: "IX_DoanhThus_MaThanhToan",
                table: "DoanhThus",
                column: "MaThanhToan");

            migrationBuilder.CreateIndex(
                name: "IX_DoanhThus_MaTV",
                table: "DoanhThus",
                column: "MaTV");

            migrationBuilder.CreateIndex(
                name: "IX_PT_GoiTap_MaGoiTap",
                table: "PT_GoiTap",
                column: "MaGoiTap");

            migrationBuilder.CreateIndex(
                name: "IX_PT_GoiTap_MaPT",
                table: "PT_GoiTap",
                column: "MaPT");

            migrationBuilder.CreateIndex(
                name: "IX_PT_LopHoc_MaLopHoc",
                table: "PT_LopHoc",
                column: "MaLopHoc");

            migrationBuilder.CreateIndex(
                name: "IX_PT_LopHoc_MaPT",
                table: "PT_LopHoc",
                column: "MaPT");

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuCheckIns_ThanhViens_MaTV",
                table: "LichSuCheckIns",
                column: "MaTV",
                principalTable: "ThanhViens",
                principalColumn: "MaTV");

            migrationBuilder.AddForeignKey(
                name: "FK_ThongBaos_TaiKhoans_TaiKhoanMaTK",
                table: "ThongBaos",
                column: "TaiKhoanMaTK",
                principalTable: "TaiKhoans",
                principalColumn: "MaTK");

            migrationBuilder.AddForeignKey(
                name: "FK_ThongBaos_ThanhViens_MaTV",
                table: "ThongBaos",
                column: "MaTV",
                principalTable: "ThanhViens",
                principalColumn: "MaTV",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
