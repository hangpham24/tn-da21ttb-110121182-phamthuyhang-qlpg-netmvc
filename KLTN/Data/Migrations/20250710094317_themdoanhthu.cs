using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLTN.Data.Migrations
{
    /// <inheritdoc />
    public partial class themdoanhthu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoanhThu",
                columns: table => new
                {
                    MaDoanhThu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThanhToan = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoanhThu", x => x.MaDoanhThu);
                    table.ForeignKey(
                        name: "FK_DoanhThu_TaiKhoans_NguoiTao",
                        column: x => x.NguoiTao,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoanhThu_ThanhToans_MaThanhToan",
                        column: x => x.MaThanhToan,
                        principalTable: "ThanhToans",
                        principalColumn: "MaThanhToan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoanhThu_MaThanhToan",
                table: "DoanhThu",
                column: "MaThanhToan");

            migrationBuilder.CreateIndex(
                name: "IX_DoanhThu_NguoiTao",
                table: "DoanhThu",
                column: "NguoiTao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoanhThu");
        }
    }
}
