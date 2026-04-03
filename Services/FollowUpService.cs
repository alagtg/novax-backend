using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.DTOs.FollowUp;
using YourProject.API.Models;

namespace YourProject.API.Services;

public class FollowUpService
{
    private readonly AppDbContext _db;
    public FollowUpService(AppDbContext db) => _db = db;

    public async Task<List<MonthlyFollowUpDto>> GetMonth(int year, int month, string? q)
    {
        if (month < 1 || month > 12) month = DateTime.UtcNow.Month;

        // Create missing follow-up rows for all dossiers of the year
        var dossierIds = await _db.Dossiers.Where(d => d.Year == year).Select(d => d.Id).ToListAsync();
        if (dossierIds.Count > 0)
        {
            var existing = await _db.MonthlyFollowUps
                .Where(f => f.Year == year && f.Month == month)
                .Select(f => f.DossierId)
                .ToListAsync();

            var missing = dossierIds.Except(existing).ToList();
            if (missing.Count > 0)
            {
                foreach (var id in missing)
                {
                    _db.MonthlyFollowUps.Add(new MonthlyFollowUp
                    {
                        Year = year,
                        Month = month,
                        DossierId = id,
                        FactureCreee = false,
                        FacturePayee = false,
                        Notes = "",
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync();
            }
        }

        var query = _db.MonthlyFollowUps
            .AsNoTracking()
            .Include(f => f.Dossier)
            .Where(f => f.Year == year && f.Month == month);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim().ToLowerInvariant();
            query = query.Where(f =>
                f.Dossier!.Code.ToLower().Contains(q) ||
                f.Dossier.CompanyName.ToLower().Contains(q) ||
                f.Dossier.Sector.ToLower().Contains(q));
        }

        return await query
            .OrderBy(f => f.Dossier!.CompanyName)
            .Select(f => new MonthlyFollowUpDto
            {
                Id = f.Id,
                Year = f.Year,
                Month = f.Month,
                DossierId = f.DossierId,
                DossierCode = f.Dossier!.Code,
                CompanyName = f.Dossier.CompanyName,
                Sector = f.Dossier.Sector,
                LettreMissionEnvoyee = f.Dossier.LettreMissionEnvoyee,
                MandatEnvoye = f.Dossier.MandatEnvoye,
                FactureCreee = f.FactureCreee,
                FacturePayee = f.FacturePayee,
                Notes = f.Notes,
                UpdatedAt = f.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<MonthlyFollowUpDto?> Update(int id, UpdateMonthlyFollowUpRequest req)
    {
        var row = await _db.MonthlyFollowUps
            .Include(f => f.Dossier)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (row == null) return null;

        if (req.FactureCreee.HasValue) row.FactureCreee = req.FactureCreee.Value;
        if (req.FacturePayee.HasValue) row.FacturePayee = req.FacturePayee.Value;
        if (req.Notes != null) row.Notes = req.Notes;

        if (row.Dossier != null)
        {
            if (req.LettreMissionEnvoyee.HasValue) row.Dossier.LettreMissionEnvoyee = req.LettreMissionEnvoyee.Value;
            if (req.MandatEnvoye.HasValue) row.Dossier.MandatEnvoye = req.MandatEnvoye.Value;
        }

        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new MonthlyFollowUpDto
        {
            Id = row.Id,
            Year = row.Year,
            Month = row.Month,
            DossierId = row.DossierId,
            DossierCode = row.Dossier?.Code ?? "",
            CompanyName = row.Dossier?.CompanyName ?? "",
            Sector = row.Dossier?.Sector ?? "",
            LettreMissionEnvoyee = row.Dossier?.LettreMissionEnvoyee ?? false,
            MandatEnvoye = row.Dossier?.MandatEnvoye ?? false,
            FactureCreee = row.FactureCreee,
            FacturePayee = row.FacturePayee,
            Notes = row.Notes,
            UpdatedAt = row.UpdatedAt
        };
    }

    /// <summary>
    /// Appelé par la facturation : marque automatiquement le mois concerné.
    /// </summary>
    public async Task UpsertFromInvoice(int dossierId, int year, int month, bool factureCreee, bool facturePayee)
    {
        if (month < 1 || month > 12) return;

        var row = await _db.MonthlyFollowUps.FirstOrDefaultAsync(f => f.DossierId == dossierId && f.Year == year && f.Month == month);
        if (row == null)
        {
            row = new MonthlyFollowUp
            {
                DossierId = dossierId,
                Year = year,
                Month = month,
                FactureCreee = factureCreee,
                FacturePayee = facturePayee,
                Notes = "",
                UpdatedAt = DateTime.UtcNow
            };
            _db.MonthlyFollowUps.Add(row);
        }
        else
        {
            // Ne jamais repasser à false automatiquement
            if (factureCreee) row.FactureCreee = true;
            if (facturePayee) row.FacturePayee = true;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
