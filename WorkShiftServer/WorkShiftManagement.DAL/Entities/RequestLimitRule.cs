namespace WorkShiftManagement.DAL.Entities;

public partial class RequestLimitRule
{
    public int RequestLimitRuleId { get; set; }

    public int BranchId { get; set; }

    public string RequestType { get; set; } = null!;

    public int MaxRequestsPerMonth { get; set; }

    public int MinimumAdvanceDays { get; set; }

    public short? SubmissionDeadlineDayOfWeek { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string Status { get; set; } = null!;

    public int? SupersedesRuleId { get; set; }

    public Guid? PublishedBy { get; set; }

    public DateTime? PublishedAt { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<EmployeeRequest> EmployeeRequests { get; set; } = new List<EmployeeRequest>();

    public virtual ICollection<RequestLimitRule> InverseSupersedesRule { get; set; } = new List<RequestLimitRule>();

    public virtual User? PublishedByNavigation { get; set; }

    public virtual RequestLimitRule? SupersedesRule { get; set; }
}
