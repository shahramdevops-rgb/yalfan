using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yalfan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SizePositionNotUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_sizes_product_id_position",
                table: "product_sizes");

            migrationBuilder.CreateIndex(
                name: "ix_product_sizes_product_id_position",
                table: "product_sizes",
                columns: new[] { "product_id", "position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_sizes_product_id_position",
                table: "product_sizes");

            migrationBuilder.CreateIndex(
                name: "ix_product_sizes_product_id_position",
                table: "product_sizes",
                columns: new[] { "product_id", "position" },
                unique: true);
        }
    }
}
