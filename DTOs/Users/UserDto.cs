using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool CanAccessAllSocialDossiers { get; set; }
}
