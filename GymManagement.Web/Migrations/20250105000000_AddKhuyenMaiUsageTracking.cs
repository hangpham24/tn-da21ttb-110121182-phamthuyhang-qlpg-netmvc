using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddKhuyenMaiUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhuyenMaiUsages",
                columns: table => new
                {
                    KhuyenMaiUsageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhuyenMaiId = table.Column<int>(type: "int", nullable: false),
                    NguoiDungId = table.Column<int>(type: "int", nullable: false),
                    ThanhToanId = table.Column<int>(type: "int", nullable: true),
                    DangKyId = table.Column<int>(type: "int", nullable: true),
                    SoTienGoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoTienGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoTienCuoi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgaySuDung = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_DangKyId",
                table: "KhuyenMaiUsages",
                column: "DangKyId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_KhuyenMaiId",
                table: "KhuyenMaiUsages",
                column: "KhuyenMaiId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_NgaySuDung",
                table: "KhuyenMaiUsages",
                column: "NgaySuDung");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_NguoiDungId",
                table: "KhuyenMaiUsages",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiUsages_ThanhToanId",
                table: "KhuyenMaiUsages",
                column: "ThanhToanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhuyenMaiUsages");
        }
    }
}
