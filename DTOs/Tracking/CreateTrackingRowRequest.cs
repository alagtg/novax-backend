namespace YourProject.API.DTOs.Tracking;

public class CreateTrackingRowRequest
{
    public int Year { get; set; }
    public string Module { get; set; } = "";
    public string Board { get; set; } = "";
    public int DossierId { get; set; }
    public int? AssignedToUserId { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
}