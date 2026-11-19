namespace UI_Admin.Models.Accountant;

public class AccountantOrderListItemViewModel
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

public class AccountantOrderDetailViewModel
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
    public List<AccountantOrderItemViewModel> Items { get; set; } = [];
    public List<AccountantPaymentViewModel> Payments { get; set; } = [];
}

public class AccountantOrderItemViewModel
{
    public int OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class AccountantPaymentViewModel
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime RecordedAt { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
}

public class AccountantOrderListViewModel
{
    public List<AccountantOrderListItemViewModel> Items { get; set; } = [];
    public List<BranchOptionViewModel> Branches { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
    public string? Search { get; set; }
    public int? BranchId { get; set; }
    public string? StatusFilter { get; set; }
    public string? PaymentStatusFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? PaidCount { get; set; }
    public int? CompletedCount { get; set; }
    public decimal? TotalOrderValue { get; set; }
}

public class BranchOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BranchSalesReportViewModel
{
    public List<BranchOptionViewModel> Branches { get; set; } = [];
    public int? SelectedBranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class BranchSalesSummaryModel
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BranchSalesAnalyticsModel
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CompletedOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double RevenueGrowthPercent { get; set; }
    public double OrdersGrowthPercent { get; set; }
    public List<BranchSalesTimelinePointModel> Timeline { get; set; } = [];
    public List<BranchSalesCategoryPointModel> Categories { get; set; } = [];
}

public class BranchSalesTimelinePointModel
{
    public string DateLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
}

public class BranchSalesCategoryPointModel
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Quantity { get; set; }
}