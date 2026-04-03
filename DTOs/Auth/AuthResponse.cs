using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
}
