using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourProject.API.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrackingRows_Year_DossierId_AssignedToUserId_Module",
                table: "TrackingRows");

            migrationBuilder.AddColumn<string>(
                name: "Board",
                table: "TrackingRows",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRows_Year_DossierId_AssignedToUserId_Module_Board",
                table: "TrackingRows",
                columns: new[] { "Year", "DossierId", "AssignedToUserId", "Module", "Board" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrackingRows_Year_DossierId_AssignedToUserId_Module_Board",
                table: "TrackingRows");

            migrationBuilder.DropColumn(
                name: "Board",
                table: "TrackingRows");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRows_Year_DossierId_AssignedToUserId_Module",
                table: "TrackingRows",
                columns: new[] { "Year", "DossierId", "AssignedToUserId", "Module" },
                unique: true);
        }
    }
}
