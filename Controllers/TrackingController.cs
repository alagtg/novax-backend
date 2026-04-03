using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.DTOs.Common;
using YourProject.API.Models.Enums;
using YourProject.API.Services;
using YourProject.API.DTOs.Tracking;

namespace YourProject.API.Controllers;

[Authorize]
public class TrackingController : BaseApiController
{
    private readonly TrackingService _tracking;

    public TrackingController(TrackingService tracking)
    {
        _tracking = tracking;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int year,
        [FromQuery] string module,
        [FromQuery] string? board,
        [FromQuery] int? userId,
        [FromQuery] string? q,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        if (year <= 0) return BadRequest("year is required");
        if (!Enum.TryParse<ModuleType>(module, ignoreCase: true, out var m))
            return BadRequest("invalid module");

        var b = string.IsNullOrWhiteSpace(board) ? "Default" : board.Trim();
        var isAdmin = (CurrentRole ?? "").Equals("ADMIN", StringComparison.OrdinalIgnoreCase);

        int? scopedUserId = isAdmin ? userId : CurrentUserId;
        if (!isAdmin && !scopedUserId.HasValue) return Unauthorized();

        if (page.HasValue)
        {
            var pq = new PagedQuery
            {
                Page = page.Value,
                PageSize = pageSize ?? 20
            };

            var filters = Request.Query
                .Where(x => x.Key.StartsWith("f_", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    x => x.Key.Substring(2),
                    x => x.Value.ToString()
                );

            var res = await _tracking.GetPaged(
                year,
                m,
                b,
                scopedUserId,
                q,
                isAdmin,
                pq.SafePage,
                pq.SafePageSize(),
                filters
            );

            return Ok(res);
        }

        var rows = await _tracking.Get(year, m, b, scopedUserId, q, isAdmin);
        return Ok(rows);
    }
    [HttpPost("{id}/upload-juridique-file")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadJuridiqueFile(int id, [FromForm] IFormFile file)
    {
        if (!CurrentUserId.HasValue)
            return Unauthorized();

        if (file == null)
            return BadRequest("Le champ 'file' est null.");

        if (file.Length == 0)
            return BadRequest("Le fichier est vide.");

        var isAdmin = (CurrentRole ?? "").Equals("ADMIN", StringComparison.OrdinalIgnoreCase);

        try
        {
            var result = await _tracking.UploadJuridiqueFile(id, CurrentUserId.Value, isAdmin, file);

            if (result == null)
                return NotFound("Ligne introuvable ou accès refusé.");

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest("Erreur upload: " + ex.Message);
        }
    }

    [HttpGet("look")]
    public async Task<IActionResult> Look(
        [FromQuery] int year,
        [FromQuery] string module,
        [FromQuery] int? userId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (year <= 0) return BadRequest("year is required");
        if (!Enum.TryParse<ModuleType>(module, ignoreCase: true, out var m))
            return BadRequest("invalid module");

        var isAdmin = (CurrentRole ?? "").Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
        int? scopedUserId = isAdmin ? userId : CurrentUserId;
        if (!isAdmin && !scopedUserId.HasValue) return Unauthorized();

        var pq = new PagedQuery
        {
            Page = page,
            PageSize = pageSize
        };

        var res = await _tracking.LookPaged(year, m, scopedUserId, isAdmin, q, pq.SafePage, pq.SafePageSize());
        return Ok(res);
    }

    [HttpPost("ensure")]
    public async Task<IActionResult> Ensure([FromBody] EnsureTrackingRowRequest req)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        if (req.Year <= 0) return BadRequest("year is required");
        if (!Enum.TryParse<ModuleType>(req.Module, ignoreCase: true, out var m))
            return BadRequest("invalid module");

        var isAdmin = (CurrentRole ?? "").Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
        var board = string.IsNullOrWhiteSpace(req.Board) ? "Default" : req.Board.Trim();

        var dto = await _tracking.Ensure(
            req.Year,
            m,
            board,
            req.DossierId,
            CurrentUserId.Value,
            isAdmin,
            req.AssignedToUserId
        );

        if (dto == null) return Forbid();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveTrackingRowRequest req)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var isAdmin = (CurrentRole ?? "").Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
        var row = await _tracking.Update(id, CurrentUserId.Value, isAdmin, req);

        return row == null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTrackingRowRequest req)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        if (req.Year <= 0) return BadRequest("year is required");
        if (!Enum.TryParse<ModuleType>(req.Module, ignoreCase: true, out var m))
            return BadRequest("invalid module");

        var isAdmin = (CurrentRole ?? "").Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
        var board = string.IsNullOrWhiteSpace(req.Board) ? "Default" : req.Board.Trim();

        var dto = await _tracking.Create(
            req.Year,
            m,
            board,
            req.DossierId,
            CurrentUserId.Value,
            isAdmin,
            req.AssignedToUserId,
            req.Data
        );

        if (dto == null) return Forbid();
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var isAdmin = (CurrentRole ?? "").Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
        var ok = await _tracking.Delete(id, CurrentUserId.Value, isAdmin);

        return ok ? Ok() : NotFound();
    }
}