using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
public sealed class PayrollCorrectionRequest
{ [Range(1, int.MaxValue)] public int CorrectionRequestId { get; set; } }
