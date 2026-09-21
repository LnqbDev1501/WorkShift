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
    public async Task<List<RegionalMinimumWage>> GetCurrentWagesAsync()
    {
        return await _context.RegionalMinimumWages
            .AsNoTracking()
            .Where(w => !_context.RegionalMinimumWages.Any(r => r.ReplacedWageId == w.RegionalMinimumWageId))
            .OrderBy(w => w.RegionalMinimumWageId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<RegionalMinimumWage>> GetAllAsync()
    {
        return await _context.RegionalMinimumWages
            .AsNoTracking()
            .OrderBy(w => w.RegionalMinimumWageId)
            .ToListAsync();
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
    public async Task<bool> IsAlreadyReplacedAsync(int id)
    {
        return await _context.RegionalMinimumWages
            .AsNoTracking()
            .AnyAsync(r => r.ReplacedWageId == id);
    }

    /// <inheritdoc />
    public async Task<bool> CurrentNameExistsAsync(string wageName, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(wageName))
        {
            return false;
        }

        var pattern = wageName.Trim();
        var query = _context.RegionalMinimumWages
            .AsNoTracking()
            .Where(w => !_context.RegionalMinimumWages.Any(r => r.ReplacedWageId == w.RegionalMinimumWageId))
            .Where(w => EF.Functions.ILike(w.WageName, pattern));

        if (excludeId.HasValue)
        {
            query = query.Where(w => w.RegionalMinimumWageId != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWage> CreateAsync(RegionalMinimumWage entity)
    {
        _context.RegionalMinimumWages.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWage> ReplaceWageAsync(int oldWageId, RegionalMinimumWage newWageEntity)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Save new replacement wage record
                _context.RegionalMinimumWages.Add(newWageEntity);
                await _context.SaveChangesAsync();

                // 2. Update all regions pointing to oldWageId to point to newWageEntity.RegionalMinimumWageId
                var affectedRegions = await _context.Regions
                    .Where(r => r.RegionalMinimumWageId == oldWageId)
                    .ToListAsync();

                foreach (var region in affectedRegions)
                {
                    region.RegionalMinimumWageId = newWageEntity.RegionalMinimumWageId;
                }

                await _context.SaveChangesAsync();

                // 3. Commit transaction
                await transaction.CommitAsync();

                return newWageEntity;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}
