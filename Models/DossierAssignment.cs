using YourProject.API.Models.Enums;

namespace YourProject.API.Models;

public class DossierAssignment : BaseEntity
{
    public int DossierId { get; set; }
    public Dossier? Dossier { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public ModuleType Module { get; set; }
}
