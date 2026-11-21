using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yalfan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "product_numbers",
                startValue: 101L);

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "colours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colours", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fabrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fabrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category_translations",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_translations", x => new { x.category_id, x.locale });
                    table.ForeignKey(
                        name: "fk_category_translations_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "colour_translations",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    colour_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colour_translations", x => new { x.colour_id, x.locale });
                    table.ForeignKey(
                        name: "fk_colour_translations_colours_colour_id",
                        column: x => x.colour_id,
                        principalTable: "colours",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fabric_translations",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    fabric_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fabric_translations", x => new { x.fabric_id, x.locale });
                    table.ForeignKey(
                        name: "fk_fabric_translations_fabrics_fabric_id",
                        column: x => x.fabric_id,
                        principalTable: "fabrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_sizes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_sizes", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_sizes_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_translations",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_translations", x => new { x.product_id, x.locale });
                    table.ForeignKey(
                        name: "fk_product_translations_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_size_translations",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    product_size_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_size_translations", x => new { x.product_size_id, x.locale });
                    table.ForeignKey(
                        name: "fk_product_size_translations_product_sizes_product_size_id",
                        column: x => x.product_size_id,
                        principalTable: "product_sizes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    sku = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fabric_id = table.Column<Guid>(type: "uuid", nullable: false),
                    colour_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_variants_colours_colour_id",
                        column: x => x.colour_id,
                        principalTable: "colours",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_variants_fabrics_fabric_id",
                        column: x => x.fabric_id,
                        principalTable: "fabrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_variants_product_sizes_size_id",
                        column: x => x.size_id,
                        principalTable: "product_sizes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_variants_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_translations_locale_name",
                table: "category_translations",
                columns: new[] { "locale", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_colour_translations_locale_name",
                table: "colour_translations",
                columns: new[] { "locale", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fabric_translations_locale_name",
                table: "fabric_translations",
                columns: new[] { "locale", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_sizes_product_id_position",
                table: "product_sizes",
                columns: new[] { "product_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_translations_locale_name",
                table: "product_translations",
                columns: new[] { "locale", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_colour_id",
                table: "product_variants",
                column: "colour_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_fabric_id",
                table: "product_variants",
                column: "fabric_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_id_fabric_id_colour_id_size_id",
                table: "product_variants",
                columns: new[] { "product_id", "fabric_id", "colour_id", "size_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_id_number",
                table: "product_variants",
                columns: new[] { "product_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_size_id",
                table: "product_variants",
                column: "size_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_sku",
                table: "product_variants",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_number",
                table: "products",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_translations");

            migrationBuilder.DropTable(
                name: "colour_translations");

            migrationBuilder.DropTable(
                name: "fabric_translations");

            migrationBuilder.DropTable(
                name: "product_size_translations");

            migrationBuilder.DropTable(
                name: "product_translations");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "colours");

            migrationBuilder.DropTable(
                name: "fabrics");

            migrationBuilder.DropTable(
                name: "product_sizes");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropSequence(
                name: "product_numbers");
        }
    }
}
