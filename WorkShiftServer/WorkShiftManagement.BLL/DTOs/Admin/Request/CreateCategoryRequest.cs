namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class CreateCategoryRequest
{
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
}
