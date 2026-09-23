namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

/// <summary>
/// Response DTO containing regional minimum wage tier list details.
/// </summary>
public class RegionalMinimumWageResponse
{
    /// <summary>
    /// Unique identifier of the regional minimum wage level.
    /// </summary>
    public int RegionalMinimumWageId { get; set; }

    /// <summary>
    /// Display name of the region wage tier (e.g., "Vùng I").
    /// </summary>
    public string WageName { get; set; } = null!;

    /// <summary>
    /// Minimum hourly rate in VND.
    /// </summary>
    public decimal MinimumHourlyRate { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current status (DRAFT, PUBLISH, ACTIVE, INACTIVE, CANCEL).
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Effective start date.
    /// </summary>
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective end date (system-managed when replaced).
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Identifier of the replaced wage level, if any.
    /// </summary>
    public int? ReplacedWageId { get; set; }
}
