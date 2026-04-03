namespace YourProject.API.Models;

/// <summary>
/// Tableau de suivi global par mois (année + mois) et par dossier.
/// Sert à suivre l'état "Facture créée" / "Facture payée" et des notes.
/// Les champs LettreMissionEnvoyee / MandatEnvoye sont stockés sur Dossier,
/// mais renvoyés dans les DTO pour simplifier l'UI.
/// </summary>
public class MonthlyFollowUp : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; } // 1..12

    public int DossierId { get; set; }
    public Dossier? Dossier { get; set; }

    public bool FactureCreee { get; set; } = false;
    public bool FacturePayee { get; set; } = false;

    public string Notes { get; set; } = "";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
