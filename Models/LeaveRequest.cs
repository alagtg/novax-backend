using YourProject.API.Models.Enums;

namespace YourProject.API.Models;

public class LeaveRequest : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = "";

    public LeaveStatus Status { get; set; } = LeaveStatus.EnAttente;
    public string? AdminComment { get; set; }
}
