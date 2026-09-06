namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class BranchEmployeeResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
}
