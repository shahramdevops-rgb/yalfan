using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yalfan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contractors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "fa-x-icu"),
                    mobile_number = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    iban = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contractor_stages",
                columns: table => new
                {
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contractor_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_stages", x => new { x.contractor_id, x.stage_id });
                    table.ForeignKey(
                        name: "fk_contractor_stages_contractors_contractor_id",
                        column: x => x.contractor_id,
                        principalTable: "contractors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contractor_stages_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_stages_stage_id",
                table: "contractor_stages",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractors_mobile_number",
                table: "contractors",
                column: "mobile_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_stages");

            migrationBuilder.DropTable(
                name: "contractors");
        }
    }
}
