namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class StaffAccountListItemResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool HasPassword { get; set; }
}