using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class UpdateOrderRequest
{
    [Required]
    public List<UpdateOrderItemRequest> Items { get; set; } = [];

    public string? Note { get; set; }
}

public class UpdateOrderItemRequest
{
    public int? OrderItemId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Discount amount cannot be negative.")]
    public decimal DiscountAmount { get; set; } = 0;

    public string? Note { get; set; }
}
