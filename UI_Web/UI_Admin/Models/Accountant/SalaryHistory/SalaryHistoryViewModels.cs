using System.ComponentModel.DataAnnotations;
using System.Globalization;
namespace UI_Admin.Models.Accountant.SalaryHistory;

public sealed class SalaryHistoryQuery
{
    [StringLength(200)] public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public string? Status { get; set; }
    public string? ChangeType { get; set; }
    [DataType(DataType.Date)] public DateOnly? EffectiveFrom { get; set; }
    [DataType(DataType.Date)] public DateOnly? EffectiveTo { get; set; }
    public int Page { get; set; } = 1;
}
public sealed class SalaryHistoryListViewModel
{
    public SalaryHistoryQuery Query { get; set; } = new();
    public List<SalaryHistoryItem> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;
    public string? ErrorMessage { get; set; }
}
public sealed class SalaryHistoryItem
{
    public int SalaryHistoryId { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeFullName { get; set; } = "";
    public decimal HourlyRate { get; set; }
    public decimal? PreviousHourlyRate { get; set; }
    public string ChangeType { get; set; } = "";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public int? SalaryIncreaseRequestId { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByFullName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public SalaryHistoryContract Contract { get; set; } = new();
}
public sealed class SalaryHistoryContract
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public string EmployeePosition { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "";
}
public static class SalaryHistoryDisplay
{
    public static string Money(decimal? amount) => amount.HasValue ? amount.Value.ToString("#,##0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫" : "—";
    public static string Date(DateOnly? date) => date?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Open-ended";
    public static string Label(string value) => value switch
    { "SCHEDULED" => "Scheduled", "ACTIVE" => "Active", "INACTIVE" => "Inactive", "INITIAL" => "Initial salary", "INCREASE" => "Increase", "DECREASE" => "Decrease", _ => value };
    public static string Badge(string status) => status switch
    { "ACTIVE" => "bg-emerald-50 text-emerald-700", "SCHEDULED" => "bg-amber-50 text-amber-700", _ => "bg-slate-100 text-slate-600" };
}
