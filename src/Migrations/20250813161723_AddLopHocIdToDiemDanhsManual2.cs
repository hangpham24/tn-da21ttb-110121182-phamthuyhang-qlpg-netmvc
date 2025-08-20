using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLopHocIdToDiemDanhsManual2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
