using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Households.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeviceMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Memberships\" WHERE \"IsDevice\";");

            migrationBuilder.DropColumn(
                name: "IsDevice",
                table: "Memberships");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDevice",
                table: "Memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
