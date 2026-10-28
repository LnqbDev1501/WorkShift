using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UI_Admin.Models.Manager;
public sealed class ManagerShiftEditViewModel
{
    public int? Id { get; set; }
    public int WeekId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Shift template")]
    public int ShiftTemplateId { get; set; }
    [Required, Display(Name = "Work date")]
    public DateOnly? WorkDate { get; set; }
    [StringLength(500)] public string? Note { get; set; }
    [Required, MinLength(1)] public List<ShiftRequirementInput> EmployeeRequirements { get; set; } = [new()];
    [BindNever] public ShiftOptionsResponse Options { get; set; } = new();
    [BindNever] public ScheduleWeekDetailResponse? Week { get; set; }
    [BindNever] public bool CanSave { get; set; }
    [BindNever] public string? ErrorMessage { get; set; }
}
public sealed class ShiftRequirementInput
{
    [Range(1, int.MaxValue)] public int EmployeeTypeId { get; set; }
    [Range(1, int.MaxValue)] public int RequiredCount { get; set; } = 1;
}
public sealed class ManagerShiftDetailViewModel
{
    public int Id { get; set; }
    public ShiftDetailResponse? Shift { get; set; }
    public ScheduleWeekDetailResponse? Week { get; set; }
    public ShiftOptionsResponse Options { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool CanEdit => Shift?.Status == "DRAFT" && Week?.Status == "DRAFT" && Shift.Staff.Count == 0;
    public bool CanChangeAssignments => Shift?.Status == "SCHEDULED" && Week?.Status is "OPEN_REGISTRATION" or "REGISTRATION_CLOSED" && ScheduleDisplay.Vietnam(Shift.ScheduledStartAt) > DateTime.UtcNow.AddHours(7);
    public DateTime? AssignmentDeadline => Shift is null ? null : ScheduleDisplay.Vietnam(Shift.ScheduledStartAt).Date.AddDays(-1);
    public bool CanAssign => CanChangeAssignments && AssignmentAllowedAt(Shift!.ScheduledStartAt, DateTime.UtcNow);
    public static bool AssignmentAllowedAt(DateTime scheduledStartAt, DateTime utcNow) => utcNow.AddHours(7) < ScheduleDisplay.Vietnam(scheduledStartAt).Date.AddDays(-1);
}
public sealed class ShiftCancelInput
{
    [Required, StringLength(500)] public string Reason { get; set; } = "";
}
