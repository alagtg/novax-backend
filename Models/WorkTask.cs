using YourProject.API.Models.Enums;

namespace YourProject.API.Models;

public class WorkTask : BaseEntity
{
    public int Year { get; set; } = DateTime.UtcNow.Year;

    public int DossierId { get; set; }
    public Dossier? Dossier { get; set; }

    // Optionnel: assignation directe à un employé (pour filtres rapides)
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public ModuleType Module { get; set; }
    public string Title { get; set; } = "";     // Ex: "TVA 1er acompte"
    public string Description { get; set; } = ""; // Ex: "Déclaration + paiement"
    public DateTime? DueDate { get; set; }

    public WorkStatus Status { get; set; } = WorkStatus.EnCours;

    // Montants optionnels (acomptes, TVA, IS...)
    public decimal? Amount { get; set; }
    public string? Reference { get; set; } // ex: N° reçu, référence...
}
