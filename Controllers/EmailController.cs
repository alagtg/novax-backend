
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.Models;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;

    public EmailController(AppDbContext db, EmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    [HttpPost("send-to-all")]
    public async Task<IActionResult> SendToAll(
        [FromForm] string subject,
        [FromForm] string body,
        [FromForm] List<IFormFile>? files)
    {
        var emails = await _db.Dossiers
            .Where(d => d.Email != null && d.Email != "")
            .Select(d => d.Email!)
            .Distinct()
            .ToListAsync();

        await _emailService.SendEmailAsync(emails, subject, body, files);

        return Ok(new { message = $"{emails.Count} emails envoyés." });
    }

    [HttpPost("send-selected")]
    public async Task<IActionResult> SendSelected(
       [FromForm] string subject,
       [FromForm] string body,
       [FromForm] string emails,
       [FromForm] List<IFormFile>? files)
    {
        if (string.IsNullOrEmpty(emails))
            return BadRequest("Emails vide");

        var list = System.Text.Json.JsonSerializer
            .Deserialize<List<string>>(emails);

        if (list == null || !list.Any())
            return BadRequest("Aucun email sélectionné");

        await _emailService.SendEmailAsync(list, subject, body, files);

        return Ok(new { message = $"{list.Count} emails envoyés." });
    }
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
        => Ok(await _db.EmailTemplates.ToListAsync());
    [HttpGet("dossiers-emails")]
    public async Task<IActionResult> GetDossiersEmails()
    {
        var emails = await _db.Dossiers
            .Where(d => !string.IsNullOrEmpty(d.Email))
            .GroupBy(d => d.Email)
            .Select(g => new {
                email = g.Key,
                companyName = g.First().CompanyName
            })
            .ToListAsync();

        return Ok(emails);
    }
    [HttpPost("templates")]
    public async Task<IActionResult> SaveTemplate(EmailTemplate template)
    {
        _db.EmailTemplates.Add(template);
        await _db.SaveChangesAsync();
        return Ok(template);
    }
}