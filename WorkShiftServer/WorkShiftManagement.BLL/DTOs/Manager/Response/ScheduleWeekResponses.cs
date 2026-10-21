namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ScheduleWeekSummaryResponse
{
    public int ScheduleWeekId { get; set; }
    public int BranchId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? OpenedAt { get; set; }
    public DateTime? RegistrationClosedAt { get; set; }
    public int TotalShifts { get; set; }
    public int RequiredEmployees { get; set; }
    public int AssignedEmployees { get; set; }
    public int RemainingEmployees => Math.Max(RequiredEmployees - AssignedEmployees, 0);
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ScheduleWeekDetailResponse : ScheduleWeekSummaryResponse
{
    public List<ScheduleWeekShiftResponse> Shifts { get; set; } = [];
}

public sealed class ScheduleWeekShiftResponse
{
    public int ShiftId { get; set; }
    public int ShiftTemplateId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RequiredEmployees { get; set; }
    public int AssignedEmployees { get; set; }
    public int RemainingEmployees => Math.Max(RequiredEmployees - AssignedEmployees, 0);
    public List<ShiftEmployeeRequirementResponse> EmployeeRequirements { get; set; } = [];
}

public sealed class ShiftEmployeeRequirementResponse
{
    public int ShiftRequirementId { get; set; }
    public int EmployeeTypeId { get; set; }
    public string EmployeeTypeName { get; set; } = string.Empty;
    public int RequiredCount { get; set; }
    public int AssignedCount { get; set; }
    public int RemainingCount => Math.Max(RequiredCount - AssignedCount, 0);
    public bool IsFulfilled => AssignedCount >= RequiredCount;
}
