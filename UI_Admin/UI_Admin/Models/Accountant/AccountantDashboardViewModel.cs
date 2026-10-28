using System.Globalization;

namespace UI_Admin.Models.Accountant;

public sealed class AccountantDashboardData
{
    public DateOnly Date { get; set; }
    public List<AccountantDashboardMonthlySalary> MonthlySalaries { get; set; } = [];
    public int TotalPayrollPeriods { get; set; }
    public List<AccountantDashboardPeriodStatusResponse> PayrollPeriodsByStatus { get; set; } = [];
    public int TotalPayrolls { get; set; }
    public int CalculatedPayrolls { get; set; }
    public int SubmittedPayrolls { get; set; }
    public int RejectedPayrolls { get; set; }
    public int AwaitingPaymentPayrolls { get; set; }
    public decimal AwaitingPaymentNetSalary { get; set; }
    public List<AccountantDashboardPayrollStatusResponse> PayrollsByStatus { get; set; } = [];
    public List<AccountantDashboardRecentPeriodResponse> RecentPayrollPeriods { get; set; } = [];
}

public sealed class AccountantDashboardMonthlySalary
{
    public int Month { get; set; }
    public int PayrollCount { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal NetSalary { get; set; }
}

public sealed class AccountantDashboardPeriodStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class AccountantDashboardPayrollStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal NetSalary { get; set; }
}

public sealed class AccountantDashboardRecentPeriodResponse
{
    public int PayrollPeriodId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string PeriodCode { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalPayrolls { get; set; }
    public decimal TotalNetSalary { get; set; }
}

public sealed class AccountantDashboardViewModel
{
    public AccountantDashboardData? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public static string Number(int value) => value.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
    public static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("en-US")) + " VND";
    public static string Percent(int count, int total) => (total <= 0 ? 0 : Math.Clamp(100d * count / total, 0, 100)).ToString("0.##", CultureInfo.InvariantCulture);
    public static string ChartColor(string status) => status.ToUpperInvariant() switch
    {
        "APPROVED" => "#10a580", "PAID" => "#0284c7", "REJECTED" => "#df6575",
        "REVISION_REQUIRED" => "#ea580c", "SUBMITTED" => "#b77912",
        "CALCULATED" => "#6256db", _ => "#64748b"
    };
    public static string Label(string status) => status.ToUpperInvariant() switch
    {
        "DRAFT" => "Draft", "OPEN" => "Open", "CALCULATED" => "Calculated",
        "SUBMITTED" => "Submitted", "APPROVED" => "Approved", "REJECTED" => "Rejected",
        "REVISION_REQUIRED" => "Revision Required", "PAID" => "Paid",
        "CLOSED" => "Closed", "CANCELLED" => "Cancelled", _ => status
    };
    public static string Tone(string status) => status.ToUpperInvariant() switch
    {
        "APPROVED" or "PAID" => "success", "REJECTED" or "REVISION_REQUIRED" => "danger",
        "SUBMITTED" => "warning", "CALCULATED" or "OPEN" => "accent", _ => "muted"
    };
}
