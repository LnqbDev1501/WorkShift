using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class SalaryIncreaseRequestDetail
{
    public int RequestId { get; set; }

    public int CurrentContractId { get; set; }

    public decimal CurrentHourlyRate { get; set; }

    public decimal ProposedHourlyRate { get; set; }

    public virtual EmploymentContract CurrentContract { get; set; } = null!;

    public virtual EmployeeRequest Request { get; set; } = null!;

    public virtual ICollection<StaffSalaryHistory> StaffSalaryHistories { get; set; } = new List<StaffSalaryHistory>();
}
