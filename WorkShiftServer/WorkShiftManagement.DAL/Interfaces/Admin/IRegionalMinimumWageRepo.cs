using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

/// <summary>
/// Interface defining data access repository operations for regional minimum wages.
/// </summary>
public interface IRegionalMinimumWageRepo
{
    /// <summary>
    /// Retrieves regional minimum wage records with optional status and search filtering.
    /// </summary>
    /// <param name="status">Optional status filter (e.g., "ACTIVE", "DRAFT", "PUBLISH").</param>
    /// <param name="search">Optional search term for WageName.</param>
    /// <returns>A list of matching regional minimum wage entities.</returns>
    Task<List<RegionalMinimumWage>> GetListAsync(string? status = null, string? search = null);

    /// <summary>
    /// Retrieves a regional minimum wage record by its unique ID.
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <returns>The regional minimum wage entity or null if not found.</returns>
    Task<RegionalMinimumWage?> GetByIdAsync(int id);

    /// <summary>
    /// Checks whether a regional minimum wage record exists with the specified ID.
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <returns>True if exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Checks whether a regional minimum wage record exists and has status = 'ACTIVE'.
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <returns>True if active; otherwise, false.</returns>
    Task<bool> IsActiveAsync(int id);

    /// <summary>
    /// Checks whether there is another PUBLISH wage already scheduled to replace the given active wage.
    /// </summary>
    /// <param name="oldWageId">The active wage ID being replaced.</param>
    /// <returns>True if a pending PUBLISH replacement exists; otherwise, false.</returns>
    Task<bool> HasPublishedReplacementAsync(int oldWageId);

    /// <summary>
    /// Retrieves all PUBLISH wages whose EffectiveFrom date is on or before currentDate.
    /// </summary>
    /// <param name="currentDate">Current business date.</param>
    /// <returns>A list of due published wage entities.</returns>
    Task<List<RegionalMinimumWage>> GetDuePublishedWagesAsync(DateOnly currentDate);

    /// <summary>
    /// Inserts a new regional minimum wage record into the database.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <returns>The newly created entity.</returns>
    Task<RegionalMinimumWage> CreateAsync(RegionalMinimumWage entity);

    /// <summary>
    /// Updates an existing regional minimum wage record in place.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The updated entity.</returns>
    Task<RegionalMinimumWage> UpdateAsync(RegionalMinimumWage entity);

    /// <summary>
    /// Transactionally activates a due published wage, deactivates replaced wage, and updates region references.
    /// </summary>
    /// <param name="newWageId">The ID of the published wage to activate.</param>
    /// <param name="currentDate">Current business date.</param>
    /// <returns>True if successfully activated; false if already processed or invalid state.</returns>
    Task<bool> ActivateDueWageTransactionAsync(int newWageId, DateOnly currentDate);
}
