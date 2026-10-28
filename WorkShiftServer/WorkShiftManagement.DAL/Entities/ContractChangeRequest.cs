namespace WorkShiftManagement.DAL.Entities;

public partial class ContractChangeRequest
{
    public int ChangeRequestId { get; set; }

    public int ContractId { get; set; }

    public Guid RequestedBy { get; set; }

    public string RequestType { get; set; } = null!;

    public string? StaffComment { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? SubmittedAt { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewComment { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual EmploymentContract Contract { get; set; } = null!;

    public virtual ICollection<ContractChangeRequestItem> ContractChangeRequestItems { get; set; } = new List<ContractChangeRequestItem>();

    public virtual User RequestedByNavigation { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }
}
