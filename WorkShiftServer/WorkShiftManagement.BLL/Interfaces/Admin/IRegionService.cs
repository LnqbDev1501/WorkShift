using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

/// <summary>
/// Interface defining business logic operations for region management.
/// </summary>
public interface IRegionService
{
    /// <summary>
    /// Retrieves all regions.
    /// </summary>
    /// <returns>A list of region response DTOs.</returns>
    Task<List<RegionResponse>> GetAllAsync();

    /// <summary>
    /// Retrieves a paginated list of regions matching the provided query parameters.
    /// </summary>
    /// <param name="query">The query filters including search term, status, page number, and page size.</param>
    /// <returns>A paginated response containing matching regions.</returns>
    Task<PagedResponse<RegionResponse>> GetPagedAsync(RegionListQuery query);

    /// <summary>
    /// Retrieves detailed region information by region ID.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <returns>The region detail DTO or null if not found.</returns>
    Task<RegionDetailResponse?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new region after performing business validation and duplicate checks.
    /// </summary>
    /// <param name="request">The region creation payload.</param>
    /// <returns>The newly created region response DTO.</returns>
    Task<RegionResponse> CreateAsync(CreateRegionRequest request);

    /// <summary>
    /// Updates the name and description of an existing region.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <param name="request">The update payload.</param>
    /// <returns>The updated region response DTO or null if not found.</returns>
    Task<RegionResponse?> UpdateAsync(int id, UpdateRegionRequest request);

    /// <summary>
    /// Soft deletes (deactivates) a region by changing its status to INACTIVE.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <returns>True if successfully deactivated; otherwise, false.</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Restores an inactive region by changing its status back to ACTIVE.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <returns>True if successfully restored; otherwise, false.</returns>
    Task<bool> RestoreAsync(int id);
}
