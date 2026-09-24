namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class EmployeeTypeResponse
{
    public int EmployeeTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
