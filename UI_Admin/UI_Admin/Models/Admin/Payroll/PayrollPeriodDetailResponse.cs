namespace UI_Admin.Models.Admin.Payroll
{
    public class PayrollPeriodDetailResponse
    {
        public int PayrollPeriodId { get; set; }

        public string PeriodCode { get; set; } = string.Empty;

        public string PeriodName { get; set; } = string.Empty;

        public int BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public PayrollActorResponse? CalculatedBy { get; set; }

        public DateTime? CalculatedAt { get; set; }

        public PayrollActorResponse? SubmittedBy { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public PayrollActorResponse? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public PayrollSummaryResponse Summary { get; set; } = new();

        public PayrollPageResponse Payrolls { get; set; } = new();
    }

    public class PayrollSummaryResponse
    {
        public int TotalPayrolls { get; set; }

        public int RejectedPayrollCount { get; set; }

        public decimal TotalWorkPay { get; set; }

        public decimal TotalReward { get; set; }

        public decimal TotalPenalty { get; set; }

        public decimal TotalAttendanceDeduction { get; set; }

        public decimal TotalNetSalary { get; set; }
    }

    public class PayrollPageResponse
    {
        public List<PayrollResponse> Items { get; set; } = new();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}
