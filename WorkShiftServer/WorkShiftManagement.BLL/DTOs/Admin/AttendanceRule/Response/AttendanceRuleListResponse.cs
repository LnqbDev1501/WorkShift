namespace WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Response;

public class AttendanceRuleListResponse
{
    public IReadOnlyList<AttendanceRuleResponse> Items { get; set; } = Array.Empty<AttendanceRuleResponse>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
