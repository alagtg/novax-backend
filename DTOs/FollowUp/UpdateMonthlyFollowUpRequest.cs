namespace YourProject.API.DTOs.FollowUp;

public class UpdateMonthlyFollowUpRequest
{
    // Dossier-level
    public bool? LettreMissionEnvoyee { get; set; }
    public bool? MandatEnvoye { get; set; }

    // Month-level
    public bool? FactureCreee { get; set; }
    public bool? FacturePayee { get; set; }

    public string? Notes { get; set; }
}
