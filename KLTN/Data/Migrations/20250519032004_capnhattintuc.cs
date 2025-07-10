using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLTN.Data.Migrations
{
    /// <inheritdoc />
    public partial class capnhattintuc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LuotXem",
                table: "TinTucs");

            migrationBuilder.DropColumn(
                name: "ThoiGianDoc",
                table: "TinTucs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LuotXem",
                table: "TinTucs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThoiGianDoc",
                table: "TinTucs",
                type: "int",
                nullable: true);
        }
    }
}
