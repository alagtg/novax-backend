using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourProject.API.Migrations
{
    public partial class AddTrackingBoard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Board",
                table: "TrackingRows",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Default");

            // Drop old unique index (Year, DossierId, AssignedToUserId, Module)
            migrationBuilder.DropIndex(
                name: "IX_TrackingRows_Year_DossierId_AssignedToUserId_Module",
                table: "TrackingRows");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRows_Year_DossierId_AssignedToUserId_Module_Board",
                table: "TrackingRows",
                columns: new[] { "Year", "DossierId", "AssignedToUserId", "Module", "Board" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrackingRows_Year_DossierId_AssignedToUserId_Module_Board",
                table: "TrackingRows");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRows_Year_DossierId_AssignedToUserId_Module",
                table: "TrackingRows",
                columns: new[] { "Year", "DossierId", "AssignedToUserId", "Module" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "Board",
                table: "TrackingRows");
        }
    }
}
