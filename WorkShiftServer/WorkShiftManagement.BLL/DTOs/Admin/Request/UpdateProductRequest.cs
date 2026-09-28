using Microsoft.AspNetCore.Http;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class UpdateProductRequest
{
    public int CategoryId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
}
