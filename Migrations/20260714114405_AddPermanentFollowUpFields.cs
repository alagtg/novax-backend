using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourProject.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPermanentFollowUpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FollowUpStatus",
                table: "Dossiers",
                type: migrationBuilder.ActiveProvider.Contains("Npgsql") ? "text" : "nvarchar(max)",
                nullable: false,
                defaultValue: "enCours");

            migrationBuilder.AddColumn<string>(
                name: "FollowUpSupplier",
                table: "Dossiers",
                type: migrationBuilder.ActiveProvider.Contains("Npgsql") ? "text" : "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Preserve the most recently selected values and apply them to every
            // yearly copy sharing the same dossier code.
            if (migrationBuilder.ActiveProvider.Contains("Npgsql"))
            {
                migrationBuilder.Sql("""
                    UPDATE "Dossiers" AS d
                    SET "FollowUpSupplier" = COALESCE((
                            SELECT mf."Fournisseur"
                            FROM "MonthlyFollowUps" AS mf
                            INNER JOIN "Dossiers" AS source_dossier ON source_dossier."Id" = mf."DossierId"
                            WHERE source_dossier."Code" = d."Code"
                              AND NULLIF(BTRIM(mf."Fournisseur"), '') IS NOT NULL
                            ORDER BY mf."UpdatedAt" DESC
                            LIMIT 1
                        ), ''),
                        "FollowUpStatus" = COALESCE((
                            SELECT CASE
                                WHEN mf."Notes" LIKE '%#SORTIE%' THEN 'sortie'
                                WHEN mf."FacturePayee" THEN 'fait'
                                ELSE 'enCours'
                            END
                            FROM "MonthlyFollowUps" AS mf
                            INNER JOIN "Dossiers" AS source_dossier ON source_dossier."Id" = mf."DossierId"
                            WHERE source_dossier."Code" = d."Code"
                            ORDER BY mf."UpdatedAt" DESC
                            LIMIT 1
                        ), 'enCours');
                    """);
            }
            else
            {
                migrationBuilder.Sql("""
                UPDATE d
                SET d.FollowUpSupplier = COALESCE(s.Fournisseur, ''),
                    d.FollowUpStatus = CASE
                        WHEN st.Notes LIKE '%#SORTIE%' THEN 'sortie'
                        WHEN st.FacturePayee = 1 THEN 'fait'
                        ELSE 'enCours'
                    END
                FROM Dossiers d
                OUTER APPLY (
                    SELECT TOP (1) mf.Fournisseur
                    FROM MonthlyFollowUps mf
                    INNER JOIN Dossiers sourceDossier ON sourceDossier.Id = mf.DossierId
                    WHERE sourceDossier.Code = d.Code
                      AND NULLIF(LTRIM(RTRIM(mf.Fournisseur)), '') IS NOT NULL
                    ORDER BY mf.UpdatedAt DESC
                ) s
                OUTER APPLY (
                    SELECT TOP (1) mf.FacturePayee, mf.Notes
                    FROM MonthlyFollowUps mf
                    INNER JOIN Dossiers sourceDossier ON sourceDossier.Id = mf.DossierId
                    WHERE sourceDossier.Code = d.Code
                    ORDER BY mf.UpdatedAt DESC
                ) st;
                """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowUpStatus",
                table: "Dossiers");

            migrationBuilder.DropColumn(
                name: "FollowUpSupplier",
                table: "Dossiers");
        }
    }
}
