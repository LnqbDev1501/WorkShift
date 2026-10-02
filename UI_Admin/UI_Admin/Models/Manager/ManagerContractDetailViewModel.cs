namespace UI_Admin.Models.Manager;

public class ManagerContractDetailViewModel
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string? ContractTerms { get; set; }
    public int? ReplacesContractId { get; set; }

    public Guid StaffUserId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string EmployeePhone { get; set; } = string.Empty;
    public string EmployeeAddress { get; set; } = string.Empty;
    public string EmployeeIdentityNo { get; set; } = string.Empty;
    public string EmployeePosition { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; }
    public string? CreatedByManagerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? TerminationReason { get; set; }
    public string? ManagerRejectionComment { get; set; }

    public bool HasPdf { get; set; }
}
