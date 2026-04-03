using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.DTOs.Leaves;
using YourProject.API.Models;
using YourProject.API.Models.Enums;

namespace YourProject.API.Services;

public class LeaveService
{
    private readonly AppDbContext _db;
    public LeaveService(AppDbContext db) => _db = db;

    public async Task<List<LeaveDto>> GetMine(int userId)
    {
        return await _db.LeaveRequests
            .Include(l => l.User)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => ToDto(l))
            .ToListAsync();
    }

    public async Task<List<LeaveDto>> GetAll()
    {
        return await _db.LeaveRequests
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => ToDto(l))
            .ToListAsync();
    }

    public async Task<LeaveDto> Create(int userId, CreateLeaveRequest req)
    {
        var lr = new LeaveRequest
        {
            UserId = userId,
            StartDate = req.StartDate.Date,
            EndDate = req.EndDate.Date,
            Reason = req.Reason,
            Status = LeaveStatus.EnAttente
        };

        _db.LeaveRequests.Add(lr);
        await _db.SaveChangesAsync();

        lr = await _db.LeaveRequests.Include(x => x.User).FirstAsync(x => x.Id == lr.Id);
        return ToDto(lr);
    }

    public async Task<LeaveDto?> UpdateStatus(int id, UpdateLeaveStatusRequest req)
    {
        var lr = await _db.LeaveRequests.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (lr == null) return null;

        lr.Status = req.Status;
        lr.AdminComment = req.AdminComment;

        await _db.SaveChangesAsync();
        return ToDto(lr);
    }

    private static LeaveDto ToDto(LeaveRequest l) => new()
    {
        Id = l.Id,
        UserId = l.UserId,
        UserName = l.User?.FullName ?? "",
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        Reason = l.Reason,
        Status = l.Status,
        AdminComment = l.AdminComment
    };
}
