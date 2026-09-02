using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class PayrollDetail
{
    public int PayrollDetailId { get; set; }

    public int PayrollId { get; set; }

    public int? PublicHolidayId { get; set; }

    public decimal Multiplier { get; set; }

    public string Type { get; set; } = null!;

    public string? ItemCode { get; set; }

    public string Description { get; set; } = null!;

    public int? SalaryHistoryId { get; set; }

    public int? RewardPenaltyId { get; set; }

    public int? AttendancePenaltyId { get; set; }

    public DateOnly? WorkFrom { get; set; }

    public DateOnly? WorkTo { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? Rate { get; set; }

    public decimal Amount { get; set; }

    public int DisplayOrder { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AttendancePenaltyRecord? AttendancePenalty { get; set; }

    public virtual Payroll Payroll { get; set; } = null!;

    public virtual PublicHolidayConfig? PublicHoliday { get; set; }

    public virtual EmployeeRewardsPenalty? RewardPenalty { get; set; }

    public virtual StaffSalaryHistory? SalaryHistory { get; set; }
}
