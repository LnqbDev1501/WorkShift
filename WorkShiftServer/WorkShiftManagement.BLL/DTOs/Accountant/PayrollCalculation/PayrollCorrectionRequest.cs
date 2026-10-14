using System.ComponentModel.DataAnnotations;
namespace WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;

public sealed class PayrollCorrectionRequest
{
    [Required, StringLength(1000, MinimumLength = 1)]
    public string CorrectionReason { get; set; } = "";
    public int? FeedbackId { get; set; }
    public int? SalaryHistoryId { get; set; }
    public int? HolidayPayRuleId { get; set; }
    public List<AttendanceDeductionCorrection> AttendanceDeductions { get; set; } = [];
}
public sealed class AttendanceDeductionCorrection
{
    public int AttendancePenaltyId { get; set; }
    public decimal Amount { get; set; }
    [Required, StringLength(500)]
    public string Reason { get; set; } = "";
}
