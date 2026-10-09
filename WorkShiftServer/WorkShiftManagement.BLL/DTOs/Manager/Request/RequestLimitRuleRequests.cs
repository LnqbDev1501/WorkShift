using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class RequestLimitRuleListQuery
{
    public string? RequestType { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateRequestLimitRuleRequest
{
    [Required] public string RequestType { get; set; } = string.Empty;
    [Required] public int? MaxRequestsPerMonth { get; set; }
    [Required] public int? MinimumAdvanceDays { get; set; }
    public short? SubmissionDeadlineDayOfWeek { get; set; }
    [Required] public DateOnly? EffectiveFrom { get; set; }
}

public class UpdateRequestLimitRuleRequest : CreateRequestLimitRuleRequest;
public class ReplaceRequestLimitRuleRequest : CreateRequestLimitRuleRequest;
