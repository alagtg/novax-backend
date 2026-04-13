using YourProject.API.Models.Enums;

namespace YourProject.API.Models;

public class User : BaseEntity
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.EMPLOYE;
    public bool IsActive { get; set; } = true;
    public bool CanAccessAllSocialDossiers { get; set; } = false;
    // Navigation
    public ICollection<DossierAssignment> Assignments { get; set; } = new List<DossierAssignment>();
}
