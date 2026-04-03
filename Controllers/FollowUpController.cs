using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.DTOs.FollowUp;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowUpController : ControllerBase
{
    private readonly FollowUpService _svc;
    public FollowUpController(FollowUpService svc) => _svc = svc;

    // ADMIN + COMPTABLE_FACTURE (suivi global)
    [HttpGet]
    [Authorize(Roles = "ADMIN,COMPTABLE_FACTURE")]
    public async Task<IActionResult> Get([FromQuery] int year, [FromQuery] int month, [FromQuery] string? q)
    {
        var rows = await _svc.GetMonth(year, month, q);
        return Ok(rows);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN,COMPTABLE_FACTURE")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMonthlyFollowUpRequest req)
    {
        var row = await _svc.Update(id, req);
        if (row == null) return NotFound();
        return Ok(row);
    }
}
