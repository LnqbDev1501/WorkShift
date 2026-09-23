namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

/// <summary>
/// Response DTO containing detailed information of a region and its associated branches.
/// </summary>
public class RegionDetailResponse
{
    /// <summary>
    /// Unique identifier of the region.
    /// </summary>
    public int RegionId { get; set; }

    /// <summary>
    /// Code of the region.
    /// </summary>
    public string RegionCode { get; set; } = null!;

    /// <summary>
    /// Display name of the region.
    /// </summary>
    public string RegionName { get; set; } = null!;

    /// <summary>
    /// Description of the region.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current status: 'ACTIVE' or 'INACTIVE'.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Minimum wage tier details for this region.
    /// </summary>
    public RegionalMinimumWageResponse? MinimumWage { get; set; }

    /// <summary>
    /// Total count of branches assigned to this region.
    /// </summary>
    public int BranchCount { get; set; }

    /// <summary>
    /// List of branches belonging to this region.
    /// </summary>
    public List<RegionBranchResponse> Branches { get; set; } = [];
}

/// <summary>
/// Summary DTO for a branch associated with a region.
/// </summary>
public class RegionBranchResponse
{
    /// <summary>
    /// Unique identifier of the branch.
    /// </summary>
    public int BranchId { get; set; }

    /// <summary>
    /// Code of the branch.
    /// </summary>
    public string BranchCode { get; set; } = null!;

    /// <summary>
    /// Display name of the branch.
    /// </summary>
    public string BranchName { get; set; } = null!;

    /// <summary>
    /// Status of the branch (e.g., 'ACTIVE', 'INACTIVE', 'CLOSED').
    /// </summary>
    public string Status { get; set; } = null!;
}
