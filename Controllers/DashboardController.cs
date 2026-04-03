using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[Authorize]
public class DashboardController : BaseApiController
{
    private readonly DashboardService _dash;

    public DashboardController(DashboardService dash) => _dash = dash;

    // ✅ GLOBAL / PAR EMPLOYÉE / PAR CLIENT
    // Admin: peut passer employeeId et dossierId
    // Employé: employeeId forcé = CurrentUserId, dossierId ignoré (sécurité)
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromQuery] int year,
        [FromQuery] int? employeeId,
        [FromQuery] int? dossierId,
        [FromQuery] bool includeLines = false // optionnel
    )
    {
        if (!User.IsInRole("ADMIN"))
        {
            employeeId = CurrentUserId;
            dossierId = null; // ✅ sécurité
        }

        var dto = await _dash.GetSummary(year, employeeId, dossierId, includeLines);
        return Ok(dto);
    }

    // ✅ Optionnel: endpoint séparé si tu veux charger les lignes uniquement quand l’utilisateur clique "Voir détails"
    [HttpGet("lines")]
    public async Task<IActionResult> Lines(
        [FromQuery] int year,
        [FromQuery] int? employeeId,
        [FromQuery] int? dossierId
    )
    {
        if (!User.IsInRole("ADMIN"))
        {
            employeeId = CurrentUserId;
            dossierId = null;
        }

        var lines = await _dash.GetLines(year, employeeId, dossierId);
        return Ok(lines);
    }
    [HttpGet("bi")]
    public async Task<IActionResult> Bi(
    [FromQuery] int year,
    [FromQuery] int? employeeId,
    [FromQuery] int? dossierId,
    [FromQuery] string periodeType = "Mensuel",   // Mensuel | Trimestriel | Annuel
    [FromQuery] string? periodeValue = null       // 2 | T1 | ANNUEL
)
    {
        if (!User.IsInRole("ADMIN"))
        {
            employeeId = CurrentUserId;
            dossierId = null;
        }

        var dto = await _dash.GetBi(year, employeeId, dossierId, periodeType, periodeValue);
        return Ok(dto);
    }
}