using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstallationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddonId = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TargetKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreHearts",
                columns: table => new
                {
                    ListingKey = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreHearts", x => new { x.ListingKey, x.UserId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreEvents_AddonId_Source_Kind_At",
                table: "StoreEvents",
                columns: new[] { "AddonId", "Source", "Kind", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreHearts_UserId",
                table: "StoreHearts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreEvents");

            migrationBuilder.DropTable(
                name: "StoreHearts");
        }
    }
}
