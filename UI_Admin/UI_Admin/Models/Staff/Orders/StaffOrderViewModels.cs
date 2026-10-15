using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Staff.Orders;

public class StaffProductOptionViewModel
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class StaffCreateOrderViewModel
{
    [Required(ErrorMessage = "Please select at least one product.")]
    public List<StaffOrderItemViewModel> Items { get; set; } = [];

    [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
    public string? Note { get; set; }

    public List<StaffProductOptionViewModel> Products { get; set; } = [];

    public StaffCreateOrderRequest ToCreateRequest() => new()
    {
        Items = Items.Select(i => new StaffCreateOrderItemRequest
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            DiscountAmount = i.DiscountAmount,
            Note = i.Note
        }).ToList(),
        Note = Note?.Trim()
    };
}

public class StaffOrderItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TotalAmount => Price * Quantity - DiscountAmount;
    public string? Note { get; set; }
}

public class StaffCreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Note { get; set; }
}

public class StaffCreateOrderRequest
{
    public List<StaffCreateOrderItemRequest> Items { get; set; } = [];
    public string? Note { get; set; }
}

public class StaffOrderListItemViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

public class StaffOrderListViewModel
{
    public List<StaffOrderListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class StaffOrderDetailViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public List<StaffOrderDetailItemViewModel> Items { get; set; } = [];
    public List<StaffPaymentViewModel> Payments { get; set; } = [];
}

public class StaffOrderDetailItemViewModel
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

public class StaffPaymentViewModel
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Note { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

// API Response DTOs
public class StaffOrderListResponse
{
    public List<StaffOrderListItemResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }

    public StaffOrderListViewModel ToViewModel() => new()
    {
        Items = Items.Select(i => new StaffOrderListItemViewModel
        {
            OrderId = i.OrderId,
            OrderNumber = i.OrderNumber,
            OrderDate = i.OrderDate,
            Status = i.Status,
            PaymentStatus = i.PaymentStatus,
            TotalAmount = i.TotalAmount,
            ItemCount = i.ItemCount
        }).ToList(),
        PageNumber = PageNumber,
        PageSize = PageSize,
        TotalItems = TotalItems,
        TotalPages = TotalPages
    };
}

public class StaffOrderListItemResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

public class StaffOrderDetailResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public List<StaffOrderDetailItemResponse> Items { get; set; } = [];
    public List<StaffPaymentResponse> Payments { get; set; } = [];

    public StaffOrderDetailViewModel ToViewModel() => new()
    {
        OrderId = OrderId,
        OrderNumber = OrderNumber,
        OrderDate = OrderDate,
        Status = Status,
        PaymentStatus = PaymentStatus,
        PaidAt = PaidAt,
        Note = Note,
        Subtotal = Subtotal,
        TotalAmount = TotalAmount,
        Items = Items.Select(i => new StaffOrderDetailItemViewModel
        {
            OrderItemId = i.OrderItemId,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            DiscountAmount = i.DiscountAmount,
            TotalAmount = i.TotalAmount,
            Note = i.Note
        }).ToList(),
        Payments = Payments.Select(p => new StaffPaymentViewModel
        {
            PaymentId = p.PaymentId,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            ReferenceNumber = p.ReferenceNumber,
            Note = p.Note,
            RecordedByName = p.RecordedByName,
            RecordedAt = p.RecordedAt
        }).ToList()
    };
}

public class StaffOrderDetailItemResponse
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

public class StaffPaymentResponse
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Note { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

public class StaffProductListResponse
{
    public List<StaffProductOptionViewModel> Items { get; set; } = [];
}