namespace UI_Admin.Models.Manager;

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

public sealed class ManagerScheduleFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Status { get; set; }
}
public sealed class ManagerScheduleListViewModel
{
    public ManagerScheduleFilter Filter { get; set; } = new();
    public List<ScheduleWeekSummaryResponse>? Weeks { get; set; }
    public string? ErrorMessage { get; set; }
}
public sealed class ManagerScheduleDetailViewModel
{
    public int Id { get; set; }
    public ScheduleWeekDetailResponse? Week { get; set; }
    public string? ErrorMessage { get; set; }
}
public static class ScheduleDisplay
{
    public static readonly string[] Statuses = ["DRAFT", "OPEN_REGISTRATION", "REGISTRATION_CLOSED", "COMPLETED", "CANCELLED"];
    public static string Label(string value) => ManagerDashboardViewModel.Label(value);
    public static string Tone(string value) => value switch
    {
        "OPEN_REGISTRATION" or "SCHEDULED" => "accent", "COMPLETED" => "success",
        "CANCELLED" => "danger", "REGISTRATION_CLOSED" => "warning", _ => "muted"
    };
    public static DateTime Vietnam(DateTime value) => (value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc)).AddHours(7);
    public static string Date(DateOnly value) => value.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
    public static string Time(DateTime value) => Vietnam(value).ToString("dd MMM yyyy, HH:mm", System.Globalization.CultureInfo.InvariantCulture);
    public static string Coverage(int assigned, int required) => DashboardViewModel.Percent(assigned, required);
}
public sealed class ManagerScheduleEditViewModel
{
    public int? Id { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Display(Name = "Week start date")]
    public DateOnly? WeekStartDate { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Display(Name = "Registration deadline")]
    public DateTime? RegistrationDeadline { get; set; }
    public bool LockStartDate { get; set; }
    public bool CanSave { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
