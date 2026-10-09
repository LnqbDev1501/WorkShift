using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class PayrollCarryForwardDeduction
{
    public int CarryForwardId { get; set; }

    public Guid UserId { get; set; }

    public int SourcePayrollId { get; set; }

    public int SourcePayrollDetailId { get; set; }

    public decimal OriginalAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PayrollCarryForwardApplication> PayrollCarryForwardApplications { get; set; } = new List<PayrollCarryForwardApplication>();

    public virtual ICollection<PayrollDetail> PayrollDetails { get; set; } = new List<PayrollDetail>();

    public virtual Payroll SourcePayroll { get; set; } = null!;

    public virtual PayrollDetail SourcePayrollDetail { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
