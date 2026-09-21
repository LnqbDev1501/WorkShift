using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

/// <summary>
/// Interface defining business logic operations for regional minimum wages.
/// </summary>
public interface IRegionalMinimumWageService
{
    /// <summary>
    /// Retrieves current (un-replaced) regional minimum wage tiers for dropdown selections.
    /// </summary>
    /// <returns>A list of current regional minimum wage response DTOs.</returns>
    Task<List<RegionalMinimumWageResponse>> GetCurrentWagesAsync();

    /// <summary>
    /// Retrieves detailed information for a specific regional minimum wage ID.
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <returns>Detailed response object or null if not found.</returns>
    Task<RegionalMinimumWageDetailResponse?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new regional minimum wage tier.
    /// </summary>
    /// <param name="request">The creation payload.</param>
    /// <param name="callerId">The ID of the admin executing the request.</param>
    /// <returns>The created regional minimum wage detail response.</returns>
    Task<RegionalMinimumWageDetailResponse> CreateAsync(CreateRegionalMinimumWageRequest request, Guid callerId);

    /// <summary>
    /// Updates a regional minimum wage tier by creating a new version replacement and updating referencing regions.
    /// </summary>
    /// <param name="id">The old regional minimum wage identifier being replaced.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="callerId">The ID of the admin executing the request.</param>
    /// <returns>The newly created replacement regional minimum wage detail response.</returns>
    Task<RegionalMinimumWageDetailResponse> UpdateAsync(int id, UpdateRegionalMinimumWageRequest request, Guid callerId);
}
