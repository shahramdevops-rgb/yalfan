using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yalfan.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Categories, fabrics, colours and stages can be deactivated, and stages have a place in the
    /// order of the work that the operator changes by dragging (ADR 0022). Every existing row
    /// starts active.
    /// </summary>
    public partial class AddActiveFlagsAndStageOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "stages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "stages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // The stages so far were ordered by id (ADR 0018), which put the three seeded ones in
            // the order of the work; their positions keep exactly that order (ADR 0022). Done by
            // the migration, not the API, so it writes no audit entry, like the seed itself.
            migrationBuilder.Sql("""
                UPDATE stages
                SET position = ordered.place
                FROM (SELECT id, row_number() OVER (ORDER BY id) AS place FROM stages) AS ordered
                WHERE stages.id = ordered.id;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "fabrics",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "colours",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "stages");

            migrationBuilder.DropColumn(
                name: "position",
                table: "stages");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "fabrics");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "colours");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "categories");
        }
    }
}
