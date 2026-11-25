using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yalfan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserLoginNumbers : Migration
    {
        /// <summary>
        /// Moves each user's number into <c>user_login_numbers</c> (ADR 0026), so an owner account
        /// can log in with several. The copy runs in SQL and so writes no audit entry: each user's
        /// number is already in the audit of his creation, under <c>users</c>.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_login_numbers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_login_numbers", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_login_numbers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_login_numbers_mobile_number",
                table: "user_login_numbers",
                column: "mobile_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_login_numbers_user_id",
                table: "user_login_numbers",
                column: "user_id");

            // Every number stays switched on: being able to log in is decided by the user's own
            // active flag, as before.
            migrationBuilder.Sql("""
                INSERT INTO user_login_numbers (id, user_id, mobile_number, is_active)
                SELECT gen_random_uuid(), id, mobile_number, TRUE FROM users;
                """);

            // An account keeps one number switched on (ADR 0026), also in the database: two
            // requests switching off the account's two numbers at once would each see the other
            // still on. Checked at commit; the lock on the user's row makes the second wait for
            // the first and then see it, so one of them is refused.
            migrationBuilder.Sql("""
                CREATE FUNCTION user_keeps_a_login_number() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM 1 FROM users WHERE id = OLD.user_id FOR UPDATE;
                    IF FOUND AND NOT EXISTS (
                        SELECT 1 FROM user_login_numbers WHERE user_id = OLD.user_id AND is_active) THEN
                        RAISE EXCEPTION 'user % would have no login number switched on', OLD.user_id
                            USING ERRCODE = 'check_violation';
                    END IF;
                    RETURN NULL;
                END;
                $$;

                CREATE CONSTRAINT TRIGGER user_login_numbers_keep_one_on
                AFTER UPDATE OF is_active OR DELETE ON user_login_numbers
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION user_keeps_a_login_number();
                """);

            migrationBuilder.DropIndex(
                name: "ix_users_mobile_number",
                table: "users");

            migrationBuilder.DropColumn(
                name: "mobile_number",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                collation: "fa-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            // A code now belongs to the number it was sent to. Codes live two minutes and are not
            // audited, so the old ones are dropped rather than guessed a number.
            migrationBuilder.Sql("DELETE FROM otp_codes;");

            migrationBuilder.DropIndex(
                name: "ix_otp_codes_user_id_created_at",
                table: "otp_codes");

            migrationBuilder.AddColumn<string>(
                name: "mobile_number",
                table: "otp_codes",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_user_id_mobile_number_created_at",
                table: "otp_codes",
                columns: new[] { "user_id", "mobile_number", "created_at" });
        }

        /// <summary>Keeps each user's first number in number order; an owner account's others are lost.</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER user_login_numbers_keep_one_on ON user_login_numbers;
                DROP FUNCTION user_keeps_a_login_number();
                """);

            migrationBuilder.Sql("DELETE FROM otp_codes;");

            migrationBuilder.DropIndex(
                name: "ix_otp_codes_user_id_mobile_number_created_at",
                table: "otp_codes");

            migrationBuilder.DropColumn(
                name: "mobile_number",
                table: "otp_codes");

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_user_id_created_at",
                table: "otp_codes",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldCollation: "fa-x-icu");

            migrationBuilder.AddColumn<string>(
                name: "mobile_number",
                table: "users",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE users
                SET mobile_number = first.mobile_number
                FROM (SELECT DISTINCT ON (user_id) user_id, mobile_number
                      FROM user_login_numbers ORDER BY user_id, mobile_number) AS first
                WHERE users.id = first.user_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "mobile_number",
                table: "users",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_mobile_number",
                table: "users",
                column: "mobile_number",
                unique: true);

            migrationBuilder.DropTable(
                name: "user_login_numbers");
        }
    }
}
