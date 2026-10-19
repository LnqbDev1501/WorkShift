namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public class StaffContractListItemResponse
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string EmployeeFullName { get; set; } = null!;
    public string EmployeePosition { get; set; } = null!;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentToStaffAt { get; set; }
    public DateTime? StaffAcceptedAt { get; set; }
    public bool HasPdf { get; set; }
}
