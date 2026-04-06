using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YourProject.API.Data;
using YourProject.API.DTOs.Billing;
using YourProject.API.Models;
using YourProject.API.Models.Billing;
using YourProject.API.Models.Enums;
using System.Diagnostics;
using YourProject.API.DTOs.Common;

namespace YourProject.API.Services;

public class BillingService
{
    private readonly AppDbContext _db;
    private readonly FollowUpService _followUp;
    private readonly IEmailSender _emailSender;

    public BillingService(
        AppDbContext db,
        FollowUpService followUp,
        IEmailSender emailSender)
    {
        _db = db;
        _followUp = followUp;
        _emailSender = emailSender;
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public async Task<BillingSettingsDto> GetSettings()
    {
        var s = await EnsureSettings();
        return ToSettingsDto(s);
    }

    public async Task<BillingSettingsDto> UpdateSettings(SaveBillingSettingsRequest req)
    {
        var s = await EnsureSettings();

        s.CompanyName = req.CompanyName;
        s.Address = req.Address;
        s.PostalCode = req.PostalCode;
        s.City = req.City;
        s.Phone = req.Phone;
        s.Email = req.Email;
        s.Siret = req.Siret;
        s.VatNumber = req.VatNumber;
        s.AccountHolder = req.AccountHolder;
        s.BankName = req.BankName;
        s.Agency = req.Agency;
        s.Iban = req.Iban;
        s.Bic = req.Bic;
        s.NumberingFormat = string.IsNullOrWhiteSpace(req.NumberingFormat) ? "{YYYY}-{SEQ}-C" : req.NumberingFormat;
        s.AnnualCounter = req.AnnualCounter < 1 ? 1 : req.AnnualCounter;
        s.Suffix = req.Suffix;

        await _db.SaveChangesAsync();
        return ToSettingsDto(s);
    }

    public async Task<BillingSettingsDto> UploadLogo(IFormFile file)
    {
        var s = await EnsureSettings();
        var url = await SaveFile(file, "logo", new[] { ".png", ".jpg", ".jpeg", ".webp" });
        s.LogoUrl = url;
        await _db.SaveChangesAsync();
        return ToSettingsDto(s);
    }

    public async Task<BillingSettingsDto> UploadTemplate(IFormFile file)
    {
        var s = await EnsureSettings();
        await SaveFile(file, "billing-template", new[] { ".pdf" });
        return ToSettingsDto(s);
    }

    // =========================================================
    // SEARCH / GET
    // =========================================================

    public async Task<PagedResult<InvoiceDto>> Search(
        int year,
        int? month,
        int? dossierId,
        string? q,
        PagedQuery paged)
    {
        var page = paged.SafePage;
        var pageSize = paged.SafePageSize(100);

        var query = _db.Invoices
            .Include(i => i.Dossier)
            .Include(i => i.Lines)
            .AsQueryable()
            .Where(i => i.Year == year);

        if (month.HasValue)
            query = query.Where(i => i.Month == month.Value);

        if (dossierId.HasValue)
            query = query.Where(i => i.DossierId == dossierId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim().ToLowerInvariant();
            query = query.Where(i =>
                i.Number.ToLower().Contains(q) ||
                (i.Dossier!.CompanyName ?? "").ToLower().Contains(q) ||
                (i.Dossier!.Code ?? "").ToLower().Contains(q) ||
                (i.Dossier!.Email ?? "").ToLower().Contains(q));
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderBy(i => i.Number) // tri principal sur numéro facture
            .ThenBy(i => i.Dossier!.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<InvoiceDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            Items = items.Select(ToDto).ToList()
        };
    }
    public async Task<int> DeleteMonth(int year, int month)
    {
        var invoices = await _db.Invoices
            .Include(x => x.Lines)
            .Where(x => x.Year == year && x.Month == month)
            .ToListAsync();

        if (!invoices.Any())
            return 0;

        var lines = invoices.SelectMany(x => x.Lines).ToList();
        if (lines.Any())
            _db.InvoiceLines.RemoveRange(lines);

        _db.Invoices.RemoveRange(invoices);
        await _db.SaveChangesAsync();

        return invoices.Count;
    }
    private async Task SaveWorksheetPdfToDisk(string originalWorkbookPath, string worksheetName, int invoiceId, int year, int month, string invoiceNumber)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "novax-billing", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var tempXlsx = Path.Combine(tempRoot, $"{SanitizeFileName(invoiceNumber)}.xlsx");
        var outputDir = Path.Combine(tempRoot, "pdf");
        Directory.CreateDirectory(outputDir);

        try
        {
            using (var workbook = new XLWorkbook(originalWorkbookPath))
            {
                var targetSheet = workbook.Worksheets.FirstOrDefault(w => w.Name == worksheetName);
                if (targetSheet == null)
                    throw new InvalidOperationException($"Feuille '{worksheetName}' introuvable dans le workbook.");

                // Supprimer toutes les autres feuilles
                var sheetsToDelete = workbook.Worksheets
                    .Where(w => !w.Name.Equals(worksheetName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var sheet in sheetsToDelete)
                {
                    workbook.Worksheet(sheet.Name).Delete();
                }

                // Sécuriser visibilité
                workbook.Worksheet(worksheetName).Visibility = XLWorksheetVisibility.Visible;

                workbook.SaveAs(tempXlsx);
            }

            var sofficePath = GetLibreOfficePath();

            var psi = new ProcessStartInfo
            {
                FileName = sofficePath,
                Arguments = $"--headless --convert-to pdf --outdir \"{outputDir}\" \"{tempXlsx}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("Impossible de démarrer LibreOffice.");

            var stdErr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Conversion PDF échouée. {stdErr}");

            var pdfName = Path.GetFileNameWithoutExtension(tempXlsx) + ".pdf";
            var generatedPdf = Path.Combine(outputDir, pdfName);

            if (!File.Exists(generatedPdf))
                throw new InvalidOperationException("PDF non généré par LibreOffice.");

            var finalFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "invoices",
                year.ToString(),
                month.ToString("00"));

            Directory.CreateDirectory(finalFolder);

            var safeNumber = SanitizeFileName(invoiceNumber);
            var uniqueFileName = $"{safeNumber}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.pdf";
            var finalPath = Path.Combine(finalFolder, uniqueFileName);

            File.Copy(generatedPdf, finalPath, true);

            var inv = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId);
            if (inv != null)
            {
                inv.PdfUrl = $"/uploads/invoices/{year}/{month:00}/{uniqueFileName}";
                await _db.SaveChangesAsync();
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
            catch
            {
            }
        }
    }
    private static string GetLibreOfficePath()
    {
        var possiblePaths = new[]
        {
        @"C:\Program Files\LibreOffice\program\soffice.exe",
        @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
    };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        return "soffice";
    }

    public async Task<InvoiceDto?> GetById(int id)
    {
        var inv = await _db.Invoices
            .Include(x => x.Dossier)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

        return inv == null ? null : ToDto(inv);
    }

    // =========================================================
    // CREATE / UPDATE
    // =========================================================

    public async Task<InvoiceDto> Create(SaveInvoiceRequest req)
    {
        var settings = await EnsureSettings();
        var number = BuildNumber(settings, req.Year);

        var inv = new Invoice
        {
            Year = req.Year,
            Month = req.Month,
            DossierId = req.DossierId,
            Number = number,
            IssueDate = req.IssueDate.Date,
            DueDate = req.DueDate?.Date,
            Status = req.Status,
            PaidAmount = req.PaidAmount,
            PaymentType = req.PaymentType
        };

        foreach (var l in req.Lines)
        {
            inv.Lines.Add(new InvoiceLine
            {
                Label = l.Label,
                Quantity = l.Quantity,
                UnitPriceHt = l.UnitPriceHt,
                VatRate = l.VatRate
            });
        }

        Recalculate(inv);

        _db.Invoices.Add(inv);
        settings.AnnualCounter += 1;
        await _db.SaveChangesAsync();

        await _followUp.UpsertFromInvoice(
            inv.DossierId,
            inv.Year,
            inv.Month,
            factureCreee: true,
            facturePayee: inv.Status == InvoiceStatus.Payee);

        inv = await _db.Invoices
            .Include(x => x.Dossier)
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == inv.Id);

        return ToDto(inv);
    }

    public async Task<InvoiceDto?> Update(int id, SaveInvoiceRequest req)
    {
        var inv = await _db.Invoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null) return null;

        inv.Year = req.Year;
        inv.Month = req.Month;
        inv.DossierId = req.DossierId;
        inv.IssueDate = req.IssueDate.Date;
        inv.DueDate = req.DueDate?.Date;
        inv.Status = req.Status;
        inv.PaidAmount = req.PaidAmount;
        inv.PaymentType = req.PaymentType;

        inv.Lines.Clear();

        foreach (var l in req.Lines)
        {
            inv.Lines.Add(new InvoiceLine
            {
                Label = l.Label,
                Quantity = l.Quantity,
                UnitPriceHt = l.UnitPriceHt,
                VatRate = l.VatRate
            });
        }

        Recalculate(inv);
        await _db.SaveChangesAsync();

        await _followUp.UpsertFromInvoice(
            inv.DossierId,
            inv.Year,
            inv.Month,
            factureCreee: true,
            facturePayee: inv.Status == InvoiceStatus.Payee);

        inv = await _db.Invoices
            .Include(x => x.Dossier)
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == id);

