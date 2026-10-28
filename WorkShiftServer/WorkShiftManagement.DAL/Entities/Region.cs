namespace WorkShiftManagement.DAL.Entities;

public partial class Region
{
    public int RegionId { get; set; }

    public string RegionCode { get; set; } = null!;

    public string RegionName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? RegionalMinimumWageId { get; set; }

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();

    public virtual ICollection<EmploymentContract> EmploymentContracts { get; set; } = new List<EmploymentContract>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual RegionalMinimumWage? RegionalMinimumWage { get; set; }
}
