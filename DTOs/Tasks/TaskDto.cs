using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Tasks;

public class TaskDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int DossierId { get; set; }
    public string DossierCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public int? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public ModuleType Module { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public WorkStatus Status { get; set; }
    public decimal? Amount { get; set; }
    public string? Reference { get; set; }
}