        return ToDto(inv);
    }

    public async Task<InvoiceDto?> UpdatePayment(int id, UpdateInvoicePaymentRequest req)
    {
        var inv = await _db.Invoices
            .Include(x => x.Dossier)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null) return null;

        inv.PaidAmount = req.PaidAmount;
        inv.PaymentType = req.PaymentType;

        Recalculate(inv);
        await _db.SaveChangesAsync();

        await _followUp.UpsertFromInvoice(
            inv.DossierId,
            inv.Year,
            inv.Month,
            factureCreee: true,
            facturePayee: inv.Status == InvoiceStatus.Payee);

        return ToDto(inv);
    }

    public async Task<InvoiceDto?> UploadInvoicePdf(int id, IFormFile file)
    {
        var inv = await _db.Invoices
            .Include(x => x.Dossier)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf")
            throw new InvalidOperationException("Seuls les PDF sont autorisés.");

        var folder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "invoices",
            inv.Year.ToString(),
            inv.Month.ToString("00"));

        Directory.CreateDirectory(folder);

        var fileName = $"{SanitizeFileName(inv.Number)}.pdf";
        var fullPath = Path.Combine(folder, fileName);

        using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream);

        inv.PdfUrl = $"/uploads/invoices/{inv.Year}/{inv.Month:00}/{fileName}";
        await _db.SaveChangesAsync();

        return ToDto(inv);
    }

    // =========================================================
    // IMPORT WORKBOOK (TON CAS REEL)
    // =========================================================

    public async Task<List<InvoiceDto>> ImportWorkbookInvoices(int year, int selectedMonth, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Fichier Excel manquant.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
            throw new InvalidOperationException("Le fichier doit être un Excel.");

        var tempImportFolder = Path.Combine(Path.GetTempPath(), "novax-imports");
        Directory.CreateDirectory(tempImportFolder);

        var originalWorkbookPath = Path.Combine(tempImportFolder, $"{Guid.NewGuid():N}{ext}");

        await using (var fs = File.Create(originalWorkbookPath))
        {
            await file.CopyToAsync(fs);
        }

        try
        {
            using var workbook = new XLWorkbook(originalWorkbookPath);
            var result = new List<InvoiceDto>();

            foreach (var ws in workbook.Worksheets)
            {
                var sheetName = (ws.Name ?? "").Trim();
                var lowerSheet = sheetName.ToLowerInvariant();

                if (lowerSheet.Contains("à ne pas facturé") || lowerSheet.Contains("a ne pas facture"))
                    continue;

                try
                {
                    var invoiceNumber = ReadText(ws.Cell("C13"));
                    if (string.IsNullOrWhiteSpace(invoiceNumber))
                        invoiceNumber = sheetName;

                    if (string.IsNullOrWhiteSpace(invoiceNumber))
                        continue;

                    var rawIssueDate = ReadDate(ws.Cell("C14"));
                    var issueDate = rawIssueDate ?? new DateTime(year, selectedMonth, 1);

                    if (issueDate.Month != selectedMonth || issueDate.Year != year)
                    {
                        var safeDay = Math.Min(issueDate.Day, DateTime.DaysInMonth(year, selectedMonth));
                        issueDate = new DateTime(year, selectedMonth, safeDay);
                    }

                    var clientName = ReadText(ws.Cell("H19"));
                    if (string.IsNullOrWhiteSpace(clientName))
                        continue;

                    var clientAddress = ReadText(ws.Cell("H20"));
                    var clientPostalCity = ReadText(ws.Cell("H21"));

                    var totalTtcCell = ReadDecimal(ws.Cell("J34"));
                    var paidAmount = ReadDecimal(ws.Cell("J35"));

                    var dossier = await GetOrCreateDossierFromSheet(
                        year,
                        clientName,
                        clientAddress,
                        clientPostalCity);

                    var existing = await _db.Invoices
                        .Include(x => x.Dossier)
                        .Include(x => x.Lines)
                        .FirstOrDefaultAsync(x =>
                            x.Year == year &&
                            x.Month == selectedMonth &&
                            x.Number == invoiceNumber);

                    Invoice inv;

                    if (existing == null)
                    {
                        inv = new Invoice
                        {
                            Year = year,
                            Month = selectedMonth,
                            DossierId = dossier.Id,
                            Number = invoiceNumber,
                            IssueDate = issueDate.Date,
                            Status = InvoiceStatus.Brouillon,
                            PaidAmount = paidAmount
                        };

                        _db.Invoices.Add(inv);
                    }
                    else
                    {
                        inv = existing;
                        inv.Year = year;
                        inv.Month = selectedMonth;
                        inv.DossierId = dossier.Id;
                        inv.Number = invoiceNumber;
                        inv.IssueDate = issueDate.Date;
                        inv.Status = InvoiceStatus.Brouillon;
                        inv.PaidAmount = paidAmount;

                        _db.InvoiceLines.RemoveRange(inv.Lines);
                        inv.Lines.Clear();
                    }

                    for (int row = 28; row <= 40; row++)
                    {
                        var description = ReadText(ws.Cell($"A{row}"));
                        var unitPrice = ReadDecimal(ws.Cell($"E{row}"));
                        var quantity = ReadDecimal(ws.Cell($"F{row}"));
                        var lineHt = ReadDecimal(ws.Cell($"G{row}"));
                        var vatRaw = ReadDecimal(ws.Cell($"H{row}"));
                        var lineTva = ReadDecimal(ws.Cell($"I{row}"));
                        var lineTtc = ReadDecimal(ws.Cell($"J{row}"));

                        if (string.IsNullOrWhiteSpace(description))
                            continue;

                        var d = description.ToLowerInvariant();
                        if (d.Contains("règlement") || d.Contains("reglement") || d.Contains("iban") || d.Contains("bic"))
                            continue;

                        if (quantity <= 0) quantity = 1;
                        var vatRate = vatRaw <= 1 ? vatRaw * 100m : vatRaw;

                        var line = new InvoiceLine
                        {
                            Label = description.Trim(),
                            Quantity = quantity,
                            UnitPriceHt = unitPrice,
                            VatRate = vatRate
                        };

                        line.LineHt = lineHt > 0 ? lineHt : (line.Quantity * line.UnitPriceHt);
                        line.LineTva = lineTva > 0 ? lineTva : (line.LineHt * (line.VatRate / 100m));
                        line.LineTtc = lineTtc > 0 ? lineTtc : (line.LineHt + line.LineTva);

                        inv.Lines.Add(line);
                    }

                    if (!inv.Lines.Any())
                    {
                        inv.Lines.Add(new InvoiceLine
                        {
                            Label = "Facture importée depuis Excel",
                            Quantity = 1,
                            UnitPriceHt = totalTtcCell,
                            VatRate = 0
                        });
                    }

                    Recalculate(inv);

                    inv.PaidAmount = paidAmount;
                    inv.RemainingAmount = Math.Max(0, inv.TotalTtc - inv.PaidAmount);

                    if (inv.RemainingAmount == 0 && inv.TotalTtc > 0)
                        inv.Status = InvoiceStatus.Payee;
                    else
                        inv.Status = InvoiceStatus.Brouillon;

                    await _db.SaveChangesAsync();

                    await _followUp.UpsertFromInvoice(
                        inv.DossierId,
                        inv.Year,
                        inv.Month,
                        factureCreee: true,
                        facturePayee: inv.Status == InvoiceStatus.Payee);

                    await SaveWorksheetPdfToDisk(originalWorkbookPath, sheetName, inv.Id, inv.Year, inv.Month, inv.Number);

                    inv = await _db.Invoices
                        .Include(x => x.Dossier)
                        .Include(x => x.Lines)
                        .FirstAsync(x => x.Id == inv.Id);

                    result.Add(ToDto(inv));
                }
                catch (DbUpdateException ex)
                {
                    var detail = ex.InnerException?.Message ?? ex.GetBaseException().Message;
                    throw new InvalidOperationException(
                        $"Erreur feuille '{sheetName}' : {detail}", ex);
                }
                catch (Exception ex)
                {
                    var detail = ex.GetBaseException().Message;
                    throw new InvalidOperationException(
                        $"Erreur feuille '{sheetName}' : {detail}", ex);
                }
            }

            return result;
        }
        finally
        {
            try
            {
                if (File.Exists(originalWorkbookPath))
                    File.Delete(originalWorkbookPath);
            }
            catch
            {
            }
        }
    }    // =========================================================
    // EMAIL
    // =========================================================

    public async Task<bool> SendInvoiceEmail(int id)
    {
        var inv = await _db.Invoices
            .Include(x => x.Dossier)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null || inv.Dossier == null || string.IsNullOrWhiteSpace(inv.Dossier.Email))
            return false;

        byte[]? pdfBytes = null;
        var fileName = $"{SanitizeFileName(inv.Number)}.pdf";

        if (!string.IsNullOrWhiteSpace(inv.PdfUrl))
        {
            var relative = inv.PdfUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);

            if (File.Exists(physical))
                pdfBytes = await File.ReadAllBytesAsync(physical);
        }

        if (pdfBytes == null)
            pdfBytes = await GeneratePdf(id);

        if (pdfBytes == null)
            return false;

        var subject = $"Facture {inv.Number}";
        var body =
