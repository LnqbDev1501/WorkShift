namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

/// <summary>
/// Request payload DTO for updating an existing regional minimum wage (version replacement).
/// </summary>
public class UpdateRegionalMinimumWageRequest
{
    /// <summary>
    /// Updated display name of the wage level.
    /// </summary>
    public string WageName { get; set; } = null!;

    /// <summary>
    /// Updated minimum hourly rate in VND. Must be greater than 0.
    /// </summary>
    public decimal MinimumHourlyRate { get; set; }

    /// <summary>
    /// Optional updated description for the wage tier.
    /// </summary>
    public string? Description { get; set; }
}
