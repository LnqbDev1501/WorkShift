namespace WorkShiftManagement.BLL.DTOs.Accountant.Response;

public sealed class AccountantDashboardResponse
{
    public DateOnly Date { get; set; }
    public List<AccountantDashboardMonthlySalaryResponse> MonthlySalaries { get; set; } = [];
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

public sealed class AccountantDashboardMonthlySalaryResponse
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
