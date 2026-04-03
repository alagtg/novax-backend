using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.DTOs.Tasks;
using YourProject.API.Models;
using YourProject.API.Models.Enums;

namespace YourProject.API.Services;

public class TaskService
{
    private readonly AppDbContext _db;
    public TaskService(AppDbContext db) => _db = db;

    public async Task<List<TaskDto>> Search(int year, ModuleType? module, int? dossierId, int? assignedToUserId, WorkStatus? status)
    {
        var q = _db.WorkTasks
            .Include(t => t.Dossier)
            .Include(t => t.AssignedToUser)
            .AsQueryable();

        q = q.Where(t => t.Year == year);

        if (module.HasValue) q = q.Where(t => t.Module == module.Value);
        if (dossierId.HasValue) q = q.Where(t => t.DossierId == dossierId.Value);
        if (assignedToUserId.HasValue) q = q.Where(t => t.AssignedToUserId == assignedToUserId.Value);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);

        return await q
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.Title)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Year = t.Year,
                DossierId = t.DossierId,
                DossierCode = t.Dossier != null ? t.Dossier.Code : "",
                CompanyName = t.Dossier != null ? t.Dossier.CompanyName : "",
                AssignedToUserId = t.AssignedToUserId,
                AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.FullName : null,
                Module = t.Module,
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate,
                Status = t.Status,
                Amount = t.Amount,
                Reference = t.Reference
            })
            .ToListAsync();
    }

    public async Task<TaskDto?> GetById(int id)
    {
        var t = await _db.WorkTasks
            .Include(x => x.Dossier)
            .Include(x => x.AssignedToUser)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (t == null) return null;

        return new TaskDto
        {
            Id = t.Id,
            Year = t.Year,
            DossierId = t.DossierId,
            DossierCode = t.Dossier?.Code ?? "",
            CompanyName = t.Dossier?.CompanyName ?? "",
            AssignedToUserId = t.AssignedToUserId,
            AssignedToName = t.AssignedToUser?.FullName,
            Module = t.Module,
            Title = t.Title,
            Description = t.Description,
            DueDate = t.DueDate,
            Status = t.Status,
            Amount = t.Amount,
            Reference = t.Reference
        };
    }

    public async Task<TaskDto> Create(SaveTaskRequest req)
    {
        var t = new WorkTask
        {
            Year = req.Year,
            DossierId = req.DossierId,
            AssignedToUserId = req.AssignedToUserId,
            Module = req.Module,
            Title = req.Title,
            Description = req.Description,
            DueDate = req.DueDate,
            Status = req.Status,
            Amount = req.Amount,
            Reference = req.Reference
        };

        _db.WorkTasks.Add(t);
        await _db.SaveChangesAsync();

        return (await GetById(t.Id))!;
    }

    public async Task<TaskDto?> Update(int id, SaveTaskRequest req)
    {
        var t = await _db.WorkTasks.FindAsync(id);
        if (t == null) return null;

        t.Year = req.Year;
        t.DossierId = req.DossierId;
        t.AssignedToUserId = req.AssignedToUserId;
        t.Module = req.Module;
        t.Title = req.Title;
        t.Description = req.Description;
        t.DueDate = req.DueDate;
        t.Status = req.Status;
        t.Amount = req.Amount;
        t.Reference = req.Reference;

        await _db.SaveChangesAsync();
        return await GetById(id);
    }

    public async Task<bool> Delete(int id)
    {
        var t = await _db.WorkTasks.FindAsync(id);
        if (t == null) return false;

        _db.WorkTasks.Remove(t);
        await _db.SaveChangesAsync();
        return true;
    }
}
