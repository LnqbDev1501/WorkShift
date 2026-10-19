using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Models.EmployeeAdjustments;

public static class EmployeeAdjustmentTypes
{
    public const string Reward = "REWARD";
    public const string Penalty = "PENALTY";

    public static string Label(string type) => type == Penalty ? "Penalty" : "Reward";
    public static string Plural(string type) => type == Penalty ? "Penalties" : "Rewards";
    public static bool IsValid(string? type) => type is Reward or Penalty;
}

public sealed class EmployeeAdjustmentQuery : IValidatableObject
{
    [StringLength(200)] public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    [Range(1, int.MaxValue)] public int? PayrollPeriodId { get; set; }
    [Range(1, int.MaxValue)] public int? BranchId { get; set; }
    public string? ApprovalStatus { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
    [Range(1, int.MaxValue / 100)] public int Page { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (CreatedFrom > CreatedTo) yield return new("Created from must not be after created to.");
        if (!string.IsNullOrEmpty(ApprovalStatus) && ApprovalStatus is not ("PENDING" or "APPROVED" or "REJECTED"))
            yield return new("Invalid approval status.");
    }
}

public sealed class EmployeeAdjustmentListViewModel
{
    public string AdjustmentType { get; set; } = EmployeeAdjustmentTypes.Reward;
    public EmployeeAdjustmentQuery Query { get; set; } = new();
    public List<EmployeeAdjustmentItem> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class EmployeeAdjustmentItem
{
    public int RewardPenaltyId { get; set; }
    public string AdjustmentType { get; set; } = "";
    public Guid UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int PayrollPeriodId { get; set; }
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string PeriodStatus { get; set; } = "";
    public string AdjustmentName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public string ApprovalStatus { get; set; } = "";
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? ReviewNote { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
}

public sealed class SaveEmployeeAdjustmentViewModel : IValidatableObject
{
    public string AdjustmentType { get; set; } = EmployeeAdjustmentTypes.Reward;
    [Required] public Guid? UserId { get; set; }
    [Range(1, int.MaxValue)] public int? PayrollPeriodId { get; set; }
    [Required, StringLength(100)] public string AdjustmentName { get; set; } = "";
    [Required] public decimal? Amount { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
    [BindNever, ValidateNever] public List<StaffAccountListItemViewModel> StaffOptions { get; set; } = [];
    [BindNever, ValidateNever] public List<PayrollPeriodOption> PayrollPeriodOptions { get; set; } = [];
    [BindNever, ValidateNever] public bool OptionsLoaded { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (UserId == Guid.Empty) yield return new("Please select an employee.", [nameof(UserId)]);
        if (PayrollPeriodId is null or <= 0) yield return new("Please select a payroll period.", [nameof(PayrollPeriodId)]);
        if (string.IsNullOrWhiteSpace(AdjustmentName)) yield return new("Please enter the item name.", [nameof(AdjustmentName)]);
        if (AdjustmentName.Trim().Length > 100) yield return new("Item name must be at most 100 characters.", [nameof(AdjustmentName)]);
        if (Amount is null) yield return new("Please enter the amount.", [nameof(Amount)]);
        else if (Amount <= 0 || Amount > 9999999999999999.99m || Amount != decimal.Truncate(Amount.Value * 100) / 100)
            yield return new("Amount must be greater than 0 with at most two decimal places (max 16 integer digits).", [nameof(Amount)]);
        if (string.IsNullOrWhiteSpace(Reason)) yield return new("Please enter a reason.", [nameof(Reason)]);
        if (Reason.Trim().Length > 500) yield return new("Reason must be at most 500 characters.", [nameof(Reason)]);
    }
}

public sealed class PayrollPeriodOption
{
    public int PayrollPeriodId { get; set; }
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "";
}

public sealed class EmployeeAdjustmentDetailsViewModel
{
    public EmployeeAdjustmentItem Item { get; set; } = new();
    public ReviewEmployeeAdjustmentViewModel Decision { get; set; } = new();
}

public sealed class ReviewEmployeeAdjustmentViewModel
{
    [StringLength(1000)] public string? ReviewNote { get; set; }
}

public static class EmployeeAdjustmentDisplay
{
    public static string Money(decimal value) =>
        value.ToString("#,0.##", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";

    public static string Date(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? "—";

    public static string Time(DateTime? value) => value.HasValue
        ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).AddHours(7).ToString("dd/MM/yyyy HH:mm")
        : "—";

    public static string Badge(string status) => status switch
    {
        "PENDING" => "bg-amber-50 text-amber-700",
        "APPROVED" => "bg-emerald-50 text-emerald-700",
        "REJECTED" => "bg-red-50 text-red-700",
        _ => "bg-gray-100 text-gray-700"
    };

    public static string PeriodStatusBadge(string status) => status switch
    {
        "DRAFT" => "bg-gray-100 text-gray-700",
        "OPEN" => "bg-sky-50 text-sky-700",
        "CALCULATED" => "bg-indigo-50 text-indigo-700",
        "SUBMITTED" => "bg-blue-50 text-blue-700",
        "REVISION_REQUIRED" => "bg-amber-50 text-amber-700",
        "APPROVED" => "bg-emerald-50 text-emerald-700",
        "PAID" or "CLOSED" => "bg-gray-200 text-gray-600",
        _ => "bg-gray-100 text-gray-700"
    };
}
