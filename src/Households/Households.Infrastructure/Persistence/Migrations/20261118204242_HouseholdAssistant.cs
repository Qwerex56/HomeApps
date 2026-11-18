using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Households.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdAssistant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every existing household gets its assistant with the defaults (all sources, read-only, 30/7 days).
            migrationBuilder.AddColumn<string>(
                name: "AssistantAccessLevel",
                table: "Households",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ReadOnly");

            migrationBuilder.AddColumn<bool>(
                name: "AssistantEnabled",
                table: "Households",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "AssistantEventDays",
                table: "Households",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "AssistantLocationEventDays",
                table: "Households",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<string>(
                name: "AssistantName",
                table: "Households",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "JARVIS");

            migrationBuilder.AddColumn<int>(
                name: "AssistantToolSources",
                table: "Households",
                type: "integer",
                nullable: false,
                defaultValue: 7);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssistantAccessLevel",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "AssistantEnabled",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "AssistantEventDays",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "AssistantLocationEventDays",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "AssistantName",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "AssistantToolSources",
                table: "Households");
        }
    }
}
