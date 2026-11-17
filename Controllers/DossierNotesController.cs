using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.Models;

namespace YourProject.API.Controllers;

[Authorize]
[Route("api/dossiers/{dossierId:int}/notebook")]
public class DossierNotesController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly FiscalYearService _fiscal;
    public DossierNotesController(AppDbContext db, FiscalYearService fiscal)
    { _db = db; _fiscal = fiscal; }

    private async Task<bool> CanAccess(int dossierId)
    {
        if (User.IsInRole("ADMIN") || User.IsInRole("COMPTABLE_FACTURE")) return true;
        var user = await _db.Users.FindAsync(CurrentUserId ?? 0);
        if (user == null || !user.IsActive) return false;
        return user.CanAccessAllSocialDossiers || await _db.DossierAssignments
            .AnyAsync(a => a.DossierId == dossierId && a.UserId == user.Id);
    }

    [HttpGet("/api/notebook/dossiers")]
    public async Task<IActionResult> Dossiers([FromQuery] int year)
    {
        var user = await _db.Users.FindAsync(CurrentUserId ?? 0);
        if (user == null || !user.IsActive) return Unauthorized();
        var query = _db.Dossiers.AsNoTracking().Where(d => d.Year == year);
        if (!User.IsInRole("ADMIN") && !User.IsInRole("COMPTABLE_FACTURE") && !user.CanAccessAllSocialDossiers)
            query = query.Where(d => d.Assignments.Any(a => a.UserId == user.Id));
        return Ok(await query.OrderBy(d => d.CompanyName).ThenBy(d => d.Code)
            .Select(d => new { d.Id, d.Code, d.CompanyName, d.Sector }).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> List(int dossierId)
    {
        if (!await CanAccess(dossierId)) return Forbid();
        var dossier = await _db.Dossiers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dossierId);
        if (dossier == null) return NotFound();
        var notes = await _db.Set<DossierNote>().AsNoTracking()
            .Where(n => n.DossierId == dossierId)
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Select(n => new { n.Id, n.Title, n.Details, n.AuthorName, n.CreatedAt, n.UpdatedAt,
                canManage = n.AuthorId == CurrentUserId || User.IsInRole("ADMIN") })
            .ToListAsync();
        return Ok(new { notes, legacyNotes = dossier.Notes, isClosed = await _fiscal.IsClosed(dossier.Year) });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,EMPLOYE,COMPTABLE_FACTURE")]
    public async Task<IActionResult> Add(int dossierId, [FromBody] AddNoteRequest request)
    {
        if (!await CanAccess(dossierId)) return Forbid();
        var dossier = await _db.Dossiers.FindAsync(dossierId);
        if (dossier == null) return NotFound();
        if (await _fiscal.IsClosed(dossier.Year))
            return BadRequest(new { message = "Année clôturée : le bloc-notes est en lecture seule." });
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Details))
            return BadRequest(new { message = "Le titre et les détails sont obligatoires." });
        var author = await _db.Users.FindAsync(CurrentUserId ?? 0);
        if (author == null || !author.IsActive) return Unauthorized();
        var note = new DossierNote {
            DossierId = dossierId, AuthorId = author.Id, AuthorName = author.FullName,
            Title = request.Title.Trim(), Details = request.Details.Trim(), CreatedAt = DateTime.UtcNow
        };
        _db.Set<DossierNote>().Add(note);
        await _db.SaveChangesAsync();
        return Ok(new { note.Id, note.Title, note.Details, note.AuthorName, note.CreatedAt, note.UpdatedAt, canManage = true });
    }

    [HttpPut("{noteId:int}")]
    public async Task<IActionResult> Edit(int dossierId, int noteId, [FromBody] AddNoteRequest request)
    {
        if (!await CanAccess(dossierId)) return Forbid();
        var note = await _db.Set<DossierNote>().Include(n => n.Dossier)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.DossierId == dossierId);
        if (note == null) return NotFound();
        if (note.AuthorId != CurrentUserId && !User.IsInRole("ADMIN")) return Forbid();
        if (await _fiscal.IsClosed(note.Dossier.Year))
            return BadRequest(new { message = "Année clôturée : modification impossible." });
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Details))
            return BadRequest(new { message = "Le titre et les détails sont obligatoires." });
        note.Title = request.Title.Trim();
        note.Details = request.Details.Trim();
        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { note.Id, note.Title, note.Details, note.AuthorName, note.CreatedAt, note.UpdatedAt, canManage = true });
    }

    [HttpDelete("{noteId:int}")]
    public async Task<IActionResult> Delete(int dossierId, int noteId)
    {
        if (!await CanAccess(dossierId)) return Forbid();
        var note = await _db.Set<DossierNote>().Include(n => n.Dossier)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.DossierId == dossierId);
        if (note == null) return NotFound();
        if (note.AuthorId != CurrentUserId && !User.IsInRole("ADMIN")) return Forbid();
        if (await _fiscal.IsClosed(note.Dossier.Year))
            return BadRequest(new { message = "Année clôturée : suppression impossible." });
        _db.Set<DossierNote>().Remove(note);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public class AddNoteRequest
    {
        [Required, MaxLength(160)] public string Title { get; set; } = "";
        [Required, MaxLength(5000)] public string Details { get; set; } = "";
    }
}
