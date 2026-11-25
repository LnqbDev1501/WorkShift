namespace WorkShiftManagement.DAL.ReadModels.Staff;

public sealed class StaffShiftReadModel
{
    public int ShiftId { get; set; }
    public int ScheduleWeekId { get; set; }
    public string ShiftNameSnapshot { get; set; } = "";
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public string ScheduleWeekStatus { get; set; } = "";
    public DateTime RegistrationDeadline { get; set; }
    public List<StaffRegistrationReadModel> Registrations { get; set; } = [];
    public List<StaffRequirementReadModel> EmployeeRequirements { get; set; } = [];
}

public sealed class StaffRegistrationReadModel
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public int? ShiftRequirementId { get; set; }
    public string AssignmentSource { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime AssignedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public bool HasAttendanceOrPayroll { get; set; }
    public bool HasBlockingRequest { get; set; }
}

public sealed class StaffRequirementReadModel
{
    public int ShiftRequirementId { get; set; }
    public int EmployeeTypeId { get; set; }
    public int RequiredCount { get; set; }
    public string EmployeeTypeName { get; set; } = "";
    public int AssignedCount { get; set; }
}

