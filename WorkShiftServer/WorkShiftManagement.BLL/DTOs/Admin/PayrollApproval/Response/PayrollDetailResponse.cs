namespace WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Response
{
    public class PayrollDetailResponse
    {
        public int PayrollDetailId { get; set; }

        public string Type { get; set; } = string.Empty;

        public string? ItemCode { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal Multiplier { get; set; }

        public DateOnly? WorkFrom { get; set; }

        public DateOnly? WorkTo { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? Rate { get; set; }

        public decimal Amount { get; set; }
        public decimal? AppliedAmount { get; set; }
        public int? SalaryHistoryId { get; set; }
        public int? RewardPenaltyId { get; set; }
        public int? AttendancePenaltyId { get; set; }
        public int? CarryForwardId { get; set; }
        public int? PublicHolidayId { get; set; }
        public int? HolidayPayRuleId { get; set; }

        public int DisplayOrder { get; set; }

        public string? Note { get; set; }
    }
}
