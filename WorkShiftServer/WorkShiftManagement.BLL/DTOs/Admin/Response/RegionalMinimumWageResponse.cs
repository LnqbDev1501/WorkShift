namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

/// <summary>
/// Response DTO containing regional minimum wage tier details.
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
}
