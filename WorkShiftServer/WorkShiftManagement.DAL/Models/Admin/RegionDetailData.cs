namespace WorkShiftManagement.DAL.Models.Admin;

/// <summary>
/// Internal DAL data model representing detailed region data with its branches.
/// </summary>
public sealed class RegionDetailData
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
    /// Optional description of the region.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Active status flag from database table.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// List of branches associated with this region.
    /// </summary>
    public List<RegionBranchData> Branches { get; set; } = [];
}

/// <summary>
/// Internal DAL data model representing branch information assigned to a region.
/// </summary>
public sealed class RegionBranchData
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
    /// Status string of the branch.
    /// </summary>
    public string Status { get; set; } = null!;
}
