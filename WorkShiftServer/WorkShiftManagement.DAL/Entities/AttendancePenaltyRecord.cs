namespace WorkShiftManagement.DAL.Entities;

public partial class AttendancePenaltyRecord
{
    public int AttendancePenaltyId { get; set; }

    public int AttendanceId { get; set; }

    public int PenaltyRuleId { get; set; }

    public int PenaltyLevelId { get; set; }

    public string ViolationType { get; set; } = null!;

    public int ViolationMinutes { get; set; }

    public decimal PenaltyAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual Attendance Attendance { get; set; } = null!;

    public virtual ICollection<PayrollDetail> PayrollDetails { get; set; } = new List<PayrollDetail>();

    public virtual AttendancePenaltyLevel PenaltyLevel { get; set; } = null!;

    public virtual AttendancePenaltyRule PenaltyRule { get; set; } = null!;
}
