namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class OrderItemResponse
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
}

public class OrderResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];
}

public class OrderDetailResponse : OrderResponse
{
    public DateTime UpdatedAt { get; set; }
    public List<PaymentResponse> Payments { get; set; } = [];
}

public class PaymentResponse
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Note { get; set; }
    public Guid RecordedBy { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}