using System;
using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class TerminateEmploymentContractRequest
{
    [Required(ErrorMessage = "Lý do chấm dứt hợp đồng là bắt buộc.")]
    [MaxLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Ngày có hiệu lực chấm dứt hợp đồng. Nếu không cung cấp, mặc định là ngày hôm nay.
    /// </summary>
    public DateOnly? TerminationEffectiveDate { get; set; }
}
