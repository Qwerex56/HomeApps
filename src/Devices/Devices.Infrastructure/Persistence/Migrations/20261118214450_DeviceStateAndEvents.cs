using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Devices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeviceStateAndEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsLocation = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceStates",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Values = table.Column<string>(type: "jsonb", nullable: false),
                    ValuesAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastStatusAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Battery = table.Column<double>(type: "double precision", nullable: true),
                    Voltage = table.Column<double>(type: "double precision", nullable: true),
                    Rssi = table.Column<int>(type: "integer", nullable: true),
                    Uptime = table.Column<long>(type: "bigint", nullable: true),
                    Firmware = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Error = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Silent = table.Column<bool>(type: "boolean", nullable: false),
                    LowBattery = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceStates", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_DeviceStates_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_DeviceId_OccurredAt",
                table: "DeviceEvents",
                columns: new[] { "DeviceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_HouseholdId_OccurredAt",
                table: "DeviceEvents",
                columns: new[] { "HouseholdId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceEvents");

            migrationBuilder.DropTable(
                name: "DeviceStates");
        }
    }
}
