using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.DTOs.Dashboard;
using YourProject.API.Models;
using YourProject.API.Models.Enums;

namespace YourProject.API.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _db;
        public DashboardService(AppDbContext db) => _db = db;

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

        private static string? GetString(Dictionary<string, object?> d, string key)
            => d.TryGetValue(key, out var v) ? v?.ToString() : null;

        private static decimal? GetDecimal(Dictionary<string, object?> d, string key)
        {
            if (!d.TryGetValue(key, out var v) || v == null) return null;
            if (v is decimal dd) return dd;
            if (v is double db) return (decimal)db;
            if (v is float ff) return (decimal)ff;
            if (decimal.TryParse(v.ToString(), out var x)) return x;
            return null;
        }

        private static bool IsMonthlySocialBoard(string board)
        {
            var b = (board ?? "").Trim().ToLowerInvariant();
            return b == "fichespaie"
                   || b == "dsn"
                   || b == "dpae"
                   || b == "autorisationtravail"
                   || b == "ruptureconventionnelle"
                   || b == "salarieslicencies";
        }

        private static bool IsMonthlyComptaBoard(string board)
        {
            var b = (board ?? "").Trim().ToLowerInvariant();
            return b == "saisie" || b == "revision";
        }

        private static bool MatchesBiPeriod(int? month, string periodeType, string periodeValue)
        {
            if (!month.HasValue) return true;

            if (string.Equals(periodeType, "Annuel", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(periodeType, "Mensuel", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(periodeValue, out var m) && m == month.Value;

            if (string.Equals(periodeType, "Trimestriel", StringComparison.OrdinalIgnoreCase))
            {
                var q = (month.Value - 1) / 3 + 1;
                return string.Equals($"T{q}", periodeValue, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        public async Task<DashboardSummaryDto> GetSummary(int year, int? employeeId, int? dossierId, bool includeLines = false)
        {
            var now = DateTime.UtcNow;

            var dossiersQ = _db.Dossiers.AsNoTracking()
                .Where(d => d.Year == year);

            if (employeeId.HasValue)
                dossiersQ = dossiersQ.Where(d => d.Assignments.Any(a => a.UserId == employeeId.Value));

            if (dossierId.HasValue)
                dossiersQ = dossiersQ.Where(d => d.Id == dossierId.Value);

            var dossierCount = await dossiersQ.CountAsync();

            var tasksQ = _db.WorkTasks.AsNoTracking()
                .Where(t => t.Year == year);

            if (employeeId.HasValue)
                tasksQ = tasksQ.Where(t =>
                    t.AssignedToUserId == employeeId.Value ||
                    t.Dossier!.Assignments.Any(a => a.UserId == employeeId.Value));

            if (dossierId.HasValue)
                tasksQ = tasksQ.Where(t => t.DossierId == dossierId.Value);

            var totalTasks = await tasksQ.CountAsync();

            var totalRetards = await tasksQ
                .Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != WorkStatus.Fait)
                .CountAsync();

            var totalTvaADeclarer = await tasksQ
                .Where(t => t.Module == ModuleType.Comptabilite
                            && t.Title.ToLower().Contains("tva")
                            && t.Status != WorkStatus.Fait)
                .CountAsync();

            var totalIsAPayer = await tasksQ
                .Where(t => t.Module == ModuleType.Comptabilite
                            && (t.Title.ToLower().Contains("is") || t.Title.ToLower().Contains("impot"))
                            && t.Status != WorkStatus.Fait)
                .CountAsync();

            var tasksByStatusRaw = await tasksQ
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var tasksByModuleRaw = await tasksQ
                .GroupBy(t => new { t.Module, t.Status })
                .Select(g => new { g.Key.Module, g.Key.Status, Count = g.Count() })
                .ToListAsync();

            var dto = new DashboardSummaryDto
            {
                Year = year,
                TotalDossiers = dossierCount,
                TotalTasks = totalTasks,
                TotalRetards = totalRetards,
                TotalTvaADeclarer = totalTvaADeclarer,
                TotalIsAPayer = totalIsAPayer
            };

            if (dto.TotalRetards > 0)
                dto.Alerts.Add(new DashboardAlertDto { Type = "LATE_TASKS", Label = "Tâches en retard", Count = dto.TotalRetards });

            if (dto.TotalTvaADeclarer > 0)
                dto.Alerts.Add(new DashboardAlertDto { Type = "TVA_PENDING", Label = "TVA à déclarer", Count = dto.TotalTvaADeclarer });

            if (dto.TotalIsAPayer > 0)
                dto.Alerts.Add(new DashboardAlertDto { Type = "IS_PENDING", Label = "IS à payer", Count = dto.TotalIsAPayer });

            dto.MonthlyTasks = await tasksQ
                .Where(t => t.DueDate.HasValue)
                .GroupBy(t => t.DueDate!.Value.Month)
                .Select(g => new DashboardMonthlyPointDto { Month = g.Key, Value = g.Count() })
                .ToListAsync();

            if (!employeeId.HasValue)
            {
                var perfs = await _db.Users.AsNoTracking()
                    .Where(u => u.Role == UserRole.EMPLOYE)
                    .Select(u => new
                    {
                        u.Id,
                        Name = u.FullName,

                        Dossiers = _db.DossierAssignments.Count(a =>
                            a.UserId == u.Id &&
                            a.Dossier!.Year == year &&
                            (!dossierId.HasValue || a.DossierId == dossierId.Value)),

                        Tasks = _db.WorkTasks.Count(t =>
                            t.Year == year &&
                            t.AssignedToUserId == u.Id &&
                            (!dossierId.HasValue || t.DossierId == dossierId.Value)),

                        Retards = _db.WorkTasks.Count(t =>
                            t.Year == year &&
                            t.AssignedToUserId == u.Id &&
                            t.DueDate.HasValue &&
                            t.DueDate.Value < now &&
                            t.Status != WorkStatus.Fait &&
                            (!dossierId.HasValue || t.DossierId == dossierId.Value))
                    })
                    .ToListAsync();

                dto.PerformanceByCollaboratrice = perfs
                    .Select(p => new DashboardCollaboratorPerfDto
                    {
                        UserId = p.Id,
                        Name = p.Name,
                        Dossiers = p.Dossiers,
                        Tasks = p.Tasks,
                        Retards = p.Retards
                    })
                    .OrderByDescending(x => x.Tasks)
                    .ToList();
            }

            foreach (var s in Enum.GetValues<WorkStatus>())
                dto.TasksByStatus[s] = tasksByStatusRaw.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

            foreach (var m in Enum.GetValues<ModuleType>())
            {
                dto.TasksByModule[m] = new Dictionary<WorkStatus, int>();
                foreach (var s in Enum.GetValues<WorkStatus>())
                    dto.TasksByModule[m][s] = tasksByModuleRaw.FirstOrDefault(x => x.Module == m && x.Status == s)?.Count ?? 0;
            }

            var trackingQ = _db.TrackingRows.AsNoTracking()
                .Include(r => r.Dossier)
                .Include(r => r.AssignedToUser)
                .Where(r => r.Year == year);

            if (employeeId.HasValue)
                trackingQ = trackingQ.Where(r => r.AssignedToUserId == employeeId.Value);

            if (dossierId.HasValue)
                trackingQ = trackingQ.Where(r => r.DossierId == dossierId.Value);

            var rows = await trackingQ
                .OrderByDescending(r => r.UpdatedAt)
                .Take(includeLines ? 5000 : 800)
                .ToListAsync();

            var lines = BuildLines(rows);

            dto.BoardStats = lines
                .GroupBy(l => new { l.Module, l.Board })
                .Select(g => new DashboardBoardStatDto
                {
                    Module = g.Key.Module,
                    Board = g.Key.Board,
                    TotalRows = g.Count(),
                    LastUpdate = g.Max(x => x.UpdatedAt)
                })
                .OrderByDescending(x => x.LastUpdate)
                .ToList();

            dto.RecentLines = lines
                .OrderByDescending(l => l.UpdatedAt)
                .Take(20)
                .ToList();

            dto.Lines = includeLines ? lines : new List<DashboardLineDto>();

            return dto;
        }

        public async Task<List<DashboardLineDto>> GetLines(int year, int? employeeId, int? dossierId)
        {
            var trackingQ = _db.TrackingRows.AsNoTracking()
                .Include(r => r.Dossier)
                .Include(r => r.AssignedToUser)
                .Where(r => r.Year == year);

            if (employeeId.HasValue)
                trackingQ = trackingQ.Where(r => r.AssignedToUserId == employeeId.Value);

            if (dossierId.HasValue)
                trackingQ = trackingQ.Where(r => r.DossierId == dossierId.Value);

            var rows = await trackingQ
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            return BuildLines(rows);
        }

        public async Task<DashboardBiDto> GetBi(
            int year,
            int? employeeId,
            int? dossierId,
            string periodeType,
            string? periodeValue)
        {
            periodeType = string.IsNullOrWhiteSpace(periodeType) ? "Mensuel" : periodeType.Trim();
            periodeValue = ResolvePeriodeValue(periodeType, periodeValue);

            var dossiersQ = _db.Dossiers.AsNoTracking().Where(d => d.Year == year);

            if (employeeId.HasValue)
                dossiersQ = dossiersQ.Where(d => d.Assignments.Any(a => a.UserId == employeeId.Value));

            if (dossierId.HasValue)
                dossiersQ = dossiersQ.Where(d => d.Id == dossierId.Value);

            var totalDossiers = await dossiersQ.CountAsync();

            var lines = await GetLines(year, employeeId, dossierId);
            var filteredLines = FilterBiLines(lines, periodeType, periodeValue);

            var employeeRows = BuildEmployeeBiRows(filteredLines, year, dossierId);
            var clientRows = BuildClientBiRows(filteredLines, periodeValue);

            var dto = new DashboardBiDto
            {
                Year = year,
                PeriodeType = periodeType,
                PeriodeValue = periodeValue,
                TotalDossiers = totalDossiers,
                TotalEmployees = employeeRows.Count,
                Employees = employeeRows,
                Clients = clientRows
            };

            foreach (var e in employeeRows)
            {
                dto.EnCours += e.EnCours;
                dto.Fait += e.Fait;
                dto.Echouee += e.Echouee;
            }

            var totalStatuses = dto.EnCours + dto.Fait + dto.Echouee;
            dto.SuccessRate = totalStatuses > 0
                ? Math.Round((decimal)dto.Fait * 100m / totalStatuses, 2)
                : 0m;

            return dto;
        }

        private static string ResolvePeriodeValue(string periodeType, string? periodeValue)
        {
            if (!string.IsNullOrWhiteSpace(periodeValue))
                return periodeValue.Trim();

            if (string.Equals(periodeType, "Trimestriel", StringComparison.OrdinalIgnoreCase))
                return "T1";

            if (string.Equals(periodeType, "Annuel", StringComparison.OrdinalIgnoreCase))
                return "ANNUEL";

            return DateTime.UtcNow.Month.ToString();
        }

        private static List<DashboardLineDto> FilterBiLines(
            List<DashboardLineDto> lines,
            string periodeType,
            string periodeValue)
        {
            var result = new List<DashboardLineDto>();

            foreach (var l in lines)
            {
                if (l.Module == ModuleType.Social)
                {
                    if (MatchesBiPeriod(l.Month, periodeType, periodeValue))
                        result.Add(l);

                    continue;
                }

                if (l.Module == ModuleType.Comptabilite &&
                    string.Equals(l.Board, "TVA", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(periodeType, "Annuel", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(l);
                        continue;
                    }

                    if (string.Equals(periodeType, "Mensuel", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(periodeValue, out var m))
                        {
                            var p = (l.Periode ?? "").Trim().ToUpperInvariant();
                            if (p == m.ToString())
                                result.Add(l);
                        }

                        continue;
                    }

                    if (string.Equals(periodeType, "Trimestriel", StringComparison.OrdinalIgnoreCase))
                    {
                        var p = (l.Periode ?? "").Trim().ToUpperInvariant();
                        if (p == periodeValue.Trim().ToUpperInvariant())
                            result.Add(l);

                        continue;
                    }

                    continue;
                }

                if (l.Module == ModuleType.Comptabilite &&
                    (string.Equals(l.Board, "Saisie", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(l.Board, "Revision", StringComparison.OrdinalIgnoreCase)))
                {
                    if (MatchesBiPeriod(l.Month, periodeType, periodeValue))
                        result.Add(l);

                    continue;
                }

                if (l.Module == ModuleType.Comptabilite &&
                    (string.Equals(l.Board, "CFE", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(l.Board, "Bilan", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(l.Board, "IS", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(l.Board, "AGO", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(l.Board, "InfosGenerales", StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(l);
                    continue;
                }
            }

            return result;
        }

        private List<DashboardBiEmployeeDto> BuildEmployeeBiRows(List<DashboardLineDto> lines, int year, int? dossierId)
        {
            var employees = lines
                .GroupBy(x => new { x.AssignedToUserId, x.AssignedToName })
                .Select(g =>
                {
                    var row = new DashboardBiEmployeeDto
                    {
                        EmployeeId = g.Key.AssignedToUserId,
                        EmployeeName = g.Key.AssignedToName,
                        TotalDossiers = _db.DossierAssignments.AsNoTracking().Count(a =>
                            a.UserId == g.Key.AssignedToUserId &&
                            a.Dossier!.Year == year &&
                            (!dossierId.HasValue || a.DossierId == dossierId.Value))
                    };
                    row.InfosGeneralesStatus = AggregateBoardStatus(g.Where(x => x.Board == "InfosGenerales").Select(ResolveLineStatus));
                    row.AgoStatus = AggregateBoardStatus(g.Where(x => x.Board == "AGO").Select(ResolveLineStatus));
                    row.SaisieStatus = AggregateBoardStatus(g.Where(x => x.Board == "Saisie").Select(ResolveLineStatus));
                    row.RevisionStatus = AggregateBoardStatus(g.Where(x => x.Board == "Revision").Select(ResolveLineStatus));

                    row.TvaStatus = AggregateBoardStatus(g.Where(x => x.Board == "TVA").Select(ResolveLineStatus));
                    row.CfeStatus = AggregateBoardStatus(g.Where(x => x.Board == "CFE").Select(ResolveLineStatus));
                    row.IsStatus = AggregateBoardStatus(g.Where(x => x.Board == "IS").Select(ResolveLineStatus));
                    row.BilanStatus = AggregateBoardStatus(g.Where(x => x.Board == "Bilan").Select(ResolveLineStatus));

                    row.FichesPaieStatus = AggregateBoardStatus(g.Where(x => x.Board == "FichesPaie").Select(ResolveLineStatus));
                    row.DsnStatus = AggregateBoardStatus(g.Where(x => x.Board == "DSN").Select(ResolveLineStatus));
                    row.DpaeStatus = AggregateBoardStatus(g.Where(x => x.Board == "DPAE").Select(ResolveLineStatus));

                    foreach (var s in g.Select(ResolveLineStatus).Where(x => x.HasValue).Select(x => x!.Value))
                    {
                        if (s == 0) row.EnCours++;
                        else if (s == 1) row.Fait++;
                        else if (s == 2) row.Echouee++;
                    }

                    var total = row.EnCours + row.Fait + row.Echouee;
                    row.Performance = total > 0
                        ? Math.Round((decimal)row.Fait * 100m / total, 2)
                        : 0m;

                    return row;
                })
                .OrderByDescending(x => x.Performance)
                .ThenBy(x => x.EmployeeName)
                .ToList();

            return employees;
        }

        private static List<DashboardBiClientDto> BuildClientBiRows(List<DashboardLineDto> lines, string periodeValue)
        {
            return lines
                .GroupBy(x => new { x.DossierId, x.DossierCode, x.CompanyName })
                .Select(g =>
                {
                    var row = new DashboardBiClientDto
                    {
                        DossierId = g.Key.DossierId,
                        DossierCode = g.Key.DossierCode,
                        CompanyName = g.Key.CompanyName,
                        PeriodeLabel = periodeValue,
                        LastUpdate = g.OrderByDescending(x => x.UpdatedAt).Select(x => (DateTime?)x.UpdatedAt).FirstOrDefault()
                    };
                    row.InfosGeneralesStatus = AggregateBoardStatus(g.Where(x => x.Board == "InfosGenerales").Select(ResolveLineStatus));
                    row.AgoStatus = AggregateBoardStatus(g.Where(x => x.Board == "AGO").Select(ResolveLineStatus));
                    row.SaisieStatus = AggregateBoardStatus(g.Where(x => x.Board == "Saisie").Select(ResolveLineStatus));
                    row.RevisionStatus = AggregateBoardStatus(g.Where(x => x.Board == "Revision").Select(ResolveLineStatus));

                    row.TvaStatus = AggregateBoardStatus(g.Where(x => x.Board == "TVA").Select(ResolveLineStatus));
                    row.CfeStatus = AggregateBoardStatus(g.Where(x => x.Board == "CFE").Select(ResolveLineStatus));
                    row.IsStatus = AggregateBoardStatus(g.Where(x => x.Board == "IS").Select(ResolveLineStatus));
                    row.BilanStatus = AggregateBoardStatus(g.Where(x => x.Board == "Bilan").Select(ResolveLineStatus));

                    row.FichesPaieStatus = AggregateBoardStatus(g.Where(x => x.Board == "FichesPaie").Select(ResolveLineStatus));
                    row.DsnStatus = AggregateBoardStatus(g.Where(x => x.Board == "DSN").Select(ResolveLineStatus));
                    row.DpaeStatus = AggregateBoardStatus(g.Where(x => x.Board == "DPAE").Select(ResolveLineStatus));

                    row.ComptaStatus = AggregateBoardStatus(new int?[]
                    {
                        row.InfosGeneralesStatus,
                        row.AgoStatus,
                        row.SaisieStatus,
                        row.RevisionStatus
                    });

                    row.FiscalStatus = AggregateBoardStatus(new int?[]
                    {
                        row.TvaStatus,
                        row.CfeStatus,
                        row.IsStatus,
                        row.BilanStatus
                    });

                    row.SocialStatus = AggregateBoardStatus(new int?[]
                    {
                        row.FichesPaieStatus,
                        row.DsnStatus,
                        row.DpaeStatus
                    });

                    row.GlobalStatus = AggregateBoardStatus(new int?[]
                    {
                        row.ComptaStatus,
                        row.FiscalStatus,
                        row.SocialStatus
                    });

                    return row;
                })
                .OrderBy(x => x.DossierCode)
                .ToList();
        }

        private static int? ParseStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return null;

            if (int.TryParse(status, out var n))
            {
                if (n is 0 or 1 or 2) return n;
            }

            var s = status.Trim().ToLowerInvariant();

            if (s == "true" || s == "oui") return 1;
            if (s == "false" || s == "non") return 0;

            if (s.Contains("en cours")) return 0;
            if (s.Contains("fait") || s.Contains("pay") || s.Contains("valid")) return 1;
            if (s.Contains("échou") || s.Contains("echou") || s.Contains("bloq") || s.Contains("refus") || s.Contains("corrig")) return 2;

            return null;
        }
        private static string? InferBoardStatus(string board, Dictionary<string, object?> data)
        {
            string? yesNo(string key)
            {
                var v = GetString(data, key);
                if (string.IsNullOrWhiteSpace(v)) return null;

                var s = v.Trim().ToLowerInvariant();

                if (s == "oui" || s == "true" || s == "1") return "1";
                if (s == "non" || s == "false" || s == "0") return "0";

                return null;
            }

            if (string.Equals(board, "CFE", StringComparison.OrdinalIgnoreCase))
            {
                var payee = yesNo("payee");
                if (payee == "1") return "1";

                var declaree = yesNo("declaree");
                if (declaree == "1") return "0";
            }

            if (string.Equals(board, "Bilan", StringComparison.OrdinalIgnoreCase))
            {
                if (yesNo("declareeEdi") == "1" || yesNo("declareeManuel") == "1")
                    return "1";
            }

            if (string.Equals(board, "AGO", StringComparison.OrdinalIgnoreCase))
            {
                var validee = GetString(data, "validee");
                if (!string.IsNullOrWhiteSpace(validee))
                {
                    var s = validee.Trim().ToLowerInvariant();
                    if (s.Contains("valide")) return "1";
                    if (s.Contains("correction")) return "2";
                    if (s.Contains("en cours")) return "0";
                }

                if (yesNo("payee") == "1") return "1";
            }

            if (string.Equals(board, "DSN", StringComparison.OrdinalIgnoreCase) && yesNo("envoyee") == "1")
                return "1";

            if (string.Equals(board, "DPAE", StringComparison.OrdinalIgnoreCase) && yesNo("dpaeFaite") == "1")
                return "1";

            return null;
        }
        private static string NormalizeKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var s = value.Trim().ToLowerInvariant();

            s = s
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ê", "e")
                .Replace("à", "a")
                .Replace("ù", "u")
                .Replace("ô", "o")
                .Replace("î", "i")
                .Replace("ï", "i");

            return new string(s.Where(char.IsLetterOrDigit).ToArray());
        }

        private static string? ExtractManualStatus(Dictionary<string, object?> data)
        {
            if (data == null || data.Count == 0) return null;

            // 1) clés prioritaires exactes / fréquentes
            var preferredKeys = new[]
            {
        "statut",
        "status",
        "etat",
        "validation",
        "statutDossier",
        "validee",
        "isStatus",
        "statutIS",
        "statusIS"
    };

            foreach (var key in preferredKeys)
            {
                var value = GetString(data, key);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            // 2) recherche tolérante sur toutes les clés
            foreach (var kv in data)
            {
                var nk = NormalizeKey(kv.Key);

                if (nk == "statut" ||
                    nk == "status" ||
                    nk == "etat" ||
                    nk == "validation" ||
                    nk == "statutdossier" ||
                    nk == "validee" ||
                    nk == "isstatus" ||
                    nk == "statutis" ||
                    nk == "statusis")
                {
                    var value = kv.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return null;
        }


        private static int? ResolveLineStatus(DashboardLineDto line)
        {
            if (line == null) return null;

            // 1) status direct déjà calculé
            var direct = ParseStatus(line.Status);
            if (direct.HasValue) return direct.Value;

            var data = line.Data ?? new Dictionary<string, object?>();

            // 2) statut manuel depuis data
            var manual = ExtractManualStatus(data);
            var parsedManual = ParseStatus(manual);
            if (parsedManual.HasValue) return parsedManual.Value;

            // 3) fallback métier
            var inferred = InferBoardStatus(line.Board, data);
            var parsedInferred = ParseStatus(inferred);
            if (parsedInferred.HasValue) return parsedInferred.Value;

            return null;
        }
        private static int? AggregateBoardStatus(IEnumerable<int?> statuses)
        {
            var list = statuses.Where(x => x.HasValue).Select(x => x!.Value).ToList();
            if (!list.Any()) return null;

            if (list.Contains(2)) return 2;
            if (list.Contains(0)) return 0;
            if (list.Contains(1)) return 1;

            return null;
        }

        private List<DashboardLineDto> BuildLines(List<TrackingRow> rows)
        {
            var lines = new List<DashboardLineDto>();

            foreach (var r in rows)
            {
                var data = Parse(r.DataJson);

                if (r.Module == ModuleType.Social && IsMonthlySocialBoard(r.Board))
                {
                    if (data.TryGetValue("months", out var monthsObj) &&
                        monthsObj is JsonElement je &&
                        je.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var mProp in je.EnumerateObject())
                        {
                            if (!int.TryParse(mProp.Name, out var month)) continue;
                            if (mProp.Value.ValueKind != JsonValueKind.Object) continue;

                            var monthDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(mProp.Value.GetRawText()) ?? new();

                            string? status =
                                GetString(monthDict, "statut") ??
                                GetString(monthDict, "etat") ??
                                GetString(monthDict, "validee") ??
                                GetString(monthDict, "reguleDsn") ??
                                InferBoardStatus(r.Board, monthDict);

                            lines.Add(new DashboardLineDto
                            {
                                Module = r.Module,
                                Board = r.Board,
                                DossierId = r.DossierId,
                                DossierCode = r.Dossier?.Code ?? "",
                                CompanyName = r.Dossier?.CompanyName ?? "",
                                Sector = r.Dossier?.Sector ?? "",
                                AssignedToUserId = r.AssignedToUserId,
                                AssignedToName = r.AssignedToUser?.FullName ?? "",
                                Month = month,
                                Status = status,
                                Data = monthDict,
                                UpdatedAt = r.UpdatedAt,
                                IsLate = false
                            });
                        }

                        continue;
                    }
                }

                if (r.Module == ModuleType.Comptabilite && IsMonthlyComptaBoard(r.Board))
                {
                    if (data.TryGetValue("months", out var monthsObj) &&
                        monthsObj is JsonElement je &&
                        je.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var mProp in je.EnumerateObject())
                        {
                            if (!int.TryParse(mProp.Name, out var month)) continue;
                            if (mProp.Value.ValueKind != JsonValueKind.Object) continue;

                            var monthDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(mProp.Value.GetRawText()) ?? new();

                            string? status =
                                GetString(monthDict, "statut") ??
                                GetString(monthDict, "etat") ??
                                GetString(monthDict, "validation") ??
                                InferBoardStatus(r.Board, monthDict);

                            lines.Add(new DashboardLineDto
                            {
                                Module = r.Module,
                                Board = r.Board,
                                DossierId = r.DossierId,
                                DossierCode = r.Dossier?.Code ?? "",
                                CompanyName = r.Dossier?.CompanyName ?? "",
                                Sector = r.Dossier?.Sector ?? "",
                                AssignedToUserId = r.AssignedToUserId,
                                AssignedToName = r.AssignedToUser?.FullName ?? "",
                                Month = month,
                                Status = status,
                                Data = monthDict,
                                UpdatedAt = r.UpdatedAt,
                                IsLate = false
                            });
                        }

                        continue;
                    }
                }

                if (r.Module == ModuleType.Comptabilite &&
                    string.Equals(r.Board, "TVA", StringComparison.OrdinalIgnoreCase))
                {
                    var tvaType = GetString(data, "tvaType");

                    if (data.TryGetValue("periodes", out var periodesObj) &&
                        periodesObj is JsonElement pe &&
                        pe.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var pProp in pe.EnumerateObject())
                        {
                            var periode = pProp.Name;
                            if (pProp.Value.ValueKind != JsonValueKind.Object) continue;

                            var perDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(pProp.Value.GetRawText()) ?? new();

                            var amount = GetDecimal(perDict, "tvaNet") ?? GetDecimal(perDict, "tvaDue");
                            var status = GetString(perDict, "statut");

                            lines.Add(new DashboardLineDto
                            {
                                Module = r.Module,
                                Board = r.Board,
                                DossierId = r.DossierId,
                                DossierCode = r.Dossier?.Code ?? "",
                                CompanyName = r.Dossier?.CompanyName ?? "",
                                Sector = r.Dossier?.Sector ?? "",
                                AssignedToUserId = r.AssignedToUserId,
                                AssignedToName = r.AssignedToUser?.FullName ?? "",
                                Periode = periode,
                                TvaType = tvaType,
                                Status = status,
                                Amount = amount,
                                Data = perDict,
                                UpdatedAt = r.UpdatedAt,
                                IsLate = false
                            });
                        }

                        continue;
                    }
                }

                string? defaultStatus =
             ExtractManualStatus(data) ??
             GetString(data, "statut") ??
             GetString(data, "status") ??
             GetString(data, "statutIS") ??
             GetString(data, "statusIS") ??
             GetString(data, "isStatus") ??
             InferBoardStatus(r.Board, data);

                lines.Add(new DashboardLineDto
                {
                    Module = r.Module,
                    Board = r.Board,
                    DossierId = r.DossierId,
                    DossierCode = r.Dossier?.Code ?? "",
                    CompanyName = r.Dossier?.CompanyName ?? "",
                    Sector = r.Dossier?.Sector ?? "",
                    AssignedToUserId = r.AssignedToUserId,
                    AssignedToName = r.AssignedToUser?.FullName ?? "",
                    Status = defaultStatus,
                    Data = data,
                    UpdatedAt = r.UpdatedAt,
                    IsLate = false
                });
            }

            return lines;
        }
    }
}