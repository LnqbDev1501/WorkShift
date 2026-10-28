namespace UI_Admin.Models.EmployeeRequests;

public class AdminEmployeeRequestListViewModel
{
    public List<AdminEmployeeRequestListItemViewModel> Items { get; set; } = [];

    public List<BranchFilterOptionViewModel> Branches { get; set; } = [];

    // Filter properties
    public string? Search { get; set; }

    public string? RequestType { get; set; }

    public string? Status { get; set; }

    public int? BranchId { get; set; }

    public Guid? UserId { get; set; }

    public string? FromDate { get; set; }

    public string? ToDate { get; set; }

    // Pagination properties
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    // Status message / Error message
    public string? ErrorMessage { get; set; }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        (!string.IsNullOrWhiteSpace(RequestType) && !RequestType.Equals("ALL", StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrWhiteSpace(Status) && !Status.Equals("ALL", StringComparison.OrdinalIgnoreCase)) ||
        BranchId.HasValue ||
        UserId.HasValue ||
        !string.IsNullOrWhiteSpace(FromDate) ||
        !string.IsNullOrWhiteSpace(ToDate);
}
