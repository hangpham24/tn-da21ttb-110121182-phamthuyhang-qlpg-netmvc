using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixThanhToanDangKyIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ FIX: Make DangKyId nullable in ThanhToans table
            migrationBuilder.AlterColumn<int>(
                name: "DangKyId",
                table: "ThanhToans",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ✅ ROLLBACK: Make DangKyId NOT NULL again
            migrationBuilder.AlterColumn<int>(
                name: "DangKyId",
                table: "ThanhToans",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
