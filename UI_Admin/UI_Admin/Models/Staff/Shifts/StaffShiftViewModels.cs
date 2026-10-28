using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Staff.Shifts;

public class StaffShiftListViewModel
{
    [DataType(DataType.Date)]
    public DateOnly? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? ToDate { get; set; }

    public string ActiveTab { get; set; } = "available"; // "available" or "my-schedule"

    public IReadOnlyList<StaffShiftListItemViewModel> AvailableShifts { get; set; } = [];
    public IReadOnlyList<StaffShiftListItemViewModel> MySchedule { get; set; } = [];

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class StaffShiftListItemViewModel
{
    public int ShiftId { get; set; }
    public int ScheduleWeekId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ScheduleWeekStatus { get; set; } = string.Empty;
    public DateTime RegistrationDeadline { get; set; }
    public string? Note { get; set; }
    public StaffShiftRegistrationViewModel? MyRegistration { get; set; }
    public bool CanCancelRegistration { get; set; }
    public string? CancellationBlockedReason { get; set; }
    public List<StaffShiftRequirementViewModel> EmployeeRequirements { get; set; } = [];

    public bool IsRegistered => MyRegistration != null && MyRegistration.Status == "ASSIGNED";
    public bool IsRegistrationClosed => ScheduleWeekStatus != "OPEN_REGISTRATION" ||
        DateTime.UtcNow >= RegistrationDeadline || ScheduledStartAt <= DateTime.UtcNow ||
        Status != "SCHEDULED" || ScheduledEndAt <= ScheduledStartAt;
    public bool CanCancel => CanCancelRegistration && !IsRegistrationClosed && IsRegistered &&
        MyRegistration!.AssignmentSource == "SELF_REGISTRATION";

    public string DateDisplay => ScheduledStartAt.AddHours(7).ToString("dd/MM/yyyy (ddd)");
    public string TimeDisplay => $"{ScheduledStartAt.AddHours(7):HH:mm} - {ScheduledEndAt.AddHours(7):HH:mm}";
    public string DeadlineDisplay => RegistrationDeadline.AddHours(7).ToString("HH:mm dd/MM/yyyy");
    public double DurationHours => (ScheduledEndAt - ScheduledStartAt).TotalHours;
}

public class StaffShiftRequirementViewModel
{
    public int ShiftRequirementId { get; set; }
    public int EmployeeTypeId { get; set; }
    public string EmployeeTypeName { get; set; } = string.Empty;
    public int RequiredCount { get; set; }
    public int AssignedCount { get; set; }
    public int RemainingCount { get; set; }
}

public class StaffShiftRegistrationViewModel
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public int? ShiftRequirementId { get; set; }
    public string AssignmentSource { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
