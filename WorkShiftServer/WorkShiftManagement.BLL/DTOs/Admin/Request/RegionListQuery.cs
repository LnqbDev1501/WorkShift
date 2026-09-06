namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

/// <summary>
/// Query parameters DTO for filtering and paginating regions.
/// </summary>
public class RegionListQuery
{
    /// <summary>
    /// Optional search keyword matching region code or region name.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Optional status filter: 'ACTIVE' or 'INACTIVE'.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 1-based page number for pagination. Default is 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of records per page. Default is 10.
    /// </summary>
    public int PageSize { get; set; } = 10;
}
