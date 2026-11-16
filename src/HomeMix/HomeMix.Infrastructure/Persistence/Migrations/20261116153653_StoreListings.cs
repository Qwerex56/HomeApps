using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreListings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    AddonId = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: false),
                    Repository = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Official = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PackageSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReleaseTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Readme = table.Column<string>(type: "character varying(20008)", maxLength: 20008, nullable: true),
                    Problem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreListings", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreListings_RefreshedAt",
                table: "StoreListings",
                column: "RefreshedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoreListings_Status_SubmittedAt",
                table: "StoreListings",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreListings_SubmittedBy_LastSubmittedAt",
                table: "StoreListings",
                columns: new[] { "SubmittedBy", "LastSubmittedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreListings");
        }
    }
}
