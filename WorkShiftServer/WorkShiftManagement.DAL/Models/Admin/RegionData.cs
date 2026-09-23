namespace WorkShiftManagement.DAL.Models.Admin;

/// <summary>
/// Internal DAL data model representing region summary data.
/// </summary>
public sealed class RegionData
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
    /// Active status flag from database table.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Count of branches projected from database.
    /// </summary>
    public int BranchCount { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
