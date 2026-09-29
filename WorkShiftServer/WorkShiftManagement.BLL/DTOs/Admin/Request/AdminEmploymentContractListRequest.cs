using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class AdminEmploymentContractListRequest
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
    public int? BranchId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RegionId must be greater than 0.")]
    public int? RegionId { get; set; }

    public DateOnly? StartDateFrom { get; set; }

    public DateOnly? StartDateTo { get; set; }

    public DateOnly? EndDateFrom { get; set; }

    public DateOnly? EndDateTo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;
}