$@"Bonjour,

Veuillez trouver ci-joint votre facture.

Numéro : {inv.Number}
Montant : {inv.TotalTtc:0.00} €

Cordialement
Cabinet comptable";

        await _emailSender.SendAsync(
            inv.Dossier.Email!,
            subject,
            body,
            new List<(byte[] Content, string FileName, string ContentType)>
            {
                (pdfBytes, fileName, "application/pdf")
            });

        inv.EmailSent = true;
        inv.EmailSentAt = DateTime.UtcNow;

        if (inv.RemainingAmount == 0 && inv.TotalTtc > 0)
            inv.Status = InvoiceStatus.Payee;
        else
            inv.Status = InvoiceStatus.Envoyee;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> SendAllInvoiceEmails(int year, int month)
    {
        var invoices = await _db.Invoices
            .Include(x => x.Dossier)
            .Where(x => x.Year == year && x.Month == month)
            .ToListAsync();

        int count = 0;

        foreach (var inv in invoices)
        {
            if (inv.Dossier == null || string.IsNullOrWhiteSpace(inv.Dossier.Email))
                continue;

            var ok = await SendInvoiceEmail(inv.Id);
            if (ok) count++;
        }

        return count;
    }

    // =========================================================
    // ANNUAL TABLE
    // =========================================================

    public async Task<List<AnnualClientBillingRowDto>> GetAnnualClientTable(int year, string? q)
    {
        var invoices = await _db.Invoices
            .Include(i => i.Dossier)
            .Where(i => i.Year == year)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim().ToLowerInvariant();
            invoices = invoices
                .Where(i =>
                    (i.Dossier?.CompanyName ?? "").ToLower().Contains(q) ||
                    (i.Dossier?.Code ?? "").ToLower().Contains(q) ||
                    (i.Dossier?.Email ?? "").ToLower().Contains(q))
                .ToList();
        }

        var rows = invoices
            .GroupBy(i => new
            {
                i.DossierId,
                Client = i.Dossier!.CompanyName,
                Email = i.Dossier!.Email
            })
            .Select(g =>
            {
                var row = new AnnualClientBillingRowDto
                {
                    DossierId = g.Key.DossierId,
                    Client = g.Key.Client ?? "",
                    ClientEmail = g.Key.Email ?? ""
                };

                for (int m = 1; m <= 12; m++)
                {
                    var inv = g.Where(x => x.Month == m)
                               .OrderByDescending(x => x.IssueDate)
                               .FirstOrDefault();

                    row.Months[m] = new AnnualMonthCellDto
                    {
                        Facture = inv?.TotalTtc ?? 0m,
                        Paye = inv?.PaidAmount ?? 0m,
                        PdfUrl = inv?.PdfUrl,
                        InvoiceId = inv?.Id
                    };
                }

                row.TotalFacture = row.Months.Values.Sum(x => x.Facture);
                row.TotalPaye = row.Months.Values.Sum(x => x.Paye);
                row.Ecart = row.TotalFacture - row.TotalPaye;

                return row;
            })
            .OrderBy(x => x.Client)
            .ToList();

        return rows;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool> Delete(int id)
    {
        var inv = await _db.Invoices.FindAsync(id);
        if (inv == null) return false;

        _db.Invoices.Remove(inv);
        await _db.SaveChangesAsync();
        return true;
    }

    // =========================================================
    // PDF
    // =========================================================

    public async Task<byte[]?> GeneratePdf(int id)
    {
        var inv = await _db.Invoices
            .Include(x => x.Dossier)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

        var settings = await _db.BillingSettings.FirstOrDefaultAsync();
        if (inv == null || settings == null) return null;

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(settings.LogoUrl))
        {
            try
            {
                var clean = settings.LogoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var physical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", clean);
                if (File.Exists(physical))
                    logoBytes = await File.ReadAllBytesAsync(physical);
            }
            catch
            {
                logoBytes = null;
            }
        }

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            if (logoBytes != null)
                                left.Item().Height(60).Image(logoBytes);
                            else
                                left.Item().Text(settings.CompanyName).Bold().FontSize(20);
                        });

                        row.ConstantItem(220).Border(1).Padding(6).Column(box =>
                        {
                            box.Item().AlignCenter().Text("Facture").Bold();
                            box.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Facture n° :").SemiBold();
                                r.RelativeItem().AlignRight().Text(inv.Number);
                            });
                            box.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Date :").SemiBold();
                                r.RelativeItem().AlignRight().Text(inv.IssueDate.ToString("dd/MM/yyyy"));
                            });
                        });
                    });
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(12).Column(c =>
                        {
                            c.Item().Text("PRESTATAIRE :").Italic().Bold();
                            c.Item().PaddingTop(4).Text(settings.CompanyName).SemiBold();
                            c.Item().Text(settings.Address);
                            c.Item().Text($"{settings.PostalCode} {settings.City}");
                            if (!string.IsNullOrWhiteSpace(settings.Phone)) c.Item().Text(settings.Phone);
                            if (!string.IsNullOrWhiteSpace(settings.Email)) c.Item().Text(settings.Email);
                        });

                        row.RelativeItem().PaddingLeft(12).Column(c =>
                        {
                            c.Item().Text("CLIENT :").Italic().Bold();
                            c.Item().PaddingTop(4).Text(inv.Dossier!.CompanyName).SemiBold();
                            if (!string.IsNullOrWhiteSpace(inv.Dossier.Address)) c.Item().Text(inv.Dossier.Address);
                            c.Item().Text($"{inv.Dossier.PostalCode} {inv.Dossier.City}");
                            if (!string.IsNullOrWhiteSpace(inv.Dossier.Email)) c.Item().Text(inv.Dossier.Email);
                        });
                    });

                    col.Item().PaddingTop(18).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(7);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            string[] headers = ["Description", "Prix unitaire", "Quantité", "Prix HT", "% TVA", "TVA", "Prix TTC"];
                            foreach (var item in headers)
                                h.Cell().BorderBottom(1).Padding(4).Text(item).SemiBold();
                        });

                        foreach (var l in inv.Lines)
                        {
                            table.Cell().Padding(4).Text(l.Label);
                            table.Cell().Padding(4).AlignRight().Text($"{l.UnitPriceHt:0.00} €");
                            table.Cell().Padding(4).AlignRight().Text($"{l.Quantity:0.####}");
                            table.Cell().Padding(4).AlignRight().Text($"{l.LineHt:0.00} €");
                            table.Cell().Padding(4).AlignRight().Text($"{l.VatRate:0.##}%");
                            table.Cell().Padding(4).AlignRight().Text($"{l.LineTva:0.00} €");
                            table.Cell().Padding(4).AlignRight().Text($"{l.LineTtc:0.00} €");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Width(230).Border(1).Padding(6).Column(tot =>
                    {
                        tot.Item().Row(r => { r.RelativeItem().Text("Total HT :").SemiBold(); r.RelativeItem().AlignRight().Text($"{inv.TotalHt:0.00} €"); });
                        tot.Item().Row(r => { r.RelativeItem().Text("Total TVA :").SemiBold(); r.RelativeItem().AlignRight().Text($"{inv.TotalTva:0.00} €"); });
                        tot.Item().Row(r => { r.RelativeItem().Text("TOTAL TTC :").Bold(); r.RelativeItem().AlignRight().Text($"{inv.TotalTtc:0.00} €").Bold(); });
                        tot.Item().Row(r => { r.RelativeItem().Text("Total payé :"); r.RelativeItem().AlignRight().Text($"{inv.PaidAmount:0.00} €"); });
                        tot.Item().Row(r => { r.RelativeItem().Text("Écart :").Bold(); r.RelativeItem().AlignRight().Text($"{inv.RemainingAmount:0.00} €").Bold(); });
                    });

                    col.Item().PaddingTop(18).Column(bank =>
                    {
                        bank.Item().Text("Règlement à adresser à").Italic();
                        bank.Item().PaddingTop(4).Text($"Titulaire du compte : {settings.AccountHolder}");
                        bank.Item().Text($"Banque : {settings.BankName}");
                        bank.Item().Text($"Agence : {settings.Agency}");
                        bank.Item().Text($"IBAN : {settings.Iban}");
                        bank.Item().Text($"BIC/SWIFT : {settings.Bic}");
                    });
                });

                page.Footer().AlignCenter().Text("NOUS VOUS REMERCIONS DE VOTRE CONFIANCE.").SemiBold();
            });
        });

        return doc.GeneratePdf();
    }

