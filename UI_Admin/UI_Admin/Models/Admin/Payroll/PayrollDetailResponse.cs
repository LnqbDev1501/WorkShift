namespace UI_Admin.Models.Admin.Payroll
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

        public int DisplayOrder { get; set; }

        public string? Note { get; set; }
    }
}
