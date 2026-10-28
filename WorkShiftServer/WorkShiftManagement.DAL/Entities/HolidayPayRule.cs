namespace WorkShiftManagement.DAL.Entities;

public partial class HolidayPayRule
{
    public int HolidayPayRuleId { get; set; }

    public string RuleName { get; set; } = null!;

    public decimal PayMultiplier { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public int? ReplacedRuleId { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly? EffectiveTo { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<HolidayPayRule> InverseReplacedRule { get; set; } = new List<HolidayPayRule>();

    public virtual ICollection<PayrollDetail> PayrollDetails { get; set; } = new List<PayrollDetail>();

    public virtual HolidayPayRule? ReplacedRule { get; set; }
}
