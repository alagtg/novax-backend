using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Leaves;

public class UpdateLeaveStatusRequest
{
    public LeaveStatus Status { get; set; }
    public string? AdminComment { get; set; }
}
