using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

/// <summary>
/// Repository implementation for accessing regional minimum wage data.
/// </summary>
public class RegionalMinimumWageRepo : IRegionalMinimumWageRepo
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="RegionalMinimumWageRepo"/>.
    /// </summary>
    /// <param name="context">The database context instance.</param>
    public RegionalMinimumWageRepo(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<RegionalMinimumWage>> GetListAsync(string? status = null, string? search = null)
    {
        var query = _context.RegionalMinimumWages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normStatus = status.Trim().ToUpperInvariant();
            query = query.Where(w => w.Status == normStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normSearch = search.Trim();
            query = query.Where(w => EF.Functions.ILike(w.WageName, $"%{normSearch}%"));
        }

        return await query.OrderBy(w => w.RegionalMinimumWageId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWage?> GetByIdAsync(int id)
    {
        return await _context.RegionalMinimumWages
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.RegionalMinimumWageId == id);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.RegionalMinimumWages
            .AnyAsync(w => w.RegionalMinimumWageId == id);
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveAsync(int id)
    {
        return await _context.RegionalMinimumWages
            .AsNoTracking()
            .AnyAsync(w => w.RegionalMinimumWageId == id && w.Status == "ACTIVE");
    }

    /// <inheritdoc />
    public async Task<bool> HasPublishedReplacementAsync(int oldWageId)
    {
        return await _context.RegionalMinimumWages
            .AsNoTracking()
            .AnyAsync(w => w.ReplacedWageId == oldWageId && w.Status == "PUBLISH");
    }

    /// <inheritdoc />
    public async Task<List<RegionalMinimumWage>> GetDuePublishedWagesAsync(DateOnly currentDate)
    {
        return await _context.RegionalMinimumWages
            .AsNoTracking()
            .Where(w => w.Status == "PUBLISH" && w.EffectiveFrom != null && w.EffectiveFrom <= currentDate)
            .OrderBy(w => w.EffectiveFrom)
            .ThenBy(w => w.RegionalMinimumWageId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWage> CreateAsync(RegionalMinimumWage entity)
    {
        _context.RegionalMinimumWages.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWage> UpdateAsync(RegionalMinimumWage entity)
    {
        _context.RegionalMinimumWages.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateDueWageTransactionAsync(int newWageId, DateOnly currentDate)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lock the target new wage row using FOR UPDATE to prevent concurrent activation by multiple workers
                var newWageList = await _context.RegionalMinimumWages
                    .FromSqlInterpolated($"SELECT * FROM public.regional_minimum_wages WHERE regional_minimum_wage_id = {newWageId} FOR UPDATE")
                    .AsTracking()
                    .ToListAsync();

                var newWage = newWageList.FirstOrDefault();

                if (newWage == null || newWage.Status != "PUBLISH")
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                newWage.Status = "ACTIVE";

                if (newWage.ReplacedWageId.HasValue)
                {
                    var oldWageId = newWage.ReplacedWageId.Value;

                    // Lock the old wage row using FOR UPDATE
                    var oldWageList = await _context.RegionalMinimumWages
                        .FromSqlInterpolated($"SELECT * FROM public.regional_minimum_wages WHERE regional_minimum_wage_id = {oldWageId} FOR UPDATE")
                        .AsTracking()
                        .ToListAsync();

                    var oldWage = oldWageList.FirstOrDefault();

                    // Old wage must still be ACTIVE; if already set to INACTIVE or modified concurrently, abort transaction safely
                    if (oldWage == null || oldWage.Status != "ACTIVE")
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    oldWage.Status = "INACTIVE";

                    if (newWage.EffectiveFrom.HasValue)
                    {
                        oldWage.EffectiveTo = newWage.EffectiveFrom.Value.AddDays(-1);
                    }

                    var affectedRegions = await _context.Regions
                        .Where(r => r.RegionalMinimumWageId == oldWageId)
                        .ToListAsync();

                    foreach (var region in affectedRegions)
                    {
                        region.RegionalMinimumWageId = newWage.RegionalMinimumWageId;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}
