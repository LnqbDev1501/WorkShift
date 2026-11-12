using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Models.SalaryChanges;

public sealed class SalaryChangeQuery : IValidatableObject
{
    [StringLength(200)] public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    [Range(1, int.MaxValue)] public int? BranchId { get; set; }
    public string? Status { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    [Range(1, int.MaxValue / 100)] public int Page { get; set; } = 1;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (CreatedFrom > CreatedTo) yield return new("Created from must not be after created to.");
        if (EffectiveFrom > EffectiveTo) yield return new("Proposed date from must not be after proposed date to.");
        if (!string.IsNullOrEmpty(Status) && Status is not ("PENDING" or "APPROVED" or "REJECTED" or "CANCELLED"))
            yield return new("Invalid request status.");
    }
}

public sealed class SalaryChangeListViewModel
{
    public SalaryChangeQuery Query { get; set; } = new();
    public List<SalaryChangeItem> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class SalaryChangeItem
{
    public int SalaryChangeRequestId { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int? EmployeeRequestId { get; set; }
    public int? PreviousRequestId { get; set; }
    public int CurrentSalaryHistoryId { get; set; }
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = "";
    public decimal CurrentHourlyRate { get; set; }
    public decimal ProposedHourlyRate { get; set; }
    public DateOnly ProposedEffectiveFrom { get; set; }
    public DateOnly? ApprovedEffectiveFrom { get; set; }
    public string? EffectiveDateChangeReason { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public Guid RequestedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public int? NewSalaryHistoryId { get; set; }
    public string? NewSalaryStatus { get; set; }
    public bool CanCancel { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool RequiresEffectiveDateChange { get; set; }
    public DateOnly EarliestAllowedEffectiveFrom { get; set; }
    public string? ApprovalBlockedReason { get; set; }
}

public sealed class CreateSalaryChangeViewModel : IValidatableObject
{
    [Required] public Guid? UserId { get; set; }
    [Range(1, int.MaxValue)] public int? EmployeeRequestId { get; set; }
    [Range(1, int.MaxValue)] public int? PreviousRequestId { get; set; }
    [Required] public decimal? ProposedHourlyRate { get; set; }
    [Required, DataType(DataType.Date)] public DateOnly? ProposedEffectiveFrom { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; } = "";
    [BindNever, ValidateNever] public List<StaffAccountListItemViewModel> StaffOptions { get; set; } = [];
    [BindNever, ValidateNever] public bool OptionsLoaded { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (UserId == Guid.Empty) yield return new("Please select an employee.", [nameof(UserId)]);
        if (ProposedHourlyRate.HasValue && (ProposedHourlyRate <= 0 || ProposedHourlyRate > 9999999999999999.99m || decimal.Round(ProposedHourlyRate.Value, 2) != ProposedHourlyRate))
            yield return new("Hourly rate must be positive with at most two decimal places.", [nameof(ProposedHourlyRate)]);
        if (ProposedEffectiveFrom.HasValue && (ProposedEffectiveFrom < SalaryChangeDisplay.NextMonday || ProposedEffectiveFrom.Value.DayOfWeek != DayOfWeek.Monday))
            yield return new("Select a Monday from the next working week onward.", [nameof(ProposedEffectiveFrom)]);
        if (string.IsNullOrWhiteSpace(Reason)) yield return new("Please enter a reason.", [nameof(Reason)]);
    }
}

public sealed class SalaryChangeDecision
{
    public DateOnly? ApprovedEffectiveFrom { get; set; }
    [StringLength(1000)] public string? EffectiveDateChangeReason { get; set; }
    [StringLength(1000)] public string? ReviewNote { get; set; }
    [StringLength(1000)] public string? Reason { get; set; }
}

public sealed class SalaryChangeDetailsViewModel
{
    public SalaryChangeItem Item { get; set; } = new();
    public SalaryChangeDecision Decision { get; set; } = new();
}

public static class SalaryChangeDisplay
{
    public static DateOnly NextMonday
    {
        get { var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); var days = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7; return today.AddDays(days == 0 ? 7 : days); }
    }
    public static string Money(decimal value) => (Math.Abs(value) < 0.5m ? 0m : value).ToString("#,##0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    public static string Date(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? "—";
    public static string Time(DateTime? value) => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).AddHours(7).ToString("dd/MM/yyyy HH:mm") : "—";
    public static string Badge(string status) => status switch { "PENDING" => "bg-amber-50 text-amber-700", "APPROVED" => "bg-emerald-50 text-emerald-700", "REJECTED" => "bg-red-50 text-red-700", _ => "bg-gray-100 text-gray-700" };
}
