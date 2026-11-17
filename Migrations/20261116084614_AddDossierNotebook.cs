using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace YourProject.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDossierNotebook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var postgres = ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";

            migrationBuilder.CreateTable(
                name: "DossierNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: postgres ? "integer" : "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DossierId = table.Column<int>(type: postgres ? "integer" : "int", nullable: false),
                    AuthorId = table.Column<int>(type: postgres ? "integer" : "int", nullable: false),
                    AuthorName = table.Column<string>(type: postgres ? "character varying(200)" : "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: postgres ? "character varying(160)" : "nvarchar(160)", maxLength: 160, nullable: false),
                    Details = table.Column<string>(type: postgres ? "character varying(5000)" : "nvarchar(max)", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: postgres ? "timestamp with time zone" : "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossierNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DossierNotes_Dossiers_DossierId",
                        column: x => x.DossierId,
                        principalTable: "Dossiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DossierNotes_DossierId",
                table: "DossierNotes",
                column: "DossierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DossierNotes");
        }
    }
}
