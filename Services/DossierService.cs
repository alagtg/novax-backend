using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YourProject.API.Data;
using YourProject.API.DTOs.Dossiers;
using YourProject.API.Models;
using YourProject.API.Models.Enums;

namespace YourProject.API.Services;

public class DossierService
{
    private readonly AppDbContext _db;
    private readonly FiscalYearService _fiscalYearService;
    private readonly IConfiguration _config;

    public DossierService(
        AppDbContext db,
        FiscalYearService fiscalYearService,
        IConfiguration config)
    {
        _db = db;
        _fiscalYearService = fiscalYearService;
        _config = config;
    }

    // ===============================
    // SEARCH (lecture autorisée même si clôturé)
    // ===============================
    public async Task<List<DossierDto>> Search(int year, string? q, int? employeeId, ModuleType? module)
    {
        var query = _db.Dossiers.AsQueryable()
                                .Where(d => d.Year == year);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim().ToLowerInvariant();
            query = query.Where(d =>
                d.Code.ToLower().Contains(q) ||
                d.CompanyName.ToLower().Contains(q) ||
                d.ResponsibleName.ToLower().Contains(q) ||
                d.Siret.ToLower().Contains(q));
        }

        if (employeeId.HasValue && module.HasValue)
        {
            query = query.Where(d =>
                d.Assignments.Any(a =>
                    a.UserId == employeeId.Value &&
                    a.Module == module.Value));
        }
        else if (employeeId.HasValue)
        {
            query = query.Where(d =>
                d.Assignments.Any(a =>
                    a.UserId == employeeId.Value));
        }

