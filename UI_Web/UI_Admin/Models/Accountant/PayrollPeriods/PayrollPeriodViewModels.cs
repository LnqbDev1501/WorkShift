using System.ComponentModel.DataAnnotations;
using UI_Admin.Models.Admin.Payroll;
namespace UI_Admin.Models.Accountant.PayrollPeriods;

public sealed class PeriodQuery
{
    [StringLength(200)] public string? Search { get; set; }
    public string? Status { get; set; }
    public DateOnly? StartDateFrom { get; set; }
    public DateOnly? StartDateTo { get; set; }
    [Range(1, int.MaxValue / 100)] public int PageNumber { get; set; } = 1;
}
public sealed class PeriodPage<T>
{
    public List<T> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize)));
}
public sealed class PeriodListViewModel
{
    public PeriodQuery Query { get; set; } = new();
    public PeriodPage<PeriodRecord> Page { get; set; } = new();
    public string? Error { get; set; }
}
public sealed class PeriodRecord
{
    public int PayrollPeriodId { get; set; }
    public int? ScheduleWeekId { get; set; }
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "";
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovalDeadlineAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? InitialFeedbackDeadlineAt { get; set; }
    public int ReviewedTimesheetCount { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int ExpectedEmployeeCount { get; set; }
    public int LockedTimesheetCount { get; set; }
    public int TotalPayrolls { get; set; }
    public int RejectedPayrollCount { get; set; }
    public int PendingCorrectionCount { get; set; }
    public decimal TotalNetSalary { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanOpen { get; set; }
    public bool CanExport => Status is "APPROVED" or "PAID" or "CLOSED";
    public List<PayrollResponse> Payrolls { get; set; } = [];
    public List<UI_Admin.Models.PayrollDocuments.PayrollIssueResponse> CorrectionRequests { get; set; } = [];
    public List<UI_Admin.Models.PayrollDocuments.PayrollFeedbackResponse> Feedbacks { get; set; } = [];
    public UI_Admin.Models.Accountant.PayrollSettlement.PayrollSettlementSummaryResponse? SettlementSummary { get; set; }
    public UI_Admin.Models.PayrollDocuments.PayrollSourceImpactResponse? SourceImpact { get; set; }
}
public sealed class ScheduleWeekOption
{
    public int ScheduleWeekId { get; set; }
    public int BranchId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string Status { get; set; } = "";
}
public sealed class PeriodForm
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string PeriodName { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Choose an available work schedule week.")] public int ScheduleWeekId { get; set; }
    public List<ScheduleWeekOption> Weeks { get; set; } = [];
    public bool WeeksLoaded { get; set; }
}
public sealed record PeriodConfirmation(PeriodRecord Period, bool Delete);
public static class PeriodDisplay
{
    public static readonly string[] Statuses = ["DRAFT", "OPEN", "CALCULATED", "SUBMITTED", "REVISION_REQUIRED", "APPROVED", "PAID", "CLOSED"];
    public static string Money(decimal value) => value.ToString("#,##0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    public static string Time(DateTime? value) => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).AddHours(7).ToString("dd/MM/yyyy HH:mm") : "—";
}
