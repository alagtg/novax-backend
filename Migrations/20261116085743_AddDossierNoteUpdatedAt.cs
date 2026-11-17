using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourProject.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDossierNoteUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DossierNotes",
                type: ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? "timestamp with time zone" : "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DossierNotes");
        }
    }
}
