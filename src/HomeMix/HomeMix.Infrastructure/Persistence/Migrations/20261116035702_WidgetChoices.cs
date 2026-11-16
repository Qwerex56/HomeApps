using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WidgetChoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Widgets",
                table: "Installations",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Widgets",
                table: "Installations");
        }
    }
}
