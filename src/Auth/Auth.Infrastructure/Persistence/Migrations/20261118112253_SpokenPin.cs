using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SpokenPin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PinHash",
                table: "Identities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PinFailures",
                table: "DeviceIdentities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PinLockedUntil",
                table: "DeviceIdentities",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "PinFailures",
                table: "DeviceIdentities");

            migrationBuilder.DropColumn(
                name: "PinLockedUntil",
                table: "DeviceIdentities");
        }
    }
}
