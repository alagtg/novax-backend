using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.DTOs.Leaves;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[Authorize]
public class LeavesController : BaseApiController
{
    private readonly LeaveService _leaves;

    public LeavesController(LeaveService leaves) => _leaves = leaves;

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        return Ok(await _leaves.GetMine(CurrentUserId.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequest req)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        return Ok(await _leaves.Create(CurrentUserId.Value, req));
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> All() => Ok(await _leaves.GetAll());

    [HttpPut("{id}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeaveStatusRequest req)
    {
        var item = await _leaves.UpdateStatus(id, req);
        return item == null ? NotFound() : Ok(item);
    }
}
