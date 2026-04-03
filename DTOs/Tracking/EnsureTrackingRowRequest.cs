namespace YourProject.API.DTOs.Tracking;

public class EnsureTrackingRowRequest
{
    public int Year { get; set; }
    public string Module { get; set; } = "";
    public string Board { get; set; } = "Default";

    public int DossierId { get; set; }

    // Admin uniquement : peut forcer la collaboratrice
    public int? AssignedToUserId { get; set; }
}
