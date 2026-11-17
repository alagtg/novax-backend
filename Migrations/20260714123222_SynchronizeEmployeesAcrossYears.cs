using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourProject.API.Migrations
{
    /// <inheritdoc />
    public partial class SynchronizeEmployeesAcrossYears : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("Npgsql"))
            {
                migrationBuilder.Sql("""
                    DELETE FROM "DossierAssignments" AS duplicate
                    USING "DossierAssignments" AS keeper
                    WHERE duplicate."DossierId" = keeper."DossierId"
                      AND duplicate."Module" = keeper."Module"
                      AND duplicate."Id" > keeper."Id";

                    WITH canonical AS (
                        SELECT DISTINCT ON (d."Code", a."Module")
                            d."Code", a."Module", a."UserId"
                        FROM "DossierAssignments" a
                        INNER JOIN "Dossiers" d ON d."Id" = a."DossierId"
                        ORDER BY d."Code", a."Module", d."Year" ASC, a."Id" ASC
                    )
                    UPDATE "DossierAssignments" AS target
                    SET "UserId" = canonical."UserId"
                    FROM "Dossiers" AS target_dossier, canonical
                    WHERE target."DossierId" = target_dossier."Id"
                      AND canonical."Code" = target_dossier."Code"
                      AND canonical."Module" = target."Module"
                      AND target."UserId" <> canonical."UserId";

                    WITH canonical AS (
                        SELECT DISTINCT ON (d."Code", a."Module")
                            d."Code", a."Module", a."UserId"
                        FROM "DossierAssignments" a
                        INNER JOIN "Dossiers" d ON d."Id" = a."DossierId"
                        ORDER BY d."Code", a."Module", d."Year" ASC, a."Id" ASC
                    )
                    INSERT INTO "DossierAssignments" ("CreatedAt", "DossierId", "UserId", "Module")
                    SELECT CURRENT_TIMESTAMP, target_dossier."Id", canonical."UserId", canonical."Module"
                    FROM "Dossiers" target_dossier
                    INNER JOIN canonical ON canonical."Code" = target_dossier."Code"
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "DossierAssignments" existing
                        WHERE existing."DossierId" = target_dossier."Id"
                          AND existing."Module" = canonical."Module"
                    );
                    """);
            }
            else
            {
                migrationBuilder.Sql("""
                    ;WITH duplicates AS (
                        SELECT Id,
                               ROW_NUMBER() OVER (PARTITION BY DossierId, Module ORDER BY Id) AS row_number
                        FROM DossierAssignments
                    )
                    DELETE FROM duplicates WHERE row_number > 1;

                    ;WITH canonical AS (
                        SELECT d.Code, a.Module, a.UserId,
                               ROW_NUMBER() OVER (
                                   PARTITION BY d.Code, a.Module
                                   ORDER BY d.Year ASC, a.Id ASC
                               ) AS row_number
                        FROM DossierAssignments a
                        INNER JOIN Dossiers d ON d.Id = a.DossierId
                    )
                    UPDATE target
                    SET target.UserId = canonical.UserId
                    FROM DossierAssignments target
                    INNER JOIN Dossiers targetDossier ON targetDossier.Id = target.DossierId
                    INNER JOIN canonical ON canonical.Code = targetDossier.Code
                                            AND canonical.Module = target.Module
                                            AND canonical.row_number = 1
                    WHERE target.UserId <> canonical.UserId;

                    ;WITH canonical AS (
                        SELECT d.Code, a.Module, a.UserId,
                               ROW_NUMBER() OVER (
                                   PARTITION BY d.Code, a.Module
                                   ORDER BY d.Year ASC, a.Id ASC
                               ) AS row_number
                        FROM DossierAssignments a
                        INNER JOIN Dossiers d ON d.Id = a.DossierId
                    )
                    INSERT INTO DossierAssignments (CreatedAt, DossierId, UserId, Module)
                    SELECT SYSUTCDATETIME(), targetDossier.Id, canonical.UserId, canonical.Module
                    FROM Dossiers targetDossier
                    INNER JOIN canonical ON canonical.Code = targetDossier.Code
                                        AND canonical.row_number = 1
                    WHERE NOT EXISTS (
                        SELECT 1 FROM DossierAssignments existing
                        WHERE existing.DossierId = targetDossier.Id
                          AND existing.Module = canonical.Module
                    );
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
