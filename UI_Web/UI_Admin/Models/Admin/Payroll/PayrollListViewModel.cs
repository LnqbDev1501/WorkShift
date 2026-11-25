using System.ComponentModel.DataAnnotations;
using UI_Admin.Models.Regions;

namespace UI_Admin.Models.Admin.Payroll;

public class PayrollFilter
{
    [StringLength(100)] public string? Search { get; set; }
    [RegularExpression("^(DRAFT|OPEN|CALCULATED|SUBMITTED|APPROVED|REVISION_REQUIRED|PAID|CLOSED)$")]
    public string? Status { get; set; }
    public DateOnly? StartDateFrom { get; set; }
    public DateOnly? StartDateTo { get; set; }
    [Range(1, int.MaxValue)] public int? RegionId { get; set; }
    [Range(1, int.MaxValue)] public int? BranchId { get; set; }
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;
}

public class PayrollListViewModel
{
    public PayrollFilter Filter { get; set; } = new();
    public PayrollPeriodListResponse Page { get; set; } = new();
    public Dictionary<string, int> Counts { get; set; } = new();
    public List<RegionListItemViewModel> Regions { get; set; } = [];
    public List<PayrollBranch> Branches { get; set; } = [];
    public string? Error { get; set; }
    public string? LookupError { get; set; }
}

public class PayrollBranch
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
}
