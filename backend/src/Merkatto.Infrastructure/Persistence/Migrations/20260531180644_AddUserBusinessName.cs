using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Merkatto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBusinessName : Migration
    {
        // Note: the auto-scaffolded migration also contained a large number of AlterColumn calls
        // converting SQLite types (TEXT/INTEGER) to Postgres types. Those came from a stale
        // model snapshot that had been regenerated under the SQLite provider; the real Postgres
        // schema (from InitialCreate) is already correctly typed, so those alters were redundant
        // no-ops and have been removed. The snapshot is now corrected to Postgres types. This
        // migration only adds the new business_name column.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "business_name",
                table: "users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "business_name",
                table: "users");
        }
    }
}
