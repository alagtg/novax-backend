using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Dossiers;

public class AssignDossierRequest
{
    public int UserId { get; set; }
    public ModuleType Module { get; set; }
}
