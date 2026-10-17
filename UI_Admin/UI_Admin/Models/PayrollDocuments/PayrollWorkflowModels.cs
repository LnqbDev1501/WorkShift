using System.ComponentModel.DataAnnotations;
using UI_Admin.Models.PayrollCalculation;
namespace UI_Admin.Models.PayrollDocuments;

public sealed class PayrollVersions
{
    public Guid UserId { get; set; }
    public int PayrollPeriodId { get; set; }
    public int LatestCandidatePayrollId { get; set; }
    public int? EffectivePayrollId { get; set; }
    public List<CalculatedPayroll> Versions { get; set; } = [];
}
public sealed class CreateCorrectionInput
{
    [Range(1, int.MaxValue)] public int PayrollId { get; set; }
    [Required, StringLength(2000)] public string Reason { get; set; } = "";
}

public sealed class RespondCorrectionInput
{
    [Range(1, int.MaxValue)] public int CorrectionRequestId { get; set; }
    [Required] public string Action { get; set; } = "CORRECT"; // CORRECT or EXPLAIN
    [Required, StringLength(2000)] public string Note { get; set; } = "";
}

public sealed class ReviewCorrectionInput
{
    [Range(1, int.MaxValue)] public int CorrectionRequestId { get; set; }
    [Required] public string Decision { get; set; } = "ACCEPT_EXPLANATION"; // ACCEPT_EXPLANATION or RETURN
    [Required, StringLength(2000)] public string Note { get; set; } = "";
}

public sealed class CompleteCorrectionInput
{
    [Range(1, int.MaxValue)] public int CorrectionRequestId { get; set; }
    [Range(1, int.MaxValue)] public int PayrollId { get; set; }
    [Required, StringLength(2000)] public string Note { get; set; } = "";
}

public sealed class CorrectionInput
{
    [Required, StringLength(1000)] public string CorrectionReason { get; set; } = "";
    [Range(1, int.MaxValue)] public int? SalaryHistoryId { get; set; }
    [Range(1, int.MaxValue)] public int? HolidayPayRuleId { get; set; }
    public List<AttendanceCorrectionInput> AttendanceDeductions { get; set; } = [];
}
public sealed class AttendanceCorrectionInput
{
    [Range(1, int.MaxValue)] public int AttendancePenaltyId { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(UI_Admin.Services.InvariantMoneyModelBinder))]
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal Amount { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
}
public sealed class CorrectionForm
{
    public int SourceId { get; set; }
    public bool FromFeedback { get; set; }
    public CalculatedPayroll Payroll { get; set; } = new();
    public CorrectionInput Input { get; set; } = new();
}
public sealed class PayrollReview
{
    public CalculatedPayroll Payroll { get; set; } = new();
    public PayrollVersions History { get; set; } = new();
    public string PeriodStatus { get; set; } = "";
    public bool Latest => History.LatestCandidatePayrollId == Payroll.PayrollId;
    public List<PayrollIssueResponse> Issues { get; set; } = [];
    public PayrollIssueResponse? ActiveIssue => Issues.LastOrDefault(i => i.Status is "OPEN" or "RESPONDED");
}
public sealed class PayslipListViewModel
{
    public PayslipListRequest Query { get; set; } = new();
    public PayslipListResponse Data { get; set; } = new() { Page = 1, PageSize = 20 };
    public string? Error { get; set; }
}
public sealed class FeedbackListViewModel
{
    public PayrollFeedbackListRequest Query { get; set; } = new();
    public PayrollFeedbackListResponse Data { get; set; } = new() { Page = 1, PageSize = 20 };
    public string? Error { get; set; }
}
public sealed class FeedbackSubmitForm
{
    public PayslipSummaryResponse Payslip { get; set; } = new();
    public SubmitPayrollFeedbackRequest Input { get; set; } = new();
}
public sealed class FeedbackReviewInput
{
    [StringLength(2000)] public string? ResolutionNote { get; set; }
}
public sealed class PayrollRejectionDecision
{
    [Required, StringLength(1000)] public string Reason { get; set; } = "";
    [Required, RegularExpression("^(CALCULATION|RATE|REWARD|DEDUCTION|OTHER)$")]
    public string RejectionCategory { get; set; } = "";
}
public static class PayrollDocumentDisplay
{
    public static readonly string[] Reasons = ["CALCULATION", "RATE", "REWARD", "DEDUCTION", "OTHER"];
    public static string RolePrefix(System.Security.Claims.ClaimsPrincipal user) => user.IsInRole("STAFF") ? "Staff" : user.IsInRole("ACCOUNTANT") ? "Accountant" : "Admin";
    public static string Time(DateTime? date) => date.HasValue ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc).AddHours(7).ToString("HH:mm") : "—";
}
