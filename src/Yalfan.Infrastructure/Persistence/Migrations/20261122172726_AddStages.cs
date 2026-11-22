using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yalfan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stages_name",
                table: "stages",
                column: "name",
                unique: true);

            // The workshop's stages, named by the developer on 1405/09/01 (ADR 0018). Inserted
            // once here rather than with HasData, so the operator can rename them later without
            // the model fighting back. The ids are version 7 with the same time and rising last
            // digits, so ordering by id lists them in the order of the work, before any stage
            // the operator adds. Written by the migration, so they have no audit entry.
            migrationBuilder.InsertData(
                table: "stages",
                columns: ["id", "name"],
                values: new object[,]
                {
                    { new Guid("01a2008d-7400-7000-8000-000000000001"), "برش" },
                    { new Guid("01a2008d-7400-7000-8000-000000000002"), "دوخت" },
                    { new Guid("01a2008d-7400-7000-8000-000000000003"), "بسته‌بندی" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stages");
        }
    }
}
