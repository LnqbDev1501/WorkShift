using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Models;

public sealed class AttendanceRuleSnapshotRoot
{
    public AttendanceRuleSnapshotItem? AttendanceRule { get; set; }
    public AttendancePenaltyRuleSnapshotItem? AttendancePenaltyRule { get; set; }
}

public sealed class AttendanceRuleSnapshotItem
{
    public int AttendanceRuleId { get; set; }
    public string? RuleName { get; set; }
    public int CheckInBeforeMinutes { get; set; }
    public int CheckInGraceMinutes { get; set; }
    public int CheckOutGraceMinutes { get; set; }
    public int CheckOutAfterMinutes { get; set; }
    public int MaxAttendanceDistanceMeters { get; set; }
}

public sealed class AttendancePenaltyRuleSnapshotItem
{
    public int PenaltyRuleId { get; set; }
    public string? RuleName { get; set; }
    public List<AttendancePenaltyLevelSnapshotItem>? PenaltyLevels { get; set; }
}

public sealed class AttendancePenaltyLevelSnapshotItem
{
    public int PenaltyLevelId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public int FromMinutes { get; set; }
    public int? ToMinutes { get; set; }
    public decimal PenaltyAmount { get; set; }
}
