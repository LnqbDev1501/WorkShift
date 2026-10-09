namespace WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Request;

public sealed class CreateAttendancePenaltyLevelRequest
{
    public string ViolationType { get; set; } = string.Empty;
    public int? FromMinutes { get; set; }
    public int? ToMinutes { get; set; }
    public decimal? PenaltyAmount { get; set; }
}
