using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Leaves;

public class LeaveDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = "";
    public LeaveStatus Status { get; set; }
    public string? AdminComment { get; set; }
}
