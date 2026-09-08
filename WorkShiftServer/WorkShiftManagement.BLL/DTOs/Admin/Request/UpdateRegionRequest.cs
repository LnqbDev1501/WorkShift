namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

/// <summary>
/// Request payload DTO for updating an existing region's details.
/// </summary>
public class UpdateRegionRequest
{
    /// <summary>
    /// Updated region display name. Max length: 150.
    /// </summary>
    public string RegionName { get; set; } = null!;

    /// <summary>
    /// Updated description for the region. Max length: 500.
    /// </summary>
    public string? Description { get; set; }
}
