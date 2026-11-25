namespace UI_Admin.Models.Admin.Payroll
{
    public class PayrollResponse
    {
        public int PayrollId { get; set; }
        public int VersionNo { get; set; } = 1;
        public int? PreviousPayrollId { get; set; }
        public string? CorrectionReason { get; set; }
        public decimal CarryForwardDeduction { get; set; }
        public decimal DeferredDeductionAmount { get; set; }

        public Guid UserId { get; set; }

        public string EmployeeFullName { get; set; } = string.Empty;

        public string? EmployeePosition { get; set; }

        public decimal HourlyRate { get; set; }

        public decimal TotalWorkHours { get; set; }

        public decimal WorkPay { get; set; }

        public decimal TotalReward { get; set; }

        public decimal TotalPenalty { get; set; }

        public decimal AttendanceDeduction { get; set; }

        public decimal GrossSalary { get; set; }

        public decimal TotalDeduction { get; set; }

        public decimal NetSalary { get; set; }

        public string Status { get; set; } = string.Empty;

        public PayrollRejectionResponse? Rejection { get; set; }

        public List<PayrollDetailResponse> Details { get; set; } = new();
    }

    public class PayrollRejectionResponse
    {
        public Guid? RejectedById { get; set; }

        public string? RejectedByName { get; set; }

        public DateTime? RejectedAt { get; set; }

        public string? Reason { get; set; }
    }
}