private async Task SaveGeneratedPdfToDisk(int invoiceId, int year, int month, string invoiceNumber)
{
    var pdfBytes = await GeneratePdf(invoiceId);
    if (pdfBytes == null || pdfBytes.Length == 0)
        return;

    var folder = Path.Combine(
        Directory.GetCurrentDirectory(),
        "wwwroot",
        "uploads",
        "invoices",
        year.ToString(),
        month.ToString("00"));

    Directory.CreateDirectory(folder);

    var safeNumber = SanitizeFileName(invoiceNumber);
    var uniqueFileName = $"{safeNumber}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.pdf";
    var fullPath = Path.Combine(folder, uniqueFileName);

    await File.WriteAllBytesAsync(fullPath, pdfBytes);

    var inv = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId);
    if (inv != null)
    {
        inv.PdfUrl = $"/uploads/invoices/{year}/{month:00}/{uniqueFileName}";
        await _db.SaveChangesAsync();
    }
}
// =========================================================
// INTERNAL HELPERS
// =========================================================

private async Task<Dossier> GetOrCreateDossierFromSheet(
        int year,
        string clientName,
        string clientAddress,
        string clientPostalCity)
    {
        var dossier = await _db.Dossiers
            .FirstOrDefaultAsync(d => d.Year == year && d.CompanyName.ToLower() == clientName.ToLower());

        if (dossier != null)
        {
            if (string.IsNullOrWhiteSpace(dossier.Address) && !string.IsNullOrWhiteSpace(clientAddress))
                dossier.Address = clientAddress;

            if (string.IsNullOrWhiteSpace(dossier.City) && !string.IsNullOrWhiteSpace(clientPostalCity))
                dossier.City = clientPostalCity;

            await _db.SaveChangesAsync();
            return dossier;
        }

        dossier = new Dossier
        {
            Year = year,
            Code = $"CL-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CompanyName = clientName,
            Sector = "",
            ResponsibleName = "",
            Phone = "",
            Email = "",
            Siret = "",
            VatNumber = "",
            Address = clientAddress,
            City = clientPostalCity,
            PostalCode = "",
            Country = "",
            Notes = "",
            LettreMissionEnvoyee = false,
            MandatEnvoye = false
        };

        _db.Dossiers.Add(dossier);
        await _db.SaveChangesAsync();

        return dossier;
    }

    private static string ReadText(IXLCell cell)
        => cell?.GetFormattedString()?.Trim() ?? "";

    private static decimal ReadDecimal(IXLCell cell)
    {
        if (cell == null || cell.IsEmpty())
            return 0m;

        var raw = cell.GetFormattedString()?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(raw))
        {
            raw = raw
                .Replace("€", "")
                .Replace("%", "")
                .Replace("\u00A0", "")
                .Replace(" ", "")
                .Replace(",", ".");

            if (decimal.TryParse(
                raw,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed))
            {
                return parsed;
            }
        }

        try
        {
            var dbl = cell.GetDouble();
            return Convert.ToDecimal(dbl);
        }
        catch
        {
            return 0m;
        }
    }

    private static DateTime? ReadDate(IXLCell cell)
    {
        if (cell == null || cell.IsEmpty())
            return null;

        try
        {
            return cell.GetDateTime();
        }
        catch
        {
            var raw = cell.GetFormattedString()?.Trim() ?? "";
            if (DateTime.TryParse(raw, out var dt))
                return dt;

            return null;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return value;
    }

    private async Task<BillingSettings> EnsureSettings()
    {
        var s = await _db.BillingSettings.FirstOrDefaultAsync();
        if (s != null) return s;

        s = new BillingSettings
        {
            CompanyName = "NOVAX - Cabinet",
            Address = "Adresse ...",
            PostalCode = "0000",
            City = "Ville",
            Phone = "+216 00 000 000",
            Email = "contact@novax.tn",
            Siret = "00000000000000",
            VatNumber = "TVA000000",
            AccountHolder = "NOVAX",
            BankName = "Banque ...",
            Agency = "Agence ...",
            Iban = "TN00 0000 0000 0000 0000 0000",
            Bic = "BIC00000",
            NumberingFormat = "{YYYY}-{SEQ}-C",
            AnnualCounter = 1,
            Suffix = "-C"
        };

        _db.BillingSettings.Add(s);
        await _db.SaveChangesAsync();
        return s;
    }

    private async Task<string> SaveFile(IFormFile file, string fileNameWithoutExt, string[] allowedExts)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext) || !allowedExts.Contains(ext))
            throw new InvalidOperationException("Type de fichier non supporté.");

        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploads);

        var full = Path.Combine(uploads, $"{fileNameWithoutExt}{ext}");

        using var stream = File.Create(full);
        await file.CopyToAsync(stream);

        return $"/uploads/{fileNameWithoutExt}{ext}";
    }

    private static BillingSettingsDto ToSettingsDto(BillingSettings s) => new()
    {
        Id = s.Id,
        CompanyName = s.CompanyName,
        Address = s.Address,
        PostalCode = s.PostalCode,
        City = s.City,
        Phone = s.Phone,
        Email = s.Email,
        Siret = s.Siret,
        VatNumber = s.VatNumber,
        LogoUrl = s.LogoUrl,
        AccountHolder = s.AccountHolder,
        BankName = s.BankName,
        Agency = s.Agency,
        Iban = s.Iban,
        Bic = s.Bic,
        NumberingFormat = s.NumberingFormat,
        AnnualCounter = s.AnnualCounter,
        Suffix = s.Suffix,
        TemplateUrl = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "billing-template.pdf"))
            ? "/uploads/billing-template.pdf"
            : null
    };

    private static InvoiceDto ToDto(Invoice i) => new()
    {
        Id = i.Id,
        Year = i.Year,
        Month = i.Month,
        DossierId = i.DossierId,
        DossierCode = i.Dossier?.Code ?? "",
        CompanyName = i.Dossier?.CompanyName ?? "",
        ClientEmail = i.Dossier?.Email ?? "",
        Number = i.Number,
        IssueDate = i.IssueDate,
        DueDate = i.DueDate,
        Status = i.Status,
        TotalHt = i.TotalHt,
        TotalTva = i.TotalTva,
        TotalTtc = i.TotalTtc,
        PaidAmount = i.PaidAmount,
        RemainingAmount = i.RemainingAmount,
        PaymentType = i.PaymentType,
        PdfUrl = i.PdfUrl,
        EmailSent = i.EmailSent,
        EmailSentAt = i.EmailSentAt,
        Lines = i.Lines.Select(l => new InvoiceLineDto
        {
            Id = l.Id,
            Label = l.Label,
            Quantity = l.Quantity,
            UnitPriceHt = l.UnitPriceHt,
            VatRate = l.VatRate,
            LineHt = l.LineHt,
            LineTva = l.LineTva,
            LineTtc = l.LineTtc
        }).ToList()
    };

    private static void Recalculate(Invoice inv)
    {
        foreach (var l in inv.Lines)
        {
            if (l.LineHt <= 0) l.LineHt = l.Quantity * l.UnitPriceHt;
            if (l.LineTva <= 0) l.LineTva = l.LineHt * (l.VatRate / 100m);
            if (l.LineTtc <= 0) l.LineTtc = l.LineHt + l.LineTva;
        }

        inv.TotalHt = inv.Lines.Sum(x => x.LineHt);
        inv.TotalTva = inv.Lines.Sum(x => x.LineTva);
        inv.TotalTtc = inv.Lines.Sum(x => x.LineTtc);
        inv.RemainingAmount = Math.Max(0, inv.TotalTtc - inv.PaidAmount);

        if (inv.RemainingAmount == 0 && inv.TotalTtc > 0)
            inv.Status = InvoiceStatus.Payee;
        else if (inv.EmailSent)
            inv.Status = InvoiceStatus.Envoyee;
        else
            inv.Status = InvoiceStatus.Brouillon;
    }

    private static string BuildNumber(BillingSettings settings, int year)
    {
        var seq = settings.AnnualCounter.ToString().PadLeft(3, '0');

        var number = (settings.NumberingFormat ?? "{YYYY}-{SEQ}-C")
            .Replace("{YYYY}", year.ToString())
            .Replace("{SEQ}", seq);

        if (!string.IsNullOrWhiteSpace(settings.Suffix) && !number.EndsWith(settings.Suffix))
            number += settings.Suffix;

        return number;
    }
}