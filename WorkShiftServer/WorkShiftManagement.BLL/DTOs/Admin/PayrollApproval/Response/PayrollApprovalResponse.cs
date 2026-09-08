namespace WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Response
{
    public class PayrollApprovalResponse
    {
        public string Message { get; set; } = string.Empty;

        public int PayrollPeriodId { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime ApprovedAt { get; set; }
    }

    public class PayrollRejectResponse
    {
        public string Message { get; set; } = string.Empty;

        public int PayrollPeriodId { get; set; }

        public int PayrollId { get; set; }

        public string PayrollStatus { get; set; } = string.Empty;

        public string PayrollPeriodStatus { get; set; } = string.Empty;

        public DateTime RejectedAt { get; set; }
    }

    public class PayrollSubmitResponse
    {
        public string Message { get; set; } = string.Empty;

        public int PayrollPeriodId { get; set; }

        public int PayrollId { get; set; }

        public string PayrollStatus { get; set; } = string.Empty;

        public string PayrollPeriodStatus { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }
    }
}
