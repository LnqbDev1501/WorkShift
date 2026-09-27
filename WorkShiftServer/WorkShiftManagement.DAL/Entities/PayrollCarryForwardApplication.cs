using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class PayrollCarryForwardApplication
{
    public int ApplicationId { get; set; }

    public int CarryForwardId { get; set; }

    public int PayrollId { get; set; }

    public decimal AppliedAmount { get; set; }

    public DateTime AppliedAt { get; set; }

    public virtual PayrollCarryForwardDeduction CarryForward { get; set; } = null!;

    public virtual Payroll Payroll { get; set; } = null!;
}
