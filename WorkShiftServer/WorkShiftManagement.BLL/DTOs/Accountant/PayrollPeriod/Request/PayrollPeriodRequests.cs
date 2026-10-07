using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Accountant.PayrollPeriod.Request;

public sealed class PayrollPeriodListRequest
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateOnly? StartDateFrom { get; set; }
    public DateOnly? StartDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class SavePayrollPeriodRequest
{
    [Range(1, int.MaxValue)]
    public int ScheduleWeekId { get; set; }
    [Required, StringLength(200)]
    public string PeriodName { get; set; } = string.Empty;
}

public sealed class UpdatePayrollPeriodRequest
{
    [Range(1, int.MaxValue)]
    public int? ScheduleWeekId { get; set; }
    [StringLength(200)]
    public string? PeriodName { get; set; }
}
