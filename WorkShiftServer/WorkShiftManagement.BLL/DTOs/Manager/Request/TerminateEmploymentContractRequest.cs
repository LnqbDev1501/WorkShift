using System;
using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class TerminateEmploymentContractRequest
{
    [Required(ErrorMessage = "A termination reason is required.")]
    [MaxLength(500, ErrorMessage = "The reason cannot exceed 500 characters.")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Ngày có hiệu lực chấm dứt hợp đồng. Nếu không cung cấp, mặc định là ngày hôm nay.
    /// </summary>
    public DateOnly? TerminationEffectiveDate { get; set; }
}
