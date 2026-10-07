namespace UI_Admin.Models.Admin.AttendancePenaltyRule;

public sealed class CreateAttendancePenaltyLevelViewModel
{
    public string ViolationType { get; set; } = "LATE"; // LATE or EARLY_LEAVE
    public int? FromMinutes { get; set; }
    public int? ToMinutes { get; set; }
    public decimal? PenaltyAmount { get; set; }
}
