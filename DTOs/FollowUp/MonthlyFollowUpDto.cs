namespace YourProject.API.DTOs.FollowUp;

public class MonthlyFollowUpDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public int DossierId { get; set; }
    public string DossierCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Sector { get; set; } = "";

    // Dossier-level fields
    public bool LettreMissionEnvoyee { get; set; }
    public bool MandatEnvoye { get; set; }

    // Month-level fields
    public bool FactureCreee { get; set; }
    public bool FacturePayee { get; set; }

    public string Notes { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}
