using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class Branch
{
    public int BranchId { get; set; }

    public string BranchCode { get; set; } = null!;

    public string BranchName { get; set; } = null!;

    public int RegionId { get; set; }

    public string Address { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ChatConversation? ChatConversation { get; set; }

    public virtual ICollection<EmployeeRequest> EmployeeRequests { get; set; } = new List<EmployeeRequest>();

    public virtual ICollection<EmploymentContract> EmploymentContracts { get; set; } = new List<EmploymentContract>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PayrollPeriod> PayrollPeriods { get; set; } = new List<PayrollPeriod>();

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual Region Region { get; set; } = null!;

    public virtual ICollection<RequestLimitRule> RequestLimitRules { get; set; } = new List<RequestLimitRule>();

    public virtual ICollection<User> UserBranches { get; set; } = new List<User>();

    public virtual ICollection<User> UserPreviousBranches { get; set; } = new List<User>();

    public virtual ICollection<WorkScheduleWeek> WorkScheduleWeeks { get; set; } = new List<WorkScheduleWeek>();
}
