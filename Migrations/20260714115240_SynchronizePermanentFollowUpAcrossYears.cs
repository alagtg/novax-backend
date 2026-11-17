using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourProject.API.Migrations
{
    /// <inheritdoc />
    public partial class SynchronizePermanentFollowUpAcrossYears : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older yearly copies were created before these fields were cloned.
            // If a checkbox was selected in any year, keep it selected for all
            // dossiers sharing the same permanent code.
            if (migrationBuilder.ActiveProvider.Contains("Npgsql"))
            {
                migrationBuilder.Sql("""
                    UPDATE "Dossiers" AS target
                    SET "LettreMissionEnvoyee" = EXISTS (
                            SELECT 1 FROM "Dossiers" AS source
                            WHERE source."Code" = target."Code"
                              AND source."LettreMissionEnvoyee"
                        ),
                        "MandatEnvoye" = EXISTS (
                            SELECT 1 FROM "Dossiers" AS source
                            WHERE source."Code" = target."Code"
                              AND source."MandatEnvoye"
                        );
                    """);
            }
            else
            {
                migrationBuilder.Sql("""
                UPDATE target
                SET target.LettreMissionEnvoyee = valuesByCode.LettreMissionEnvoyee,
                    target.MandatEnvoye = valuesByCode.MandatEnvoye
                FROM Dossiers target
                CROSS APPLY (
                    SELECT
                        CAST(MAX(CAST(source.LettreMissionEnvoyee AS int)) AS bit) AS LettreMissionEnvoyee,
                        CAST(MAX(CAST(source.MandatEnvoye AS int)) AS bit) AS MandatEnvoye
                    FROM Dossiers source
                    WHERE source.Code = target.Code
                ) valuesByCode;
                """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
