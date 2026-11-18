using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Households.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDevice",
                table: "Memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDevice",
                table: "Memberships");
        }
    }
}
