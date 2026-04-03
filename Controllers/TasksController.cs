using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.DTOs.Tasks;
using YourProject.API.Models.Enums;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[Authorize]
public class TasksController : BaseApiController
{
    private readonly TaskService _tasks;

    public TasksController(TaskService tasks) => _tasks = tasks;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int year,
        [FromQuery] ModuleType? module,
        [FromQuery] int? dossierId,
        [FromQuery] int? assignedToUserId,
        [FromQuery] WorkStatus? status)
    {
        // Employé: restreindre à ses tâches (ou ses dossiers) si pas admin
        if (!User.IsInRole("ADMIN"))
            assignedToUserId = CurrentUserId;

        return Ok(await _tasks.Search(year, module, dossierId, assignedToUserId, status));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var t = await _tasks.GetById(id);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveTaskRequest req)
    {
        if (!User.IsInRole("ADMIN"))
            req.AssignedToUserId ??= CurrentUserId;

        return Ok(await _tasks.Create(req));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveTaskRequest req)
    {
        if (!User.IsInRole("ADMIN"))
            req.AssignedToUserId ??= CurrentUserId;

        var t = await _tasks.Update(id, req);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Admin ou employé (si tu veux admin seulement, garde: [Authorize(Roles="ADMIN")])
        var ok = await _tasks.Delete(id);
        return ok ? Ok(new { message = "Supprimé." }) : NotFound();
    }
}
