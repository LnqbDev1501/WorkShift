namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public sealed class BranchAssignableOptionResponse
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public int ManagerCount { get; set; }
    public bool HasManager => ManagerCount >= 1;
    public int AccountantCount { get; set; }
    public bool HasAccountant => AccountantCount >= 1;
}
