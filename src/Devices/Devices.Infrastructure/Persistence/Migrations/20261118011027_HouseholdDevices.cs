using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Devices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_OwnerId_Name",
                table: "Devices");

            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "Devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PairedAt",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PairingCodeHash",
                table: "Devices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PairingExpiresAt",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_HouseholdId_Name",
                table: "Devices",
                columns: new[] { "HouseholdId", "Name" },
                unique: true,
                filter: "\"HouseholdId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_OwnerId_Name",
                table: "Devices",
                columns: new[] { "OwnerId", "Name" },
                unique: true,
                filter: "\"HouseholdId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PairingCodeHash",
                table: "Devices",
                column: "PairingCodeHash",
                unique: true,
                filter: "\"PairingCodeHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_HouseholdId_Name",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_OwnerId_Name",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_PairingCodeHash",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PairedAt",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PairingCodeHash",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PairingExpiresAt",
                table: "Devices");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_OwnerId_Name",
                table: "Devices",
                columns: new[] { "OwnerId", "Name" },
                unique: true);
        }
    }
}
