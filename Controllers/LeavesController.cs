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

        try
        {
            return Ok(await _leaves.GetMine(CurrentUserId.Value));
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                detail = ex.GetBaseException().Message
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequest req)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        try
        {
            var res = await _leaves.Create(CurrentUserId.Value, req);
            return Ok(res);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                detail = ex.GetBaseException().Message
            });
        }
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> All()
    {
        try
        {
            return Ok(await _leaves.GetAll());
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                detail = ex.GetBaseException().Message
            });
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeaveStatusRequest req)
    {
        try
        {
            var item = await _leaves.UpdateStatus(id, req);
            return item == null ? NotFound() : Ok(item);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                detail = ex.GetBaseException().Message
            });
        }
    }
}