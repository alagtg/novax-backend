using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourProject.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFournisseurToMonthlyFollowUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fournisseur",
                table: "MonthlyFollowUps",
                type: migrationBuilder.ActiveProvider.Contains("Npgsql") ? "text" : "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fournisseur",
                table: "MonthlyFollowUps");

        }
    }
}
