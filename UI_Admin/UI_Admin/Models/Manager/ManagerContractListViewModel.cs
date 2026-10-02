namespace UI_Admin.Models.Manager;

public class ManagerContractListItemViewModel
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string EmployeePosition { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool HasPdf { get; set; }
}

public class ManagerContractListViewModel
{
    public IReadOnlyList<ManagerContractListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;

    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public Guid? StaffUserIdFilter { get; set; }

    public IReadOnlyList<ContractStaffOption> StaffOptions { get; set; } = [];
}
