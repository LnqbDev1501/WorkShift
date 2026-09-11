using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Payroll;

public class PayrollFilter
{
    [StringLength(100)] public string? Search { get; set; }
    [RegularExpression("^(SUBMITTED|APPROVED|REVISION_REQUIRED)$")]
    public string? Status { get; set; }
    [Range(1, int.MaxValue)] public int? RegionId { get; set; }
    [Range(1, int.MaxValue)] public int? BranchId { get; set; }
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
}

public class PayrollListViewModel
{
    public PayrollFilter Filter { get; set; } = new();
    public PayrollPeriodListResponse Page { get; set; } = new();
    public Dictionary<string, int> Counts { get; set; } = new();
    public List<RegionViewModel> Regions { get; set; } = [];
    public List<PayrollBranch> Branches { get; set; } = [];
    public string? Error { get; set; }
    public string? LookupError { get; set; }
}

public class PayrollBranch
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
}
