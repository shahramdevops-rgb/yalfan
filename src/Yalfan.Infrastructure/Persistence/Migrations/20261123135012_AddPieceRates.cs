using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yalfan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPieceRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "piece_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    entered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_piece_rates", x => x.id);
                    table.CheckConstraint("ck_piece_rates_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_piece_rates_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_piece_rates_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_piece_rates_product_id_stage_id_starts_on",
                table: "piece_rates",
                columns: new[] { "product_id", "stage_id", "starts_on" });

            migrationBuilder.CreateIndex(
                name: "ix_piece_rates_stage_id",
                table: "piece_rates",
                column: "stage_id");

            // Written by hand (ADR 0021): the rate card is append-only in the database too, so an
            // old rate can never be overwritten, even by a bug or a hand-written query. The
            // function names the table in its message, so the later append-only tables (the
            // ledger, stock movements) can use it too. Removing a trigger is a deliberate,
            // visible act.
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_append_only_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION '% is append-only: % is not allowed', TG_TABLE_NAME, TG_OP;
                END;
                $$;

                CREATE TRIGGER piece_rates_no_update_or_delete
                    BEFORE UPDATE OR DELETE ON piece_rates
                    FOR EACH ROW EXECUTE FUNCTION reject_append_only_change();

                CREATE TRIGGER piece_rates_no_truncate
                    BEFORE TRUNCATE ON piece_rates
                    FOR EACH STATEMENT EXECUTE FUNCTION reject_append_only_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER piece_rates_no_truncate ON piece_rates;
                DROP TRIGGER piece_rates_no_update_or_delete ON piece_rates;
                DROP FUNCTION reject_append_only_change();
                """);

            migrationBuilder.DropTable(
                name: "piece_rates");
        }
    }
}
