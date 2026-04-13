using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.DTOs.Tracking;
using YourProject.API.Models;
using YourProject.API.Models.Enums;

namespace YourProject.API.Services;

public class TrackingService
{
    private readonly AppDbContext _db;

    public TrackingService(AppDbContext db)
    {
        _db = db;
    }

    private static Dictionary<string, object?> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static string Stringify(Dictionary<string, object?> data)
        => JsonSerializer.Serialize(data ?? new());

    private static Dictionary<string, object?> BuildInitialAudit(int userId, string actorName)
        => new()
        {
            ["__audit"] = new Dictionary<string, object?>
            {
                ["createdById"] = userId,
                ["createdByName"] = actorName,
                ["createdAtUtc"] = DateTime.UtcNow,
                ["totalEditMinutes"] = 0
            }
        };

    private static Dictionary<string, object?>? ReadAudit(Dictionary<string, object?> data)
    {
        if (!data.TryGetValue("__audit", out var raw) || raw == null) return null;

        if (raw is Dictionary<string, object?> dict)
            return dict;

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText());
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static bool TryParseDate(object? value, out DateTime result)
    {
        result = default;

        if (value == null) return false;

        if (value is DateTime dt)
        {
            result = dt;
            return true;
        }

        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.String && DateTime.TryParse(je.GetString(), out var fromString))
            {
                result = fromString;
                return true;
            }
            return false;
        }

        return DateTime.TryParse(value.ToString(), out result);
    }
    public async Task<UploadTrackingFileResponse?> UploadJuridiqueFile(
      int trackingRowId,
      int currentUserId,
      bool isAdmin,
      IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Aucun fichier reçu.");

        var row = await _db.TrackingRows
            .Include(r => r.Dossier)
            .Include(r => r.AssignedToUser)
            .FirstOrDefaultAsync(r => r.Id == trackingRowId);

        if (row == null)
            throw new InvalidOperationException("Ligne tracking introuvable.");

        if (!isAdmin && row.AssignedToUserId != currentUserId)
            throw new InvalidOperationException("Accès refusé pour ce fichier.");

        var allowedExtensions = new[]
        {
        ".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx", ".xls", ".xlsx"
    };

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";

        if (!allowedExtensions.Contains(ext))
            throw new InvalidOperationException($"Type de fichier non autorisé : {ext}");

        var maxSize = 10 * 1024 * 1024;
        if (file.Length > maxSize)
            throw new InvalidOperationException("Fichier trop volumineux. Taille max = 10 MB.");

        var company = row.Dossier?.CompanyName ?? "Dossier";

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeCompany = new string(company.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(safeCompany))
            safeCompany = "Dossier";

        var rootPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "juridique",
            row.Year.ToString(),
            safeCompany
        );

        Directory.CreateDirectory(rootPath);

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(rootPath, storedName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/juridique/{row.Year}/{safeCompany}/{storedName}".Replace("\\", "/");

        return new UploadTrackingFileResponse
        {
            FileName = file.FileName,
            FilePath = relativePath,
            FileSize = file.Length,
            ContentType = file.ContentType ?? "application/octet-stream"
        };
    }
    private static int GetAuditInt(Dictionary<string, object?> audit, string key)
    {
        if (!audit.TryGetValue(key, out var raw) || raw == null) return 0;

        if (raw is int i) return i;
        if (raw is long l) return (int)l;
        if (raw is decimal dec) return (int)dec;
        if (raw is double dbl) return (int)dbl;

        if (raw is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var n))
                return n;
            if (je.ValueKind == JsonValueKind.String && int.TryParse(je.GetString(), out var fromString))
                return fromString;
        }

        return int.TryParse(raw.ToString(), out var parsed) ? parsed : 0;
    }

    private static int ComputeSpentMinutes(DateTime? startedAt)
    {
        if (!startedAt.HasValue) return 0;

        var span = DateTime.UtcNow - startedAt.Value.ToUniversalTime();
        if (span.TotalMinutes <= 0) return 0;

        return Math.Min(8 * 60, (int)Math.Round(span.TotalMinutes));
    }

    private IQueryable<DossierAssignment> AssignmentsQuery(int year, ModuleType module)
        => _db.DossierAssignments
            .Include(a => a.Dossier)
            .Include(a => a.User)
            .Where(a => a.Module == module && a.Dossier!.Year == year);

    public async Task<List<TrackingRowDto>> Get(int year, ModuleType module, string board, int? userId, string? q, bool isAdmin)
    {
        if (isAdmin) userId = null;
        if (!isAdmin && !userId.HasValue) return new();

        if (AutoSeed(module, board))
            await EnsureRowsForAssignments(year, module, board, userId, isAdmin);

        var assignQuery = AssignmentsQuery(year, module);

        if (userId.HasValue)
            assignQuery = assignQuery.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim().ToLowerInvariant();
            assignQuery = assignQuery.Where(a =>
                a.Dossier!.Code.ToLower().Contains(qq) ||
                a.Dossier!.CompanyName.ToLower().Contains(qq) ||
                a.Dossier!.Sector.ToLower().Contains(qq));
        }

        var assignments = await assignQuery
            .Select(a => new { a.DossierId, a.UserId, UserName = a.User!.FullName })
            .Distinct()
            .ToListAsync();

        if (assignments.Count == 0) return new();

        var dossierIds = assignments.Select(x => x.DossierId).Distinct().ToList();
        var userIds = assignments.Select(x => x.UserId).Distinct().ToList();

        var existing = await _db.TrackingRows
            .Where(r => r.Year == year && r.Module == module && r.Board == board && dossierIds.Contains(r.DossierId) && userIds.Contains(r.AssignedToUserId))
            .Select(r => new { r.DossierId, r.AssignedToUserId })
            .ToListAsync();

        var existingSet = existing.Select(x => (x.DossierId, x.AssignedToUserId)).ToHashSet();

        var missing = assignments
            .Where(a => !existingSet.Contains((a.DossierId, a.UserId)))
            .Select(a => new TrackingRow
            {
                Year = year,
                DossierId = a.DossierId,
                AssignedToUserId = a.UserId,
                Module = module,
                Board = board,
                DataJson = Stringify(BuildInitialAudit(a.UserId, a.UserName)),
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missing.Count > 0)
        {
            _db.TrackingRows.AddRange(missing);
            await _db.SaveChangesAsync();
        }

        var rowsQuery = _db.TrackingRows
            .Include(r => r.Dossier)
            .Include(r => r.AssignedToUser)
            .Where(r => r.Year == year && r.Module == module && r.Board == board);

        if (userId.HasValue)
            rowsQuery = rowsQuery.Where(r => r.AssignedToUserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim().ToLowerInvariant();
            rowsQuery = rowsQuery.Where(r =>
                r.Dossier!.Code.ToLower().Contains(qq) ||
                r.Dossier!.CompanyName.ToLower().Contains(qq) ||
                r.Dossier!.Sector.ToLower().Contains(qq));
        }

        var rows = await rowsQuery
            .OrderBy(r => r.Dossier!.CompanyName)
            .ThenBy(r => r.AssignedToUser!.FullName)
            .ToListAsync();

        return rows.Select(r => new TrackingRowDto
        {
            Id = r.Id,
            Year = r.Year,
            DossierId = r.DossierId,
            DossierCode = r.Dossier?.Code ?? "",
            CompanyName = r.Dossier?.CompanyName ?? "",
            Sector = r.Dossier?.Sector ?? "",
            AssignedToUserId = r.AssignedToUserId,
            AssignedToName = r.AssignedToUser?.FullName ?? "",
            Module = r.Module,
            Board = r.Board,
            Data = Parse(r.DataJson),
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }

    private static string NormalizeText(object? value)
    {
        if (value == null) return "";
        return value.ToString()?.Trim().ToLowerInvariant() ?? "";
    }

    private static object? GetBaseColumnValue(TrackingRowDto row, string key)
    {
        return key switch
        {
            "companyName" => row.CompanyName,
            "sector" => row.Sector,
            "assignedToName" => row.AssignedToName,
            "dossierCode" => row.DossierCode,
            _ => null
        };
    }

    private static object? ReadJsonValue(object? raw)
    {
        if (raw == null) return null;

        if (raw is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.GetString(),
                JsonValueKind.Number => je.ToString(),
                JsonValueKind.True => "Oui",
                JsonValueKind.False => "Non",
                JsonValueKind.Null => null,
                _ => je.ToString()
            };
        }

        return raw;
    }

    private static object? GetNestedDataValue(Dictionary<string, object?> data, string key)
    {
        if (data == null) return null;

        if (data.TryGetValue(key, out var direct))
            return ReadJsonValue(direct);

        if (data.TryGetValue("months", out var monthsRaw) &&
            monthsRaw is JsonElement monthsJe &&
            monthsJe.ValueKind == JsonValueKind.Object)
        {
            foreach (var month in monthsJe.EnumerateObject())
            {
                if (month.Value.ValueKind == JsonValueKind.Object &&
                    month.Value.TryGetProperty(key, out var found))
                {
                    return ReadJsonValue(found);
                }
            }
        }

        if (data.TryGetValue("periodes", out var periodesRaw) &&
            periodesRaw is JsonElement periodesJe &&
            periodesJe.ValueKind == JsonValueKind.Object)
        {
            foreach (var periode in periodesJe.EnumerateObject())
            {
                if (periode.Value.ValueKind == JsonValueKind.Object &&
                    periode.Value.TryGetProperty(key, out var found))
                {
                    return ReadJsonValue(found);
                }
            }
        }

        if (data.TryGetValue("items", out var itemsRaw) &&
            itemsRaw is JsonElement itemsJe &&
            itemsJe.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in itemsJe.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty(key, out var found))
                {
                    return ReadJsonValue(found);
                }
            }
        }

        return null;
    }

    private static bool MatchColumnFilters(TrackingRowDto row, Dictionary<string, string>? filters)
    {
        if (filters == null || filters.Count == 0)
            return true;

        foreach (var kv in filters)
        {
            var key = kv.Key;
            var expected = NormalizeText(kv.Value);

            if (string.IsNullOrWhiteSpace(expected))
                continue;

            var baseValue = GetBaseColumnValue(row, key);
            var value = baseValue ?? GetNestedDataValue(row.Data, key);

            var actual = NormalizeText(value);

            if (!actual.Contains(expected))
                return false;
        }

        return true;
    }

    public async Task<YourProject.API.DTOs.Common.PagedResult<TrackingRowDto>> GetPaged(
        int year,
        ModuleType module,
        string board,
        int? userId,
        string? q,
        bool isAdmin,
        int page,
        int pageSize,
        Dictionary<string, string>? filters)
    {
        if (AutoSeed(module, board))
            await EnsureRowsForAssignments(year, module, board, userId, isAdmin);

        var rowsQuery = _db.TrackingRows.AsNoTracking()
            .Include(r => r.Dossier)
            .Include(r => r.AssignedToUser)
            .Where(r => r.Year == year && r.Module == module && r.Board == board);

        // ✅ CORRECTION ICI
        if (isAdmin)
        {
            if (userId.HasValue)
                rowsQuery = rowsQuery.Where(r => r.AssignedToUserId == userId.Value);
        }
        else
        {
            // si userId a une valeur => employé normal = ses dossiers seulement
            // si userId == null => accès global social = pas de filtre
            if (userId.HasValue)
                rowsQuery = rowsQuery.Where(r => r.AssignedToUserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim().ToLowerInvariant();
            rowsQuery = rowsQuery.Where(r =>
                r.Dossier!.Code.ToLower().Contains(qq) ||
                r.Dossier!.CompanyName.ToLower().Contains(qq) ||
                r.Dossier!.Sector.ToLower().Contains(qq));
        }

        var rows = await rowsQuery
            .OrderBy(r => r.Dossier!.CompanyName)
            .ThenBy(r => r.AssignedToUser!.FullName)
            .ToListAsync();

        var allItems = rows.Select(r => new TrackingRowDto
        {
            Id = r.Id,
            Year = r.Year,
            DossierId = r.DossierId,
            DossierCode = r.Dossier?.Code ?? "",
            CompanyName = r.Dossier?.CompanyName ?? "",
            Sector = r.Dossier?.Sector ?? "",
            AssignedToUserId = r.AssignedToUserId,
            AssignedToName = r.AssignedToUser?.FullName ?? "",
            Module = r.Module,
            Board = r.Board,
            Data = Parse(r.DataJson),
            UpdatedAt = r.UpdatedAt
        }).ToList();

        var filtered = allItems
            .Where(x => MatchColumnFilters(x, filters))
            .ToList();

        var totalItems = filtered.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var pagedItems = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new YourProject.API.DTOs.Common.PagedResult<TrackingRowDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            Items = pagedItems
        };
    }
    public async Task<YourProject.API.DTOs.Common.PagedResult<DTOs.Tracking.DossierLookDto>> LookPaged(
    int year,
    ModuleType module,
    int? userId,
    bool isAdmin,
    string? q,
    int page,
    int pageSize)
    {
        var query = AssignmentsQuery(year, module);

        if (isAdmin)
        {
            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);
        }
        else
        {
            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.Dossier!.Code.ToLower().Contains(qq) ||
                a.Dossier!.CompanyName.ToLower().Contains(qq) ||
                a.Dossier!.Sector.ToLower().Contains(qq));
        }

        var dtoQuery = query
            .OrderBy(a => a.Dossier!.CompanyName)
            .Select(a => new DTOs.Tracking.DossierLookDto
            {
                DossierId = a.DossierId,
                Code = a.Dossier!.Code,
                CompanyName = a.Dossier!.CompanyName,
                Sector = a.Dossier!.Sector,
                AssignedToUserId = a.UserId,
                AssignedToName = a.User!.FullName
            })
            .Distinct();

        return await YourProject.API.Helpers.QueryableExtensions.ToPagedAsync(dtoQuery, page, pageSize);
    }

    private async Task EnsureRowsForAssignments(int year, ModuleType module, string board, int? userId, bool isAdmin)
    {
        var assignQuery = AssignmentsQuery(year, module);

        if (isAdmin)
        {
            if (userId.HasValue)
                assignQuery = assignQuery.Where(a => a.UserId == userId.Value);
        }
        else
        {
            // employé normal => filtré sur lui
            // accès global social => userId null => pas de filtre
            if (userId.HasValue)
                assignQuery = assignQuery.Where(a => a.UserId == userId.Value);
        }

        var assignments = await assignQuery
            .Select(a => new { a.DossierId, UserId = a.UserId, UserName = a.User!.FullName })
            .Distinct()
            .ToListAsync();

        if (assignments.Count == 0) return;

        var dossierIds = assignments.Select(x => x.DossierId).Distinct().ToList();
        var userIds = assignments.Select(x => x.UserId).Distinct().ToList();

        var existing = await _db.TrackingRows
            .Where(r => r.Year == year && r.Module == module && r.Board == board
                        && dossierIds.Contains(r.DossierId)
                        && userIds.Contains(r.AssignedToUserId))
            .Select(r => new { r.DossierId, r.AssignedToUserId })
            .ToListAsync();

        var existingSet = existing.Select(x => (x.DossierId, x.AssignedToUserId)).ToHashSet();

        var missing = assignments
            .Where(a => !existingSet.Contains((a.DossierId, a.UserId)))
            .Select(a => new TrackingRow
            {
                Year = year,
                DossierId = a.DossierId,
                AssignedToUserId = a.UserId,
                Module = module,
                Board = board,
                DataJson = Stringify(BuildInitialAudit(a.UserId, a.UserName)),
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missing.Count > 0)
        {
            _db.TrackingRows.AddRange(missing);
            await _db.SaveChangesAsync();
        }
    }
    public async Task<TrackingRowDto?> Ensure(int year, ModuleType module, string board, int dossierId, int currentUserId, bool isAdmin, int? assignedToUserId)
    {
        var uid = isAdmin ? (assignedToUserId ?? currentUserId) : currentUserId;

        if (!isAdmin)
        {
            var ok = await _db.DossierAssignments.AnyAsync(a => a.DossierId == dossierId && a.UserId == uid && a.Module == module);
            if (!ok) return null;
        }
        else
        {
            var existsAssign = await _db.DossierAssignments.AnyAsync(a => a.DossierId == dossierId && a.UserId == uid && a.Module == module);
            if (!existsAssign)
            {
                _db.DossierAssignments.Add(new Models.DossierAssignment { DossierId = dossierId, UserId = uid, Module = module });
                await _db.SaveChangesAsync();
            }
        }

        var row = await _db.TrackingRows
            .Include(r => r.Dossier)
            .Include(r => r.AssignedToUser)
            .FirstOrDefaultAsync(r => r.Year == year && r.Module == module && r.Board == board && r.DossierId == dossierId && r.AssignedToUserId == uid);

        if (row == null)
        {
            var actor = await _db.Users.AsNoTracking().Where(u => u.Id == uid).Select(u => u.FullName).FirstOrDefaultAsync() ?? "Employé";
            row = new TrackingRow
            {
                Year = year,
                Module = module,
                Board = board,
                DossierId = dossierId,
                AssignedToUserId = uid,
                DataJson = Stringify(BuildInitialAudit(uid, actor)),
                UpdatedAt = DateTime.UtcNow
            };
            _db.TrackingRows.Add(row);
            await _db.SaveChangesAsync();

            row = await _db.TrackingRows
                .Include(r => r.Dossier)
                .Include(r => r.AssignedToUser)
                .FirstOrDefaultAsync(r => r.Id == row.Id);
        }

        if (row == null) return null;

        return new TrackingRowDto
        {
            Id = row.Id,
            Year = row.Year,
            DossierId = row.DossierId,
            DossierCode = row.Dossier?.Code ?? "",
            CompanyName = row.Dossier?.CompanyName ?? "",
            Sector = row.Dossier?.Sector ?? "",
            AssignedToUserId = row.AssignedToUserId,
            AssignedToName = row.AssignedToUser?.FullName ?? "",
            Module = row.Module,
            Board = row.Board,
            Data = Parse(row.DataJson),
            UpdatedAt = row.UpdatedAt
        };
    }

    public async Task<TrackingRowDto?> Update(int id, int currentUserId, bool isAdmin, SaveTrackingRowRequest req)
    {
        var row = await _db.TrackingRows
            .Include(r => r.Dossier)
            .Include(r => r.AssignedToUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (row == null) return null;

        if (!isAdmin)
        {
            var canEdit =
                row.AssignedToUserId == currentUserId;

            if (!canEdit && row.Module == ModuleType.Social)
            {
                canEdit = await UserCanAccessAllSocialDossiers(currentUserId);
            }

            if (!canEdit)
                return null;
        }

        var payload = req.Data ?? new();
        DateTime? startedAt = null;

        if (payload.TryGetValue("__editStartedAt", out var rawStart))
        {
            if (TryParseDate(rawStart, out var parsedStart))
                startedAt = parsedStart;

            payload.Remove("__editStartedAt");
        }

        var currentData = payload;

        var actorName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync() ?? row.AssignedToUser?.FullName ?? "Employé";

        var audit = ReadAudit(currentData);
        if (audit == null)
        {
            audit = new Dictionary<string, object?>
            {
                ["createdById"] = row.AssignedToUserId,
                ["createdByName"] = row.AssignedToUser?.FullName ?? actorName,
                ["createdAtUtc"] = row.CreatedAt,
                ["totalEditMinutes"] = 0
            };
        }

        audit["lastModifiedById"] = currentUserId;
        audit["lastModifiedByName"] = actorName;
        audit["lastModifiedAtUtc"] = DateTime.UtcNow;
        audit["totalEditMinutes"] =
            Math.Max(0, GetAuditInt(audit, "totalEditMinutes")) + ComputeSpentMinutes(startedAt);

        currentData["__audit"] = audit;

        row.DataJson = Stringify(currentData);
        row.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new TrackingRowDto
        {
            Id = row.Id,
            Year = row.Year,
            DossierId = row.DossierId,
            DossierCode = row.Dossier?.Code ?? "",
            CompanyName = row.Dossier?.CompanyName ?? "",
            Sector = row.Dossier?.Sector ?? "",
            AssignedToUserId = row.AssignedToUserId,
            AssignedToName = row.AssignedToUser?.FullName ?? "",
            Module = row.Module,
            Board = row.Board,
            Data = Parse(row.DataJson),
            UpdatedAt = row.UpdatedAt
        };
    }
    private static bool AutoSeed(ModuleType module, string board)
    {
        var b = (board ?? "").Trim().ToLowerInvariant();

        if (module == ModuleType.Social &&
            (b == "autorisationtravail" ||
             b == "ruptureconventionnelle" ||
             b == "dpae" ||
             b == "salarieslicencies" ||
             b == "stc" ||
             b == "blocregul" ||
             b == "contactdecaisse"))
        {
            return false;
        }

        if (module == ModuleType.Juridique)
        {
            return false;
        }

        return true;
    }
    public async Task<TrackingRowDto?> Create(
        int year,
        ModuleType module,
        string board,
        int dossierId,
        int currentUserId,
        bool isAdmin,
        int? assignedToUserId,
        Dictionary<string, object?>? data)
    {
        var uid = isAdmin ? (assignedToUserId ?? currentUserId) : currentUserId;

        if (!isAdmin)
        {
            var ok = await _db.DossierAssignments.AnyAsync(a =>
                a.DossierId == dossierId &&
                a.UserId == uid &&
                a.Module == module);

            if (!ok) return null;
        }
        else
        {
            var existsAssign = await _db.DossierAssignments.AnyAsync(a =>
                a.DossierId == dossierId &&
                a.UserId == uid &&
                a.Module == module);

            if (!existsAssign)
            {
                _db.DossierAssignments.Add(new DossierAssignment
                {
                    DossierId = dossierId,
                    UserId = uid,
                    Module = module
                });

                await _db.SaveChangesAsync();
            }
        }

        var actor = await _db.Users.AsNoTracking()
            .Where(u => u.Id == uid)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync() ?? "Employé";

        var payload = data ?? new Dictionary<string, object?>();

        if (!payload.ContainsKey("__audit"))
        {
            var audit = BuildInitialAudit(uid, actor);
            foreach (var kv in audit)
                payload[kv.Key] = kv.Value;
        }

        var row = new TrackingRow
        {
            Year = year,
            Module = module,
            Board = board,
            DossierId = dossierId,
            AssignedToUserId = uid,
            DataJson = Stringify(payload),
            UpdatedAt = DateTime.UtcNow
        };

        _db.TrackingRows.Add(row);
        await _db.SaveChangesAsync();

        row = await _db.TrackingRows
            .Include(r => r.Dossier)
            .Include(r => r.AssignedToUser)
            .FirstOrDefaultAsync(r => r.Id == row.Id);

        if (row == null) return null;

        return new TrackingRowDto
        {
            Id = row.Id,
            Year = row.Year,
            DossierId = row.DossierId,
            DossierCode = row.Dossier?.Code ?? "",
            CompanyName = row.Dossier?.CompanyName ?? "",
            Sector = row.Dossier?.Sector ?? "",
            AssignedToUserId = row.AssignedToUserId,
            AssignedToName = row.AssignedToUser?.FullName ?? "",
            Module = row.Module,
            Board = row.Board,
            Data = Parse(row.DataJson),
            UpdatedAt = row.UpdatedAt
        };
    }
    public async Task<bool> UserCanAccessAllSocialDossiers(int userId)
    {
        return await _db.Users
            .Where(u => u.Id == userId && u.Role == UserRole.EMPLOYE && u.IsActive)
            .Select(u => u.CanAccessAllSocialDossiers)
            .FirstOrDefaultAsync();
    }
    public async Task<bool> Delete(int id, int currentUserId, bool isAdmin)
    {
        var row = await _db.TrackingRows.FirstOrDefaultAsync(r => r.Id == id);
        if (row == null) return false;
        if (!isAdmin && row.AssignedToUserId != currentUserId) return false;

        _db.TrackingRows.Remove(row);
        await _db.SaveChangesAsync();

        return true;
    }
}