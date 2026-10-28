using WorkShiftManagement.BLL.DTOs.Staff.Response;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ManagerContractChangeRequestResponse
{
    public int ChangeRequestId { get; set; }
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public Guid StaffUserId { get; set; }
    public string StaffFullName { get; set; } = null!;
    public string RequestType { get; set; } = null!;
    public string? StaffComment { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ContractStatus { get; set; }
    public DateOnly? CurrentStartDate { get; set; }
    public DateOnly? CurrentEndDate { get; set; }
    public decimal? CurrentHourlyRate { get; set; }
    public string? CurrentEmployeePosition { get; set; }
    public string? CurrentContractTerms { get; set; }
    public string? BranchName { get; set; }
    public string? ContractTemplateVersion { get; set; }
    public List<ContractChangeRequestItemDetailDto> Items { get; set; } = new();
}
