using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Devices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeviceDescriptor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Devices",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "Devices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Capabilities",
                table: "Devices",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "Descriptor",
                table: "Devices",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptorVersion",
                table: "Devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "Devices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "Roles",
                table: "Devices",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Devices",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "Warnings",
                table: "Devices",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            // Existing rows: the old name (lowercase letters, digits, '-') is both the display name and the slug; a computer
            // stays a computer, a satellite is an assistant (it said nothing else before descriptors).
            migrationBuilder.Sql("UPDATE \"Devices\" SET \"Slug\" = \"Name\", \"Purpose\" = CASE \"Kind\" WHEN 'Satellite' THEN 'Assistant' ELSE 'Computer' END;");

            migrationBuilder.CreateTable(
                name: "RegisteredCapabilities",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredCapabilities", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_HouseholdId_Slug",
                table: "Devices",
                columns: new[] { "HouseholdId", "Slug" },
                unique: true,
                filter: "\"HouseholdId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_OwnerId_Slug",
                table: "Devices",
                columns: new[] { "OwnerId", "Slug" },
                unique: true,
                filter: "\"HouseholdId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Devices\" SET \"Name\" = left(\"Slug\", 20) WHERE length(\"Name\") > 20 OR \"Name\" !~ '^[a-z0-9][a-z0-9-]*$'");

            migrationBuilder.DropTable(
                name: "RegisteredCapabilities");

            migrationBuilder.DropIndex(
                name: "IX_Devices_HouseholdId_Slug",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_OwnerId_Slug",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Capabilities",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Descriptor",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "DescriptorVersion",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Roles",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Warnings",
                table: "Devices");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Devices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(240)",
                oldMaxLength: 240,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False");
        }
    }
}
