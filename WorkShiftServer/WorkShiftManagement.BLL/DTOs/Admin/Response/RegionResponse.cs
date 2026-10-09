namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

/// <summary>
/// Response DTO containing summary information of a region.
/// </summary>
public class RegionResponse
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
    /// Identifier of the associated regional minimum wage.
    /// </summary>
    public int? RegionalMinimumWageId { get; set; }

    /// <summary>
    /// Display name of the minimum wage level (e.g. "Vùng II").
    /// </summary>
    public string? WageName { get; set; }

    /// <summary>
    /// Minimum hourly rate in VND.
    /// </summary>
    public decimal? MinimumHourlyRate { get; set; }

    /// <summary>
    /// Total count of branches assigned to this region.
    /// </summary>
    public int BranchCount { get; set; }

    /// <summary>
    /// Current status: 'ACTIVE' or 'INACTIVE'.
    /// </summary>
    public string Status { get; set; } = null!;
}
