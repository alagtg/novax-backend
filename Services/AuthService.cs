using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;
using YourProject.API.DTOs.Auth;
using YourProject.API.Helpers;

namespace YourProject.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtTokenHelper _jwt;

    public AuthService(AppDbContext db, JwtTokenHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResponse?> Login(LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null || !user.IsActive) return null;

        var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!ok) return null;

        var token = _jwt.CreateToken(user);
        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<bool> ChangePassword(int userId, string oldPwd, string newPwd)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(oldPwd, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPwd);
        await _db.SaveChangesAsync();
        return true;
    }
}
