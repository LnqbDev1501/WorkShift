namespace WorkShiftManagement.BLL.DTOs.Accountant.SalaryHistory.Request;

public sealed class SalaryHistoryListRequest
{
    public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public string? Status { get; set; }
    public string? ChangeType { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
