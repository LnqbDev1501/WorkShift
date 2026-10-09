using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Request;

public sealed class GetAttendancePenaltyRulesRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
    public int PageNumber { get; set; } = 1;

    public string? Search { get; set; }

    [RegularExpression("^(DRAFT|SCHEDULED|ACTIVE|INACTIVE|CANCELLED)$",
        ErrorMessage = "Status must be one of: DRAFT, SCHEDULED, ACTIVE, INACTIVE, CANCELLED.")]
    public string? Status { get; set; }
}