        return await query
            .OrderBy(d => d.CompanyName)
            .Select(d => new DossierDto
            {
                Id = d.Id,
                Year = d.Year,
                Code = d.Code,
                CompanyName = d.CompanyName,
                Sector = d.Sector,
                ResponsibleName = d.ResponsibleName,
                Phone = d.Phone,
                Email = d.Email,
                Siret = d.Siret,
                VatNumber = d.VatNumber,
                Rcs = d.Rcs,
                Fj = d.Fj,
                Address = d.Address,
                City = d.City,
                PostalCode = d.PostalCode,
                Country = d.Country,
                Notes = d.Notes,
                LettreMissionEnvoyee = d.LettreMissionEnvoyee,
                MandatEnvoye = d.MandatEnvoye,

                ComptabiliteUserId = d.Assignments
                    .Where(a => a.Module == ModuleType.Comptabilite)
                    .Select(a => (int?)a.UserId)
                    .FirstOrDefault(),

                ComptabiliteUserName = d.Assignments
                    .Where(a => a.Module == ModuleType.Comptabilite)
                    .Select(a => a.User != null ? a.User.FullName : null)
                    .FirstOrDefault(),

                // APRÈS — SocialUserId = le seul employé Social (après le fix Assign, il n'y en a qu'un)
                SocialUserId = d.Assignments
    .Where(a => a.Module == ModuleType.Social)
    .Select(a => (int?)a.UserId)
    .FirstOrDefault(),

                SocialUserIds = d.Assignments
    .Where(a => a.Module == ModuleType.Social)
    .Select(a => a.UserId)
    .ToList(),

                SocialUserName = string.Join(", ",
                    d.Assignments
                        .Where(a => a.Module == ModuleType.Social)
                        .Select(a => a.User != null ? a.User.FullName : "")
                ),

                JuridiqueUserId = d.Assignments
                    .Where(a => a.Module == ModuleType.Juridique)
                    .Select(a => (int?)a.UserId)
                    .FirstOrDefault(),

                JuridiqueUserName = d.Assignments
                    .Where(a => a.Module == ModuleType.Juridique)
                    .Select(a => a.User != null ? a.User.FullName : null)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    // ===============================
    // CREATE
    // ===============================
    public async Task<DossierDto> Create(SaveDossierRequest req)
    {
        if (await _fiscalYearService.IsClosed(req.Year))
            throw new Exception("Année clôturée. Modification impossible.");

        var d = new Dossier
        {
            Year = req.Year,
            Code = req.Code,
            CompanyName = req.CompanyName,
            Sector = req.Sector,
            ResponsibleName = req.ResponsibleName,
            Phone = req.Phone,
            Email = req.Email,
            Siret = req.Siret,
            VatNumber = req.VatNumber,
            Rcs = req.Rcs,   // ✅
            Fj = req.Fj,
            Address = req.Address,
            City = req.City,
            PostalCode = req.PostalCode,
            Country = req.Country,
            Notes = req.Notes,
            LettreMissionEnvoyee = req.LettreMissionEnvoyee,
            MandatEnvoye = req.MandatEnvoye
        };

        _db.Dossiers.Add(d);
        await _db.SaveChangesAsync();

        return await GetById(d.Id)
               ?? new DossierDto
               {
                   Id = d.Id,
                   Year = d.Year,
                   Code = d.Code,
                   CompanyName = d.CompanyName
               };
    }

    // ===============================
    // UPDATE
    // ===============================
    public async Task<DossierDto?> Update(int id, SaveDossierRequest req)
    {
        if (await _fiscalYearService.IsClosed(req.Year))
            throw new Exception("Année clôturée. Modification impossible.");

        var d = await _db.Dossiers.FindAsync(id);
        if (d == null) return null;

        d.Year = req.Year;
        d.Code = req.Code;
        d.CompanyName = req.CompanyName;
        d.Sector = req.Sector;
        d.ResponsibleName = req.ResponsibleName;
        d.Phone = req.Phone;
        d.Email = req.Email;
        d.Siret = req.Siret;
        d.VatNumber = req.VatNumber;
        d.Rcs = req.Rcs;   // ✅
        d.Fj = req.Fj;     // ✅
        d.Address = req.Address;
        d.City = req.City;
        d.PostalCode = req.PostalCode;
        d.Country = req.Country;
        d.Notes = req.Notes;
        d.LettreMissionEnvoyee = req.LettreMissionEnvoyee;
        d.MandatEnvoye = req.MandatEnvoye;

        await _db.SaveChangesAsync();

        return await GetById(d.Id);
    }

    // ===============================
    // DELETE
    // ===============================
    public async Task<bool> Delete(int id, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new Exception("Code de confirmation obligatoire.");

        var expectedCode = _config["AdminSecurity:DeleteCode"];

        if (string.IsNullOrWhiteSpace(expectedCode))
            throw new Exception("Code admin non configuré dans appsettings.");

        if (code.Trim() != expectedCode.Trim())
            throw new Exception("Code de confirmation incorrect.");

        var d = await _db.Dossiers
            .Include(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (d == null) return false;

        if (await _fiscalYearService.IsClosed(d.Year))
            throw new Exception("Année clôturée. Suppression impossible.");

        if (d.Assignments != null && d.Assignments.Any())
        {
            _db.DossierAssignments.RemoveRange(d.Assignments);
        }

        _db.Dossiers.Remove(d);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===============================
    // ASSIGN
    // ===============================
    public async Task<bool> Assign(int dossierId, int userId, ModuleType module)
    {
        var dossier = await _db.Dossiers.FindAsync(dossierId);
        var user = await _db.Users.FindAsync(userId);

        if (dossier == null || user == null)
            return false;

        if (!user.IsActive)
            throw new Exception("Cet utilisateur est inactif.");

        if (user.Role != UserRole.EMPLOYE)
            throw new Exception("Seuls les employés peuvent être assignés à un dossier.");

        if (await _fiscalYearService.IsClosed(dossier.Year))
            throw new Exception("Année clôturée. Assignation impossible.");

        var existingForModule = await _db.DossierAssignments
            .Where(a => a.DossierId == dossierId && a.Module == module)
            .ToListAsync();

        // APRÈS (correct : remplace l'employé Social comme Comptabilite/Juridique)
        if (module == ModuleType.Social)
        {
            var previousSocial = existingForModule.FirstOrDefault();

            // Déjà assigné au bon employé → rien à faire
            if (previousSocial != null && previousSocial.UserId == userId)
                return true;

            // Supprimer TOUTES les anciennes assignations Social
            if (existingForModule.Count > 0)
                _db.DossierAssignments.RemoveRange(existingForModule);

            // Ajouter la nouvelle assignation
            _db.DossierAssignments.Add(new DossierAssignment
            {
                DossierId = dossierId,
                UserId = userId,
                Module = module
            });

            // Transférer les TrackingRows (Fiches de Paie, DSN, DPAE)
            // Transférer les TrackingRows (Fiches de Paie, DSN, DPAE)
            if (previousSocial != null)
            {
                var oldUserId = previousSocial.UserId;

                // ✅ Filtrer par DossierId + AssignedToUserId seulement
                // (évite le problème de comparaison Module string vs enum)
                var trackingRows = await _db.TrackingRows
                    .Where(r => r.DossierId == dossierId
                             && r.AssignedToUserId == oldUserId)
                    .ToListAsync();

                foreach (var row in trackingRows)
                {
                    row.AssignedToUserId = userId;
                    row.UpdatedAt = DateTime.UtcNow;
                }
            }
            await _db.SaveChangesAsync();
            return true;
        }

        // Comptabilite / Juridique = un seul employé
        var previousAssignment = existingForModule.FirstOrDefault();

        // déjà assigné au bon employé
        if (previousAssignment != null && previousAssignment.UserId == userId)
            return true;

        // supprimer ancienne assignation
        if (existingForModule.Count > 0)
            _db.DossierAssignments.RemoveRange(existingForModule);

        // ajouter nouvelle assignation
        _db.DossierAssignments.Add(new DossierAssignment
        {
            DossierId = dossierId,
            UserId = userId,
            Module = module
        });

        // IMPORTANT :
        // garder les données existantes et transférer les TrackingRows
        if (previousAssignment != null)
        {
            var oldUserId = previousAssignment.UserId;

            var trackingRows = await _db.TrackingRows
                .Where(r => r.DossierId == dossierId
                         && r.Module == module
                         && r.AssignedToUserId == oldUserId)
                .ToListAsync();

            foreach (var row in trackingRows)
            {
                row.AssignedToUserId = userId;
                row.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }    // ===============================
    // UNASSIGN
    // ===============================
    public async Task<bool> Unassign(int dossierId, int userId, ModuleType module)
    {
        var a = await _db.DossierAssignments
            .FirstOrDefaultAsync(x =>
                x.DossierId == dossierId &&
                x.UserId == userId &&
                x.Module == module);

        if (a == null) return false;

        var dossier = await _db.Dossiers.FindAsync(dossierId);

        if (dossier != null && await _fiscalYearService.IsClosed(dossier.Year))
            throw new Exception("Année clôturée. Modification impossible.");

        _db.DossierAssignments.Remove(a);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===============================
    // CLONE
    // ===============================
    public async Task<int?> CloneToYear(int dossierId, int targetYear)
    {
        if (await _fiscalYearService.IsClosed(targetYear))
            throw new Exception("Année cible clôturée.");

        var src = await _db.Dossiers
            .Include(d => d.Assignments)
            .Include(d => d.Tasks)
            .FirstOrDefaultAsync(d => d.Id == dossierId);

        if (src == null) return null;

        var clone = new Dossier
        {
            Year = targetYear,
            Code = src.Code,
            CompanyName = src.CompanyName,
            Sector = src.Sector,
            ResponsibleName = src.ResponsibleName,
            Phone = src.Phone,
            Email = src.Email,
            Siret = src.Siret,
            VatNumber = src.VatNumber,
            Rcs = src.Rcs,
            Fj = src.Fj,
            Address = src.Address,
            City = src.City,
            PostalCode = src.PostalCode,
            Country = src.Country,
            Notes = src.Notes
        };

        _db.Dossiers.Add(clone);
        await _db.SaveChangesAsync();

        return clone.Id;
    }

    public async Task<DossierDto?> GetById(int id)
    {
        return await _db.Dossiers
            .Where(d => d.Id == id)
            .Select(d => new DossierDto
            {
                Id = d.Id,
                Year = d.Year,
                Code = d.Code,
                CompanyName = d.CompanyName,
                Sector = d.Sector,
                ResponsibleName = d.ResponsibleName,
                Phone = d.Phone,
                Email = d.Email,
                Siret = d.Siret,
                VatNumber = d.VatNumber,
                Rcs = d.Rcs,   // ✅
                Fj = d.Fj,
                Address = d.Address,
                City = d.City,
                PostalCode = d.PostalCode,
                Country = d.Country,
                Notes = d.Notes,
                LettreMissionEnvoyee = d.LettreMissionEnvoyee,
                MandatEnvoye = d.MandatEnvoye,

                ComptabiliteUserId = d.Assignments
                    .Where(a => a.Module == ModuleType.Comptabilite)
                    .Select(a => (int?)a.UserId)
                    .FirstOrDefault(),

                ComptabiliteUserName = d.Assignments
                    .Where(a => a.Module == ModuleType.Comptabilite)
                    .Select(a => a.User != null ? a.User.FullName : null)
                    .FirstOrDefault(),

                // APRÈS
                SocialUserId = d.Assignments
    .Where(a => a.Module == ModuleType.Social)
    .Select(a => (int?)a.UserId)
    .FirstOrDefault(),

                SocialUserName = string.Join(", ",
                    d.Assignments
                        .Where(a => a.Module == ModuleType.Social)
                        .Select(a => a.User != null ? a.User.FullName : "")
                ),

                JuridiqueUserId = d.Assignments
                    .Where(a => a.Module == ModuleType.Juridique)
                    .Select(a => (int?)a.UserId)
                    .FirstOrDefault(),

                JuridiqueUserName = d.Assignments
                    .Where(a => a.Module == ModuleType.Juridique)
                    .Select(a => a.User != null ? a.User.FullName : null)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
    }

}