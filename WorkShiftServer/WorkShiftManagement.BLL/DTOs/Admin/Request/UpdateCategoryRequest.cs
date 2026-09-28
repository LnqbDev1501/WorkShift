namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class UpdateCategoryRequest
{
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
}
