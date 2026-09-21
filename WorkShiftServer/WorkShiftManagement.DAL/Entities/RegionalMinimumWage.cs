using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class RegionalMinimumWage
{
    public int RegionalMinimumWageId { get; set; }

    public string WageName { get; set; } = null!;

    public decimal MinimumHourlyRate { get; set; }

    public string? Description { get; set; }

    public int? ReplacedWageId { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<RegionalMinimumWage> InverseReplacedWage { get; set; } = new List<RegionalMinimumWage>();

    public virtual ICollection<Region> Regions { get; set; } = new List<Region>();

    public virtual RegionalMinimumWage? ReplacedWage { get; set; }
}
