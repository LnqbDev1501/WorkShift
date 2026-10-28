namespace WorkShiftManagement.DAL.Entities;

public partial class Payroll
{
    public int PayrollId { get; set; }

    public Guid UserId { get; set; }

    public int PayrollPeriodId { get; set; }

    public int TimesheetId { get; set; }

    public int BranchId { get; set; }

    public int SalaryHistoryId { get; set; }

    public string EmployeeFullNameSnapshot { get; set; } = null!;

    public string? EmployeePositionSnapshot { get; set; }

    public string BranchNameSnapshot { get; set; } = null!;

    public decimal HourlyRateSnapshot { get; set; }

    public decimal TotalWorkHours { get; set; }

    public decimal WorkPay { get; set; }

    public decimal TotalReward { get; set; }

    public decimal TotalPenalty { get; set; }

    public decimal AttendanceDeduction { get; set; }

    public decimal GrossSalary { get; set; }

    public decimal TotalDeduction { get; set; }

    public decimal NetSalary { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CalculatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int VersionNo { get; set; }

    public int? PreviousPayrollId { get; set; }

    public string? CorrectionReason { get; set; }

    public Guid? CalculatedBy { get; set; }

    public Guid? SubmittedBy { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? PaymentConfirmedBy { get; set; }

    public DateTime? PaymentConfirmedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? PaymentReference { get; set; }

    public string? PaymentNote { get; set; }

    public DateTime? ReplacedAt { get; set; }

    public decimal CarryForwardDeduction { get; set; }

    public decimal DeferredDeductionAmount { get; set; }

    public string? WorkInputSnapshot { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User? CalculatedByNavigation { get; set; }

    public virtual ICollection<EmployeeRewardsPenalty> EmployeeRewardsPenalties { get; set; } = new List<EmployeeRewardsPenalty>();

    public virtual Payroll? InversePreviousPayroll { get; set; }

    public virtual User? PaymentConfirmedByNavigation { get; set; }

    public virtual ICollection<PayrollCarryForwardApplication> PayrollCarryForwardApplications { get; set; } = new List<PayrollCarryForwardApplication>();

    public virtual ICollection<PayrollCarryForwardDeduction> PayrollCarryForwardDeductions { get; set; } = new List<PayrollCarryForwardDeduction>();

    public virtual ICollection<PayrollCorrectionRequest> PayrollCorrectionRequestPayrolls { get; set; } = new List<PayrollCorrectionRequest>();

    public virtual ICollection<PayrollCorrectionRequest> PayrollCorrectionRequestResolvedPayrolls { get; set; } = new List<PayrollCorrectionRequest>();

    public virtual ICollection<PayrollDetail> PayrollDetails { get; set; } = new List<PayrollDetail>();

    public virtual ICollection<PayrollFeedbackRequest> PayrollFeedbackRequests { get; set; } = new List<PayrollFeedbackRequest>();

    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;

    public virtual Payslip? Payslip { get; set; }

    public virtual Payroll? PreviousPayroll { get; set; }

    public virtual StaffSalaryHistory SalaryHistory { get; set; } = null!;

    public virtual User? SubmittedByNavigation { get; set; }

    public virtual Timesheet Timesheet { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
