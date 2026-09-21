namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

/// <summary>
/// Response DTO containing comprehensive regional minimum wage details.
/// </summary>
public class RegionalMinimumWageDetailResponse
{
    /// <summary>
    /// Unique identifier of the regional minimum wage.
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
    /// Optional description for the wage tier.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Identifier of the wage version replaced by this record, if any.
    /// </summary>
    public int? ReplacedWageId { get; set; }

    /// <summary>
    /// Identifier of the admin user who created this record.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
