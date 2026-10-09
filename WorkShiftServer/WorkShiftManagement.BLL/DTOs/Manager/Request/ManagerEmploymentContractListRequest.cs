namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class ManagerEmploymentContractListRequest
{
    public Guid? StaffUserId { get; set; }
    public string? Status { get; set; }
    public string? ContractNumber { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
