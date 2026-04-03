namespace YourProject.API.DTOs.Tracking;

public class DossierLookDto
{
    public int DossierId { get; set; }
    public string Code { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Sector { get; set; } = "";
    public int AssignedToUserId { get; set; }
    public string AssignedToName { get; set; } = "";
}
