namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class CreateAdminUserResponse
{
    public Guid UserId { get; set; }
    public int RoleId { get; set; }
    public int BranchId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdentityNo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
