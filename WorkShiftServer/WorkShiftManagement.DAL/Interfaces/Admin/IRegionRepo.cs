using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

/// <summary>
/// Interface defining data access repository operations for regions.
/// </summary>
public interface IRegionRepo
{
    /// <summary>
    /// Retrieves all regions from the database.
    /// </summary>
    /// <returns>A list of region entity objects.</returns>
    Task<List<Region>> GetAllAsync();

    /// <summary>
    /// Retrieves a paginated list of region data models with search and active status filters.
    /// </summary>
    /// <param name="search">Optional search term matching region code or region name.</param>
    /// <param name="isActive">Optional boolean flag filtering active or inactive regions.</param>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <returns>A tuple containing the list of region items and total count.</returns>
    Task<(List<RegionData> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// Retrieves detailed region data by ID including associated branches.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <returns>The region detail data or null if not found.</returns>
    Task<RegionDetailData?> GetByIdAsync(int id);

    /// <summary>
    /// Checks whether a region with the specified region code already exists (case-insensitive).
    /// </summary>
    /// <param name="regionCode">The region code to check.</param>
    /// <returns>True if a matching region exists; otherwise, false.</returns>
    Task<bool> ExistsByCodeAsync(string regionCode);

    /// <summary>
    /// Checks whether a region with the specified region name already exists (case-insensitive).
    /// </summary>
    /// <param name="regionName">The region name to check.</param>
    /// <returns>True if a matching region exists; otherwise, false.</returns>
    Task<bool> ExistsByNameAsync(string regionName);

    /// <summary>
    /// Checks whether a region with the specified region name exists excluding a specific region ID.
    /// </summary>
    /// <param name="regionName">The region name to check.</param>
    /// <param name="excludeRegionId">The region ID to exclude from the search.</param>
    /// <returns>True if a duplicate name is found; otherwise, false.</returns>
    Task<bool> ExistsByNameExceptIdAsync(string regionName, int excludeRegionId);

    /// <summary>
    /// Inserts a new region record into the database.
    /// </summary>
    /// <param name="region">The region entity to create.</param>
    /// <returns>The created region data model.</returns>
    Task<RegionData> CreateAsync(Region region);

    /// <summary>
    /// Updates the region name, description, and regional minimum wage ID for a specific region ID.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <param name="regionName">The updated region name.</param>
    /// <param name="description">The updated description.</param>
    /// <param name="regionalMinimumWageId">The updated regional minimum wage ID.</param>
    /// <returns>The updated region data model or null if not found.</returns>
    Task<RegionData?> UpdateAsync(int id, string regionName, string? description, int regionalMinimumWageId);

    /// <summary>
    /// Soft deletes a region by setting its IsActive status to false.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <returns>True if successfully updated; otherwise, false.</returns>
    Task<bool> SoftDeleteAsync(int id);

    /// <summary>
    /// Restores a soft-deleted region by setting its IsActive status to true.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <returns>True if successfully updated; otherwise, false.</returns>
    Task<bool> RestoreAsync(int id);
}
