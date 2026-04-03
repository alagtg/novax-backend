using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Tasks;

public class SaveTaskRequest
{
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public int DossierId { get; set; }
    public int? AssignedToUserId { get; set; }
    public ModuleType Module { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public WorkStatus Status { get; set; } = WorkStatus.EnCours;
    public decimal? Amount { get; set; }
    public string? Reference { get; set; }
}
