using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Users;

public class UpdateUserRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.EMPLOYE;
    public bool IsActive { get; set; } = true;
}
