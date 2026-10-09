using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

/// <summary>
/// Interface defining business logic operations for regional minimum wages.
/// </summary>
public interface IRegionalMinimumWageService
{
    /// <summary>
    /// Retrieves regional minimum wage tiers with optional status and search filters.
    /// </summary>
    /// <param name="status">Optional status filter (e.g., "ACTIVE", "DRAFT", "PUBLISH").</param>
    /// <param name="search">Optional search term for WageName.</param>
    /// <returns>A list of regional minimum wage response DTOs.</returns>
    Task<List<RegionalMinimumWageResponse>> GetListAsync(string? status = null, string? search = null);

    /// <summary>
    /// Retrieves current ACTIVE regional minimum wage tiers for dropdown selections.
    /// </summary>
    /// <returns>A list of active regional minimum wage response DTOs.</returns>
    Task<List<RegionalMinimumWageResponse>> GetCurrentWagesAsync();

    /// <summary>
    /// Retrieves detailed information for a specific regional minimum wage ID.
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <returns>Detailed response object or null if not found.</returns>
    Task<RegionalMinimumWageDetailResponse?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new regional minimum wage tier in DRAFT status.
    /// </summary>
    /// <param name="request">The creation payload.</param>
    /// <param name="callerId">The ID of the admin executing the request.</param>
    /// <returns>The created regional minimum wage detail response.</returns>
    Task<RegionalMinimumWageDetailResponse> CreateAsync(CreateRegionalMinimumWageRequest request, Guid callerId);

    /// <summary>
    /// Updates an existing DRAFT regional minimum wage tier directly in place.
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="callerId">The ID of the admin executing the request.</param>
    /// <returns>The updated regional minimum wage detail response.</returns>
    Task<RegionalMinimumWageDetailResponse> UpdateDraftAsync(int id, UpdateRegionalMinimumWageRequest request, Guid callerId);

    /// <summary>
    /// Publishes a DRAFT regional minimum wage tier (DRAFT -> PUBLISH).
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <param name="callerId">The ID of the admin executing the request.</param>
    /// <returns>The published regional minimum wage detail response.</returns>
    Task<RegionalMinimumWageDetailResponse> PublishAsync(int id, Guid callerId);

    /// <summary>
    /// Cancels a DRAFT or PUBLISH regional minimum wage tier (DRAFT/PUBLISH -> CANCEL).
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <param name="callerId">The ID of the admin executing the request.</param>
    /// <returns>The cancelled regional minimum wage detail response.</returns>
    Task<RegionalMinimumWageDetailResponse> CancelAsync(int id, Guid callerId);

    /// <summary>
    /// Automatically activates all PUBLISH regional minimum wage tiers whose EffectiveFrom date is due.
    /// </summary>
    /// <returns>The count of activated wage tiers.</returns>
    Task<int> ActivateDueWagesAsync();
}
