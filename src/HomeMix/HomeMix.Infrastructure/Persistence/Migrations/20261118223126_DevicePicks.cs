using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DevicePicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DevicePicks",
                table: "Installations",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DevicePicks",
                table: "Installations");
        }
    }
}
