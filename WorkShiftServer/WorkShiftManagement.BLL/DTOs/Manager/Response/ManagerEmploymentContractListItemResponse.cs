using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ManagerEmploymentContractListItemResponse
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public Guid StaffUserId { get; set; }
    public string EmployeeFullName { get; set; } = null!;
    public string EmployeePosition { get; set; } = null!;
    public string? EmployeePhone { get; set; }
    public string? EmployeeIdentityNo { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string Status { get; set; } = null!;
    public string? TerminationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool HasPdf { get; set; }
}
