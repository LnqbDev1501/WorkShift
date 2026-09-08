namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class UpdateAdminUserResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentityNo { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
