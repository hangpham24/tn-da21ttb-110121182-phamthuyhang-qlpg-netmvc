using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLichLopAndKhuyenMaiUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_LichLops_LichLopId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_DiemDanhs_LichLops_LichLopId",
                table: "DiemDanhs");

            migrationBuilder.DropTable(
                name: "KhuyenMaiUsages");

            migrationBuilder.DropTable(
                name: "LichLops");

            migrationBuilder.DropIndex(
                name: "IX_DiemDanhs_LichLopId",
                table: "DiemDanhs");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_LichLopId",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "LopHocId",
                table: "DiemDanhs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_LopHocId",
                table: "DiemDanhs",
                column: "LopHocId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiemDanhs_LopHocs_LopHocId",
                table: "DiemDanhs",
                column: "LopHocId",
                principalTable: "LopHocs",
                principalColumn: "LopHocId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiemDanhs_LopHocs_LopHocId",
                table: "DiemDanhs");

            migrationBuilder.DropIndex(
                name: "IX_DiemDanhs_LopHocId",
                table: "DiemDanhs");

            migrationBuilder.DropColumn(
                name: "LopHocId",
                table: "DiemDanhs");

            migrationBuilder.CreateTable(
                name: "KhuyenMaiUsages",
                columns: table => new
                {
                    KhuyenMaiUsageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DangKyId = table.Column<int>(type: "int", nullable: true),
                    KhuyenMaiId = table.Column<int>(type: "int", nullable: false),
                    NguoiDungId = table.Column<int>(type: "int", nullable: false),
                    ThanhToanId = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgaySuDung = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoTienCuoi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoTienGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoTienGoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuyenMaiUsages", x => x.KhuyenMaiUsageId);
                    table.ForeignKey(
                        name: "FK_KhuyenMaiUsages_DangKys_DangKyId",
                        column: x => x.DangKyId,
                        principalTable: "DangKys",
                        principalColumn: "DangKyId");
                    table.ForeignKey(
                        name: "FK_KhuyenMaiUsages_KhuyenMais_KhuyenMaiId",
                        column: x => x.KhuyenMaiId,
                        principalTable: "KhuyenMais",
                        principalColumn: "KhuyenMaiId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KhuyenMaiUsages_NguoiDungs_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KhuyenMaiUsages_ThanhToans_ThanhToanId",
                        column: x => x.ThanhToanId,
                        principalTable: "ThanhToans",
                        principalColumn: "ThanhToanId");
                });

            migrationBuilder.CreateTable(
                name: "LichLops",
                columns: table => new
                {
                    LichLopId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LopHocId = table.Column<int>(type: "int", nullable: false),
                    GioBatDau = table.Column<TimeOnly>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeOnly>(type: "time", nullable: false),
                    Ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    SoLuongDaDat = table.Column<int>(type: "int", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_LichLopId",
                table: "DiemDanhs",
                column: "LichLopId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_LichLopId",
                table: "Bookings",
                column: "LichLopId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_DangKyId",
                table: "KhuyenMaiUsages",
                column: "DangKyId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_KhuyenMaiId",
                table: "KhuyenMaiUsages",
                column: "KhuyenMaiId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_NguoiDungId",
                table: "KhuyenMaiUsages",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_ThanhToanId",
                table: "KhuyenMaiUsages",
                column: "ThanhToanId");

            migrationBuilder.CreateIndex(
                name: "IX_LichLops_LopHocId",
                table: "LichLops",
                column: "LopHocId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_LichLops_LichLopId",
                table: "Bookings",
                column: "LichLopId",
                principalTable: "LichLops",
                principalColumn: "LichLopId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiemDanhs_LichLops_LichLopId",
                table: "DiemDanhs",
                column: "LichLopId",
                principalTable: "LichLops",
                principalColumn: "LichLopId");
        }
    }
}
