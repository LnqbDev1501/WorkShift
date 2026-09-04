namespace WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Response
{
    public class PayrollPeriodListResponse
    {
        public List<PayrollPeriodListItemResponse> Items { get; set; }
            = new();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }

    public class PayrollPeriodListItemResponse
    {
        public int PayrollPeriodId { get; set; }

        public string PeriodCode { get; set; } = string.Empty;

        public string PeriodName { get; set; } = string.Empty;

        public int BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public int TotalPayrolls { get; set; }

        public int RejectedPayrollCount { get; set; }

        public decimal TotalNetSalary { get; set; }

        public PayrollActorResponse? SubmittedBy { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public PayrollActorResponse? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }
    }

    public class PayrollActorResponse
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } = string.Empty;
    }
}