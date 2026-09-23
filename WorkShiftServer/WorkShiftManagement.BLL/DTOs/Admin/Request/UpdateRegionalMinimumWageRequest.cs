namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

/// <summary>
/// Request payload DTO for updating a DRAFT regional minimum wage.
/// </summary>
public class UpdateRegionalMinimumWageRequest
{
    /// <summary>
    /// Display name of the wage level (e.g., "Vùng I").
    /// </summary>
    public string WageName { get; set; } = null!;

    /// <summary>
    /// Minimum hourly rate in VND. Must be greater than 0.
    /// </summary>
    public decimal MinimumHourlyRate { get; set; }

    /// <summary>
    /// Optional description for the wage tier.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional identifier of the active wage being replaced.
    /// </summary>
    public int? ReplacedWageId { get; set; }

    /// <summary>
    /// Effective start date.
    /// </summary>
    public DateOnly? EffectiveFrom { get; set; }
}
