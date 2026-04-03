using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FiscalYearController : ControllerBase
{
    private readonly FiscalYearService _service;

    public FiscalYearController(FiscalYearService service)
    {
        _service = service;
    }

    [HttpGet("isclosed")]
    public async Task<IActionResult> IsClosed([FromQuery] int year)
    {
        return Ok(await _service.IsClosed(year));
    }
    [HttpPost("reopen")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Reopen([FromQuery] int year)
    {
        await _service.ReopenYear(year);
        return Ok(new { message = "Année déclôturée." });
    }
    [HttpPost("close")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Close([FromQuery] int year)
    {
        await _service.CloseYear(year);
        return Ok(new { message = "Année clôturée." });
    }
}