using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCauHinhHoaHongTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CauHinhHoaHongs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CauHinhHoaHongs",
                columns: table => new
                {
                    CauHinhHoaHongId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoiTapId = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhanTramHoaHong = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_CauHinhHoaHongs_GoiTapId",
                table: "CauHinhHoaHongs",
                column: "GoiTapId");
        }
    }
}
