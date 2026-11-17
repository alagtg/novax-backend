using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourProject.API.Migrations
{
    /// <inheritdoc />
    public partial class PreserveAllFollowUpColumnsAcrossYears : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FollowUpNotes",
                table: "Dossiers",
                type: migrationBuilder.ActiveProvider.Contains("Npgsql") ? "text" : "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            if (migrationBuilder.ActiveProvider.Contains("Npgsql"))
            {
                migrationBuilder.Sql("""
                    UPDATE "Dossiers" AS target
                    SET "FollowUpNotes" = COALESCE((
                            SELECT mf."Notes"
                            FROM "MonthlyFollowUps" AS mf
                            INNER JOIN "Dossiers" AS source_dossier ON source_dossier."Id" = mf."DossierId"
                            WHERE source_dossier."Code" = target."Code"
                            ORDER BY mf."UpdatedAt" DESC
                            LIMIT 1
                        ), ''),
                        "CompanyName" = (
                            SELECT source."CompanyName" FROM "Dossiers" AS source
                            WHERE source."Code" = target."Code"
                            ORDER BY source."Year" ASC LIMIT 1
                        ),
                        "Sector" = (
                            SELECT source."Sector" FROM "Dossiers" AS source
                            WHERE source."Code" = target."Code"
                            ORDER BY source."Year" ASC LIMIT 1
                        );
                    """);
            }
            else
            {
                migrationBuilder.Sql("""
                    UPDATE target
                    SET target.FollowUpNotes = COALESCE(latest.Notes, ''),
                        target.CompanyName = canonical.CompanyName,
                        target.Sector = canonical.Sector
                    FROM Dossiers target
                    OUTER APPLY (
                        SELECT TOP (1) mf.Notes
                        FROM MonthlyFollowUps mf
                        INNER JOIN Dossiers sourceDossier ON sourceDossier.Id = mf.DossierId
                        WHERE sourceDossier.Code = target.Code
                        ORDER BY mf.UpdatedAt DESC
                    ) latest
                    CROSS APPLY (
                        SELECT TOP (1) source.CompanyName, source.Sector
                        FROM Dossiers source
                        WHERE source.Code = target.Code
                        ORDER BY source.Year ASC
                    ) canonical;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowUpNotes",
                table: "Dossiers");
        }
    }
}
