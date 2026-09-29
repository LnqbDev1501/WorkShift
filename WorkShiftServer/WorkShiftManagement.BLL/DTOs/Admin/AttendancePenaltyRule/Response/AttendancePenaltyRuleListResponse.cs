namespace WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Response;

public sealed class AttendancePenaltyRuleListResponse
{
    public IReadOnlyList<AttendancePenaltyRuleItemResponse> Items { get; set; } = Array.Empty<AttendancePenaltyRuleItemResponse>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
