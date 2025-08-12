using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMoTaAndThoiLuongToLopHoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "LopHocs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThoiLuong",
                table: "LopHocs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoLuongDaDat",
                table: "LichLops",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "DiemDanhs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LichLopId",
                table: "DiemDanhs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianCheckIn",
                table: "DiemDanhs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "DiemDanhs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LyDoHuy",
                table: "DangKys",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PhiDangKy",
                table: "DangKys",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NgayDat",
                table: "Bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_LichLopId",
                table: "DiemDanhs",
                column: "LichLopId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiemDanhs_LichLops_LichLopId",
                table: "DiemDanhs",
                column: "LichLopId",
                principalTable: "LichLops",
                principalColumn: "LichLopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiemDanhs_LichLops_LichLopId",
                table: "DiemDanhs");

            migrationBuilder.DropIndex(
                name: "IX_DiemDanhs_LichLopId",
                table: "DiemDanhs");

            migrationBuilder.DropColumn(
                name: "MoTa",
                table: "LopHocs");

            migrationBuilder.DropColumn(
                name: "ThoiLuong",
                table: "LopHocs");

            migrationBuilder.DropColumn(
                name: "SoLuongDaDat",
                table: "LichLops");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "DiemDanhs");

            migrationBuilder.DropColumn(
                name: "LichLopId",
                table: "DiemDanhs");

            migrationBuilder.DropColumn(
                name: "ThoiGianCheckIn",
                table: "DiemDanhs");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "DiemDanhs");

            migrationBuilder.DropColumn(
                name: "LyDoHuy",
                table: "DangKys");

            migrationBuilder.DropColumn(
                name: "PhiDangKy",
                table: "DangKys");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NgayDat",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "Bookings");
        }
    }
}
