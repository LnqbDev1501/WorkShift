namespace WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Response;

public sealed class PublicHolidayResponse
{
    public int PublicHolidayId { get; set; }
    public string HolidayName { get; set; } = null!;
    public DateOnly HolidayDate { get; set; }
    public decimal? PayMultiplier { get; set; }
    public int? HolidayPayRuleId { get; set; }
    public bool IsPayrollLocked { get; set; }
    public List<int> AffectedPayrollPeriodIds { get; set; } = [];
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

