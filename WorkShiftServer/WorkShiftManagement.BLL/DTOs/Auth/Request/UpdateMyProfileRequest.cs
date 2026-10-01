namespace WorkShiftManagement.BLL.DTOs.Auth.Request;

public class UpdateMyProfileRequest
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string Address { get; set; } = null!;
}
