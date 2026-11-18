using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Devices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JarvisDeviceSwitches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "JarvisAsks",
                table: "Devices",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "JarvisMayControl",
                table: "Devices",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JarvisAsks",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "JarvisMayControl",
                table: "Devices");
        }
    }
}
