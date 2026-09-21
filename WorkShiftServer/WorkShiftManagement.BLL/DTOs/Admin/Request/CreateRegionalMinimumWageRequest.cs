namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

/// <summary>
/// Request payload DTO for creating a new regional minimum wage tier.
/// </summary>
public class CreateRegionalMinimumWageRequest
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
}
