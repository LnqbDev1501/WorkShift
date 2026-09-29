using Microsoft.AspNetCore.Http;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class CreateProductRequest
{
    public int CategoryId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
}
