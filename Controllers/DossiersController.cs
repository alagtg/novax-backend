using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.DTOs.Dossiers;
using YourProject.API.Models.Enums;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[Authorize]
public class DossiersController : BaseApiController
{
    private readonly DossierService _dossiers;

    public DossiersController(DossierService dossiers)
    {
        _dossiers = dossiers;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int year,
        [FromQuery] string? q,
        [FromQuery] int? employeeId,
        [FromQuery] ModuleType? module)
    {
        var isAdmin = User.IsInRole("ADMIN");
        if (!isAdmin)
        {
            employeeId = CurrentUserId;
        }

        var items = await _dossiers.Search(year, q, employeeId, module);
        if (!isAdmin)
        {
            foreach (var item in items)
            {
                item.LettreMissionEnvoyee = false;
                item.MandatEnvoye = false;
            }
        }

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var d = await _dossiers.GetById(id);
        if (d == null) return NotFound();

        if (!User.IsInRole("ADMIN"))
        {
            d.LettreMissionEnvoyee = false;
            d.MandatEnvoye = false;
        }

        return Ok(d);
    }

    // ================= CREATE =================
    [HttpPost]
    [Authorize(Roles = "ADMIN,EMPLOYE")]
    public async Task<IActionResult> Create([FromBody] SaveDossierRequest req)
    {
        try
        {
            var result = await _dossiers.Create(req);

            if (User.IsInRole("EMPLOYE") && !User.IsInRole("ADMIN"))
            {
                if (!CurrentUserId.HasValue)
                    return Unauthorized();

                await _dossiers.Assign(result.Id, CurrentUserId.Value, ModuleType.Juridique);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ================= UPDATE =================
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveDossierRequest req)
    {
        try
        {
            var d = await _dossiers.Update(id, req);
            return d == null ? NotFound() : Ok(d);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ================= DELETE =================
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(int id, [FromBody] DeleteDossierRequest req)
    {
        try
        {
            var ok = await _dossiers.Delete(id, req?.Code ?? "");
            return ok ? Ok(new { message = "Supprimé." }) : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ================= ASSIGN =================
    [HttpPost("{id}/assign")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignDossierRequest req)
    {
        try
        {
            var ok = await _dossiers.Assign(id, req.UserId, req.Module);
            return ok ? Ok(new { message = "Assigné." }) : BadRequest("Assignation impossible.");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ================= UNASSIGN =================
    [HttpPost("{id}/unassign")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Unassign(int id, [FromBody] AssignDossierRequest req)
    {
        try
        {
            var ok = await _dossiers.Unassign(id, req.UserId, req.Module);
            return ok ? Ok(new { message = "Retiré." }) : BadRequest("Suppression assignation impossible.");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ================= CLONE =================
    [HttpPost("{id}/clone")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Clone(int id, [FromQuery] int year)
    {
        try
        {
            var newId = await _dossiers.CloneToYear(id, year);
            return newId.HasValue ? Ok(new { newId }) : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}