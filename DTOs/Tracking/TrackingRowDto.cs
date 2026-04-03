using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Tracking;

public class TrackingRowDto
{
    public int Id { get; set; }
    public int Year { get; set; }

    public int DossierId { get; set; }
    public string DossierCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Sector { get; set; } = "";

    public int AssignedToUserId { get; set; }
    public string AssignedToName { get; set; } = "";

    public ModuleType Module { get; set; }

    public string Board { get; set; } = "Default";

    // Données flexibles (JSON) : peut contenir des sous-objets (ex: months)
    public Dictionary<string, object?> Data { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}
