using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLichLopsAndKhuyenMaiUsagesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop foreign key constraints referencing LichLops
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_LichLops_LichLopId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_DiemDanhs_LichLops_LichLopId", 
                table: "DiemDanhs");

            // Step 2: Drop indexes on tables that will be removed
            migrationBuilder.DropIndex(
                name: "IX_LichLop_LopHocId_Ngay",
                table: "LichLops");

            migrationBuilder.DropIndex(
                name: "IX_KhuyenMaiUsages_DangKyId",
                table: "KhuyenMaiUsages");

            migrationBuilder.DropIndex(
                name: "IX_KhuyenMaiUsages_KhuyenMaiId",
                table: "KhuyenMaiUsages");

            migrationBuilder.DropIndex(
                name: "IX_KhuyenMaiUsages_NgaySuDung",
                table: "KhuyenMaiUsages");

            migrationBuilder.DropIndex(
                name: "IX_KhuyenMaiUsages_NguoiDungId",
                table: "KhuyenMaiUsages");

            migrationBuilder.DropIndex(
                name: "IX_KhuyenMaiUsages_ThanhToanId",
                table: "KhuyenMaiUsages");

            // Step 3: Drop check constraints
            migrationBuilder.Sql("ALTER TABLE LichLops DROP CONSTRAINT IF EXISTS CK_LichLop_Status");
            migrationBuilder.Sql("ALTER TABLE LichLops DROP CONSTRAINT IF EXISTS CK_LichLop_TimeRange");

            // Step 4: Drop the tables (KhuyenMaiUsages first as it has no dependencies)
            migrationBuilder.DropTable(
                name: "KhuyenMaiUsages");

            migrationBuilder.DropTable(
                name: "LichLops");

            // Step 5: Set nullable foreign key columns to NULL
            migrationBuilder.Sql("UPDATE Bookings SET LichLopId = NULL WHERE LichLopId IS NOT NULL");
            migrationBuilder.Sql("UPDATE DiemDanhs SET LichLopId = NULL WHERE LichLopId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate LichLops table
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
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "SCHEDULED"),
                    SoLuongDaDat = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
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

            // Recreate KhuyenMaiUsages table
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

            // Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_LichLop_LopHocId_Ngay",
                table: "LichLops",
                columns: new[] { "LopHocId", "Ngay" });

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

            // Recreate foreign key constraints
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

            // Recreate check constraints
            migrationBuilder.Sql(@"
                ALTER TABLE LichLops 
                ADD CONSTRAINT CK_LichLop_Status 
                CHECK (TrangThai IN ('SCHEDULED', 'CANCELLED', 'FINISHED'))");

            migrationBuilder.Sql(@"
                ALTER TABLE LichLops 
                ADD CONSTRAINT CK_LichLop_TimeRange 
                CHECK (GioKetThuc > GioBatDau)");
        }
    }
}
