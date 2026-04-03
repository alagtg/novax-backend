using YourProject.API.Models.Enums;

namespace YourProject.API.Models;

public class TrackingRow : BaseEntity
{
    public int Year { get; set; }

    // "Board" = sous-interface dans un module (ex: Social: EtatDossier/ReguleDSN/...)
    // Permet d'avoir 5 interfaces Social + 5 interfaces Comptabilité.
    public string Board { get; set; } = "Default";

    public int DossierId { get; set; }
    public Dossier? Dossier { get; set; }

    public int AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public ModuleType Module { get; set; }

    // JSON object (key/value) storing module-specific columns
    public string DataJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
