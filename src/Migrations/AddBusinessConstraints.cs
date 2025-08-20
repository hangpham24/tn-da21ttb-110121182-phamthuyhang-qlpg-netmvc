using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <summary>
    /// Migration to add business logic constraints and performance indexes
    /// </summary>
    public partial class AddBusinessConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add constraint: GioKetThuc > GioBatDau for LopHoc
            migrationBuilder.Sql(@"
                ALTER TABLE LopHocs 
                ADD CONSTRAINT CK_LopHoc_TimeRange 
                CHECK (GioKetThuc > GioBatDau)");

            // 2. Add constraint: SucChua must be between 1 and 100
            migrationBuilder.Sql(@"
                ALTER TABLE LopHocs 
                ADD CONSTRAINT CK_LopHoc_Capacity 
                CHECK (SucChua > 0 AND SucChua <= 100)");

            // 3. Add constraint: ThoiLuong must be reasonable (15-300 minutes)
            migrationBuilder.Sql(@"
                ALTER TABLE LopHocs 
                ADD CONSTRAINT CK_LopHoc_Duration 
                CHECK (ThoiLuong IS NULL OR (ThoiLuong >= 15 AND ThoiLuong <= 300))");

            // 4. Add constraint: Valid TrangThai values for LopHoc
            migrationBuilder.Sql(@"
                ALTER TABLE LopHocs 
                ADD CONSTRAINT CK_LopHoc_Status 
                CHECK (TrangThai IN ('OPEN', 'CLOSED', 'FULL', 'CANCELLED'))");

            // 5. Add constraint: NgayKetThuc > NgayBatDau for DangKy
            migrationBuilder.Sql(@"
                ALTER TABLE DangKys 
                ADD CONSTRAINT CK_DangKy_DateRange 
                CHECK (NgayKetThuc > NgayBatDau)");

            // 6. Add constraint: Valid TrangThai values for DangKy
            migrationBuilder.Sql(@"
                ALTER TABLE DangKys 
                ADD CONSTRAINT CK_DangKy_Status 
                CHECK (TrangThai IN ('ACTIVE', 'EXPIRED', 'CANCELLED'))");

            // 7. Add constraint: GioKetThuc > GioBatDau for LichLop
            migrationBuilder.Sql(@"
                ALTER TABLE LichLops 
                ADD CONSTRAINT CK_LichLop_TimeRange 
                CHECK (GioKetThuc > GioBatDau)");

            // 8. Add constraint: Valid TrangThai values for LichLop
            migrationBuilder.Sql(@"
                ALTER TABLE LichLops 
                ADD CONSTRAINT CK_LichLop_Status 
                CHECK (TrangThai IN ('SCHEDULED', 'CANCELLED', 'FINISHED'))");

            // 9. Add performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_LopHoc_TrangThai_ThuTrongTuan",
                table: "LopHocs",
                columns: new[] { "TrangThai", "ThuTrongTuan" });

            migrationBuilder.CreateIndex(
                name: "IX_DangKy_NguoiDungId_TrangThai",
                table: "DangKys",
                columns: new[] { "NguoiDungId", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_LichLop_LopHocId_Ngay",
                table: "LichLops",
                columns: new[] { "LopHocId", "Ngay" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_LichLop_LopHocId_Ngay",
                table: "LichLops");

            migrationBuilder.DropIndex(
                name: "IX_DangKy_NguoiDungId_TrangThai",
                table: "DangKys");

            migrationBuilder.DropIndex(
                name: "IX_LopHoc_TrangThai_ThuTrongTuan",
                table: "LopHocs");

            // Drop constraints
            migrationBuilder.Sql("ALTER TABLE LichLops DROP CONSTRAINT CK_LichLop_Status");
            migrationBuilder.Sql("ALTER TABLE LichLops DROP CONSTRAINT CK_LichLop_TimeRange");
            migrationBuilder.Sql("ALTER TABLE DangKys DROP CONSTRAINT CK_DangKy_Status");
            migrationBuilder.Sql("ALTER TABLE DangKys DROP CONSTRAINT CK_DangKy_DateRange");
            migrationBuilder.Sql("ALTER TABLE LopHocs DROP CONSTRAINT CK_LopHoc_Status");
            migrationBuilder.Sql("ALTER TABLE LopHocs DROP CONSTRAINT CK_LopHoc_Duration");
            migrationBuilder.Sql("ALTER TABLE LopHocs DROP CONSTRAINT CK_LopHoc_Capacity");
            migrationBuilder.Sql("ALTER TABLE LopHocs DROP CONSTRAINT CK_LopHoc_TimeRange");
        }
    }
}
