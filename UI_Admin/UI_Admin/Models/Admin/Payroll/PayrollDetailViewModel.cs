using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.Payroll;

public class PayrollDetailQuery
{
    [StringLength(100)] public string? Search { get; set; }
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;
}

public class PayrollReviewInput : PayrollDetailQuery
{
    [Range(1, int.MaxValue)] public int PeriodId { get; set; }
}

public class PayrollRejectInput : PayrollReviewInput
{
    [Required, RegularExpression("^(CALCULATION|RATE|REWARD|DEDUCTION|OTHER)$")]
    public string RejectionCategory { get; set; } = "";
    [Range(1, int.MaxValue)] public int PayrollId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập lý do yêu cầu chỉnh sửa.")]
    [StringLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự.")]
    public string Reason { get; set; } = "";
}

public class PayrollDetailViewModel
{
    public PayrollPeriodDetailResponse Period { get; set; } = new();
    public PayrollDetailQuery Query { get; set; } = new();
    public int? RejectedPayrollId { get; set; }
    public string? RejectionReason { get; set; }
}
