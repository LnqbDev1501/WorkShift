namespace WorkShiftManagement.DAL.Entities;

public partial class AttendancePenaltyLevel
{
    public int PenaltyLevelId { get; set; }

    public int PenaltyRuleId { get; set; }

    public string ViolationType { get; set; } = null!;

    public int FromMinutes { get; set; }

    public int? ToMinutes { get; set; }

    public decimal PenaltyAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AttendancePenaltyRecord> AttendancePenaltyRecords { get; set; } = new List<AttendancePenaltyRecord>();

    public virtual AttendancePenaltyRule PenaltyRule { get; set; } = null!;
}
