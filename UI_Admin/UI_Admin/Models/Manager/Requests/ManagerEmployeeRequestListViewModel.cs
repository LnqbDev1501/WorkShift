namespace UI_Admin.Models.Manager.Requests;

public class ManagerEmployeeRequestListViewModel
{
    public IReadOnlyList<ManagerEmployeeRequestListItemViewModel> Items { get; set; } = new List<ManagerEmployeeRequestListItemViewModel>();

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    // Filters
    public string? Search { get; set; }
    public string? RequestType { get; set; }
    public string? Status { get; set; }
    public Guid? UserId { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }

    // Dropdown options
    public IReadOnlyList<ManagerStaffFilterOptionViewModel> StaffOptions { get; set; } = new List<ManagerStaffFilterOptionViewModel>();

    // Messages
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        (!string.IsNullOrWhiteSpace(RequestType) && !RequestType.Equals("ALL", StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrWhiteSpace(Status) && !Status.Equals("ALL", StringComparison.OrdinalIgnoreCase)) ||
        UserId.HasValue ||
        !string.IsNullOrWhiteSpace(FromDate) ||
        !string.IsNullOrWhiteSpace(ToDate);
}

public class ManagerStaffFilterOptionViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
