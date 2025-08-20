using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLichLopIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LichLopId",
                table: "DiemDanhs");

            migrationBuilder.DropColumn(
                name: "LichLopId",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LichLopId",
                table: "DiemDanhs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LichLopId",
                table: "Bookings",
                type: "int",
                nullable: true);
        }
    }
}
