namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

/// <summary>
/// Request payload DTO for creating a new region.
/// </summary>
public class CreateRegionRequest
{
    /// <summary>
    /// Unique code for the region (e.g. 'HCM', 'HN'). Max length: 20.
    /// </summary>
    public string RegionCode { get; set; } = null!;

    /// <summary>
    /// Display name of the region. Max length: 150.
    /// </summary>
    public string RegionName { get; set; } = null!;

    /// <summary>
    /// Optional description for the region. Max length: 500.
    /// </summary>
    public string? Description { get; set; }
}
