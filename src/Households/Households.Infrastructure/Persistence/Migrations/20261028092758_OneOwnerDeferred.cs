using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Households.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OneOwnerDeferred : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Memberships_OneOwner",
                table: "Memberships");

            // Checked at commit, so a handover may write the new Owner before the old one steps down.
            migrationBuilder.Sql(
                "ALTER TABLE \"Memberships\" ADD CONSTRAINT \"EX_Memberships_OneOwner\" " +
                "EXCLUDE USING btree (\"HouseholdId\" WITH =) WHERE (\"Role\" = 'Owner') DEFERRABLE INITIALLY DEFERRED;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Memberships\" DROP CONSTRAINT \"EX_Memberships_OneOwner\";");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_OneOwner",
                table: "Memberships",
                column: "HouseholdId",
                unique: true,
                filter: "\"Role\" = 'Owner'");
        }
    }
}
