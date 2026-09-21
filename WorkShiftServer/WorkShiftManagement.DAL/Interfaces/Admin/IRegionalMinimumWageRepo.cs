using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

/// <summary>
/// Interface defining data access repository operations for regional minimum wages.
/// </summary>
public interface IRegionalMinimumWageRepo
{
    /// <summary>
    /// Retrieves current (un-replaced) regional minimum wage records.
    /// </summary>
    /// <returns>A list of current regional minimum wage entities.</returns>
    Task<List<RegionalMinimumWage>> GetCurrentWagesAsync();

    /// <summary>
    /// Retrieves all regional minimum wage records including historical replaced versions.
    /// </summary>
    /// <returns>A list of all regional minimum wage entities.</returns>
    Task<List<RegionalMinimumWage>> GetAllAsync();

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
    /// Checks whether a regional minimum wage record has already been replaced by another record.
    /// </summary>
    /// <param name="id">The regional minimum wage identifier to check.</param>
    /// <returns>True if already replaced; otherwise, false.</returns>
    Task<bool> IsAlreadyReplacedAsync(int id);

    /// <summary>
    /// Checks whether a current (un-replaced) regional minimum wage with the given name exists (case-insensitive).
    /// </summary>
    /// <param name="wageName">The wage display name.</param>
    /// <param name="excludeId">Optional ID to exclude from duplicate search.</param>
    /// <returns>True if duplicate current wage name exists; otherwise, false.</returns>
    Task<bool> CurrentNameExistsAsync(string wageName, int? excludeId = null);

    /// <summary>
    /// Inserts a new regional minimum wage record into the database.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <returns>The newly created entity.</returns>
    Task<RegionalMinimumWage> CreateAsync(RegionalMinimumWage entity);

    /// <summary>
    /// Performs a transactional replacement of an existing wage with a new version, updating all referencing regions.
    /// </summary>
    /// <param name="oldWageId">The ID of the old wage being replaced.</param>
    /// <param name="newWageEntity">The new wage entity to insert.</param>
    /// <returns>The newly inserted replacement wage entity.</returns>
    Task<RegionalMinimumWage> ReplaceWageAsync(int oldWageId, RegionalMinimumWage newWageEntity);
}
