namespace UI_Admin.Models.Manager;

public class ManagerOrderListItemViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class ManagerOrderDetailViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<ManagerOrderItemViewModel> Items { get; set; } = [];
    public List<ManagerPaymentViewModel> Payments { get; set; } = [];
}

public class ManagerOrderItemViewModel
{
    public int OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ManagerPaymentViewModel
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime RecordedAt { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
}

public class ManagerOrderListViewModel
{
    public List<ManagerOrderListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public string? PaymentStatusFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? PaidCount { get; set; }
    public int? CompletedCount { get; set; }
    public decimal? TotalOrderValue { get; set; }
}