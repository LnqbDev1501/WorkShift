using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class Payroll
{
    public int PayrollId { get; set; }

    public Guid UserId { get; set; }

    public int PayrollPeriodId { get; set; }

    public int TimesheetId { get; set; }

    public int BranchId { get; set; }

    public int SalaryHistoryId { get; set; }

    public string EmployeeFullNameSnapshot { get; set; } = null!;

    public string? EmployeePositionSnapshot { get; set; }

    public string BranchNameSnapshot { get; set; } = null!;

    public decimal HourlyRateSnapshot { get; set; }

    public decimal TotalWorkHours { get; set; }

    public decimal WorkPay { get; set; }

    public decimal TotalReward { get; set; }

    public decimal TotalPenalty { get; set; }

    public decimal AttendanceDeduction { get; set; }

    public decimal GrossSalary { get; set; }

    public decimal TotalDeduction { get; set; }

    public decimal NetSalary { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CalculatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<EmployeeRewardsPenalty> EmployeeRewardsPenalties { get; set; } = new List<EmployeeRewardsPenalty>();

    public virtual ICollection<PayrollDetail> PayrollDetails { get; set; } = new List<PayrollDetail>();

    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;

    public virtual StaffSalaryHistory SalaryHistory { get; set; } = null!;

    public virtual Timesheet Timesheet { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
