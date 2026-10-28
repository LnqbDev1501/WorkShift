namespace WorkShiftManagement.DAL.Entities;

public partial class EmployeeRewardsPenalty
{
    public int RewardPenaltyId { get; set; }

    public Guid UserId { get; set; }

    public string AdjustmentType { get; set; } = null!;

    public int PayrollPeriodId { get; set; }

    public int? PayrollId { get; set; }

    public string AdjustmentName { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Reason { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletionReason { get; set; }

    public string? ApprovalStatus { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? RejectedBy { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? ReviewNote { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? DeletedByNavigation { get; set; }

    public virtual Payroll? Payroll { get; set; }

    public virtual ICollection<PayrollDetail> PayrollDetails { get; set; } = new List<PayrollDetail>();

    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;

    public virtual User? RejectedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
