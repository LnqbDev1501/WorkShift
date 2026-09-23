using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

/// <summary>
/// Data access repository implementation for region management using Entity Framework Core.
/// </summary>
public class RegionRepo : IRegionRepo
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="RegionRepo"/>.
    /// </summary>
    /// <param name="context">The database context instance.</param>
    public RegionRepo(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<Region>> GetAllAsync()
    {
        return await _context.Regions
            .Include(r => r.RegionalMinimumWage)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(List<RegionData> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Regions.AsNoTracking();

        // Apply case-insensitive search filter on RegionCode or RegionName using ILIKE
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(region =>
                EF.Functions.ILike(region.RegionCode, pattern) ||
                EF.Functions.ILike(region.RegionName, pattern));
        }

        // Apply active status filter if specified
        if (isActive.HasValue)
        {
            query = query.Where(region => region.IsActive == isActive.Value);
        }

        // Count total matching records before pagination
        var totalCount = await query.CountAsync();

        // Project directly to RegionData including regional minimum wage and count associated branches in SQL
        var items = await query
            .OrderBy(region => region.RegionId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(region => new RegionData
            {
                RegionId = region.RegionId,
                RegionCode = region.RegionCode,
                RegionName = region.RegionName,
                Description = region.Description,
                RegionalMinimumWageId = region.RegionalMinimumWageId,
                WageName = region.RegionalMinimumWage != null ? region.RegionalMinimumWage.WageName : null,
                MinimumHourlyRate = region.RegionalMinimumWage != null ? region.RegionalMinimumWage.MinimumHourlyRate : (decimal?)null,
                IsActive = region.IsActive,
                BranchCount = region.Branches.Count(),
                CreatedAt = region.CreatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<RegionDetailData?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        // Retrieve region details with minimum wage and branches sorted by BranchCode
        return await _context.Regions
            .AsNoTracking()
            .Where(region => region.RegionId == id)
            .Select(region => new RegionDetailData
            {
                RegionId = region.RegionId,
                RegionCode = region.RegionCode,
                RegionName = region.RegionName,
                Description = region.Description,
                RegionalMinimumWageId = region.RegionalMinimumWageId,
                WageName = region.RegionalMinimumWage != null ? region.RegionalMinimumWage.WageName : null,
                MinimumHourlyRate = region.RegionalMinimumWage != null ? region.RegionalMinimumWage.MinimumHourlyRate : (decimal?)null,
                IsActive = region.IsActive,
                Branches = region.Branches
                    .OrderBy(branch => branch.BranchCode)
                    .Select(branch => new RegionBranchData
                    {
                        BranchId = branch.BranchId,
                        BranchCode = branch.BranchCode,
                        BranchName = branch.BranchName,
                        Status = branch.Status
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByCodeAsync(string regionCode)
    {
        if (string.IsNullOrWhiteSpace(regionCode))
        {
            return false;
        }

        var pattern = regionCode.Trim();
        return await _context.Regions
            .AsNoTracking()
            .AnyAsync(region => EF.Functions.ILike(region.RegionCode, pattern));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string regionName)
    {
        if (string.IsNullOrWhiteSpace(regionName))
        {
            return false;
        }

        var pattern = regionName.Trim();
        return await _context.Regions
            .AsNoTracking()
            .AnyAsync(region => EF.Functions.ILike(region.RegionName, pattern));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameExceptIdAsync(string regionName, int excludeRegionId)
    {
        if (string.IsNullOrWhiteSpace(regionName))
        {
            return false;
        }

        var pattern = regionName.Trim();
        return await _context.Regions
            .AsNoTracking()
            .AnyAsync(region => region.RegionId != excludeRegionId &&
                                EF.Functions.ILike(region.RegionName, pattern));
    }

    /// <inheritdoc />
    public async Task<RegionData> CreateAsync(Region region)
    {
        _context.Regions.Add(region);
        await _context.SaveChangesAsync();

        var wage = region.RegionalMinimumWageId.HasValue
            ? await _context.RegionalMinimumWages.AsNoTracking().FirstOrDefaultAsync(w => w.RegionalMinimumWageId == region.RegionalMinimumWageId.Value)
            : null;

        return new RegionData
        {
            RegionId = region.RegionId,
            RegionCode = region.RegionCode,
            RegionName = region.RegionName,
            Description = region.Description,
            RegionalMinimumWageId = region.RegionalMinimumWageId,
            WageName = wage?.WageName,
            MinimumHourlyRate = wage?.MinimumHourlyRate,
            IsActive = region.IsActive,
            BranchCount = 0,
            CreatedAt = region.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<RegionData?> UpdateAsync(int id, string regionName, string? description, int regionalMinimumWageId)
    {
        var region = await _context.Regions.FirstOrDefaultAsync(r => r.RegionId == id);
        if (region == null)
        {
            return null;
        }

        region.RegionName = regionName;
        region.Description = description;
        region.RegionalMinimumWageId = regionalMinimumWageId;

        await _context.SaveChangesAsync();

        var wage = await _context.RegionalMinimumWages.AsNoTracking().FirstOrDefaultAsync(w => w.RegionalMinimumWageId == regionalMinimumWageId);
        var branchCount = await _context.Branches.CountAsync(b => b.RegionId == id);

        return new RegionData
        {
            RegionId = region.RegionId,
            RegionCode = region.RegionCode,
            RegionName = region.RegionName,
            Description = region.Description,
            RegionalMinimumWageId = region.RegionalMinimumWageId,
            WageName = wage?.WageName,
            MinimumHourlyRate = wage?.MinimumHourlyRate,
            IsActive = region.IsActive,
            BranchCount = branchCount,
            CreatedAt = region.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(int id)
    {
        var region = await _context.Regions.FirstOrDefaultAsync(r => r.RegionId == id);
        if (region == null)
        {
            return false;
        }

        if (!region.IsActive)
        {
            return false;
        }

        region.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RestoreAsync(int id)
    {
        var region = await _context.Regions.FirstOrDefaultAsync(r => r.RegionId == id);
        if (region == null)
        {
            return false;
        }

        if (region.IsActive)
        {
            return false;
        }

        region.IsActive = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
