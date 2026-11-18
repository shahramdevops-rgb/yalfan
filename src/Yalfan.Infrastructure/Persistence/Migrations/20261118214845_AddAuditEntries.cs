using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yalfan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    trace_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_entity_type_entity_id_occurred_at",
                table: "audit_entries",
                columns: new[] { "entity_type", "entity_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_user_id_occurred_at",
                table: "audit_entries",
                columns: new[] { "user_id", "occurred_at" });

            // Written by hand (ADR 0013): the audit is append-only in the database too, so a bug
            // or a hand-written query cannot erase history. Removing this trigger is a deliberate,
            // visible act.
            migrationBuilder.Sql("""
                CREATE FUNCTION audit_entries_append_only() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit_entries is append-only: % is not allowed', TG_OP;
                END;
                $$;

                CREATE TRIGGER audit_entries_no_update_or_delete
                    BEFORE UPDATE OR DELETE ON audit_entries
                    FOR EACH ROW EXECUTE FUNCTION audit_entries_append_only();

                CREATE TRIGGER audit_entries_no_truncate
                    BEFORE TRUNCATE ON audit_entries
                    FOR EACH STATEMENT EXECUTE FUNCTION audit_entries_append_only();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER audit_entries_no_truncate ON audit_entries;
                DROP TRIGGER audit_entries_no_update_or_delete ON audit_entries;
                DROP FUNCTION audit_entries_append_only();
                """);

            migrationBuilder.DropTable(
                name: "audit_entries");
        }
    }
}
