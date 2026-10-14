namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public sealed class StaffShiftResponse
{
    public int ShiftId { get; set; }
    public int ScheduleWeekId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public string ShiftName { get; set; } = "";
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string Status { get; set; } = "";
    public string ScheduleWeekStatus { get; set; } = "";
    public DateTime RegistrationDeadline { get; set; }
    public string? Note { get; set; }
    public StaffShiftRegistrationResponse? MyRegistration { get; set; }
    public List<StaffShiftRequirementResponse> EmployeeRequirements { get; set; } = [];
}

public sealed class StaffShiftRequirementResponse
{
    public int ShiftRequirementId { get; set; }
    public int EmployeeTypeId { get; set; }
    public string EmployeeTypeName { get; set; } = "";
    public int RequiredCount { get; set; }
    public int AssignedCount { get; set; }
    public int RemainingCount { get; set; }
}
