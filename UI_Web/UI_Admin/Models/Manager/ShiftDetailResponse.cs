namespace UI_Admin.Models.Manager;

public sealed class ShiftDetailResponse
{
    public int ShiftId { get; set; }
    public int ScheduleWeekId { get; set; }
    public int ShiftTemplateId { get; set; }
    public string ShiftName { get; set; } = "";
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ShiftEmployeeRequirementResponse> EmployeeRequirements { get; set; } = [];
    public List<ShiftStaffResponse> Staff { get; set; } = [];
    public int RequiredEmployees => EmployeeRequirements.Sum(x => x.RequiredCount);
    public int AssignedEmployees => Staff.Count(x => x.Status is "ASSIGNED" or "COMPLETED");
    public int RemainingSlots => Status == "CANCELLED" ? 0 : EmployeeRequirements.Sum(x => x.RemainingCount);
}

public sealed class ShiftStaffResponse
{
    public int AssignmentId { get; set; }
    public int? ShiftRequirementId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string AssignmentSource { get; set; } = "";
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = "";
}

