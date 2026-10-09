namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class RequestLimitRuleResponse
{
    public int RequestLimitRuleId { get; set; }
    public int BranchId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int MaxRequestsPerMonth { get; set; }
    public int MinimumAdvanceDays { get; set; }
    public short? SubmissionDeadlineDayOfWeek { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? SupersedesRuleId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
}
