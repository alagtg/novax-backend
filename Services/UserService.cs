using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.DTOs.Users;
using YourProject.API.Models;

namespace YourProject.API.Services;

public class UserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;

    public async Task<List<UserDto>> GetAll()
    {
        return await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CanAccessAllSocialDossiers = u.CanAccessAllSocialDossiers
            })
            .ToListAsync();
    }

    public async Task<UserDto?> GetById(int id)
    {
        return await _db.Users
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CanAccessAllSocialDossiers = u.CanAccessAllSocialDossiers
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto> Create(CreateUserRequest req)
    {
        var user = new User
        {
            FullName = req.FullName,
            Email = req.Email.Trim().ToLowerInvariant(),
            Role = req.Role,
            IsActive = req.IsActive,
            CanAccessAllSocialDossiers = req.CanAccessAllSocialDossiers,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CanAccessAllSocialDossiers = user.CanAccessAllSocialDossiers
        };
    }

    public async Task<UserDto?> Update(int id, UpdateUserRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        user.FullName = req.FullName;
        user.Email = req.Email.Trim().ToLowerInvariant();
        user.Role = req.Role;
        user.IsActive = req.IsActive;
        user.CanAccessAllSocialDossiers = req.CanAccessAllSocialDossiers;

        await _db.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CanAccessAllSocialDossiers = user.CanAccessAllSocialDossiers
        };
    }

    public async Task<bool> ChangePassword(int id, string newPassword)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        return true;
    }
    public async Task<bool> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }
}
