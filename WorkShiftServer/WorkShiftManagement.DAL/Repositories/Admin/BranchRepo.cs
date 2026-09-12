using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class BranchRepo : IBranchRepo
{
    private readonly AppDbContext _context;

    public BranchRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<BranchData> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int? regionId,
        string? status,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Branches.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(branch =>
                EF.Functions.ILike(branch.BranchCode, pattern) ||
                EF.Functions.ILike(branch.BranchName, pattern) ||
                EF.Functions.ILike(branch.Address, pattern));
        }

        if (regionId.HasValue)
        {
            query = query.Where(branch => branch.RegionId == regionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(branch => branch.Status.ToUpper() == normalizedStatus);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(branch => branch.BranchId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(branch => new BranchData
            {
                BranchId = branch.BranchId,
                BranchCode = branch.BranchCode,
                BranchName = branch.BranchName,
                RegionId = branch.RegionId,
                RegionCode = branch.Region.RegionCode,
                RegionName = branch.Region.RegionName,
                Address = branch.Address,
                EmployeeCount = branch.UserBranches.Count(u => u.BranchId == branch.BranchId),
                Status = branch.Status,
                CreatedAt = branch.CreatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<BranchDetailData?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Branches
            .AsNoTracking()
            .Where(branch => branch.BranchId == id)
            .Select(branch => new BranchDetailData
            {
                BranchId = branch.BranchId,
                BranchCode = branch.BranchCode,
                BranchName = branch.BranchName,
                RegionId = branch.RegionId,
                RegionCode = branch.Region.RegionCode,
                RegionName = branch.Region.RegionName,
                RegionIsActive = branch.Region.IsActive,
                RegionStatus = branch.Region.IsActive ? "ACTIVE" : "INACTIVE",
                Address = branch.Address,
                Latitude = branch.Latitude,
                Longitude = branch.Longitude,
                Phone = branch.Phone,
                Email = branch.Email,
                EmployeeCount = branch.UserBranches.Count(u => u.BranchId == branch.BranchId),
                Status = branch.Status,
                CreatedAt = branch.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsByCodeAsync(string branchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode))
        {
            return false;
        }

        var pattern = branchCode.Trim();
        return await _context.Branches
            .AsNoTracking()
            .AnyAsync(branch => EF.Functions.ILike(branch.BranchCode, pattern));
    }

    public async Task<bool> ExistsByNameAsync(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return false;
        }

        var pattern = branchName.Trim();
        return await _context.Branches
            .AsNoTracking()
            .AnyAsync(branch => EF.Functions.ILike(branch.BranchName, pattern));
    }

    public async Task<bool> ExistsByNameExceptIdAsync(string branchName, int excludeBranchId)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return false;
        }

        var pattern = branchName.Trim();
        return await _context.Branches
            .AsNoTracking()
            .AnyAsync(branch => branch.BranchId != excludeBranchId &&
                                EF.Functions.ILike(branch.BranchName, pattern));
    }

    public async Task<BranchData> CreateAsync(Branch branch)
    {
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        var region = await _context.Regions.AsNoTracking().FirstOrDefaultAsync(r => r.RegionId == branch.RegionId);

        return new BranchData
        {
            BranchId = branch.BranchId,
            BranchCode = branch.BranchCode,
            BranchName = branch.BranchName,
            RegionId = branch.RegionId,
            RegionCode = region?.RegionCode ?? string.Empty,
            RegionName = region?.RegionName ?? string.Empty,
            Address = branch.Address,
            EmployeeCount = 0,
            Status = branch.Status,
            CreatedAt = branch.CreatedAt
        };
    }

    public async Task<BranchDetailData?> UpdateAsync(Branch branch)
    {
        _context.Branches.Update(branch);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(branch.BranchId);
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchId == id);
        if (branch == null)
        {
            return false;
        }

        if (string.Equals(branch.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        branch.Status = "INACTIVE";
        branch.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchId == id);
        if (branch == null)
        {
            return false;
        }

        if (string.Equals(branch.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        branch.Status = "ACTIVE";
        branch.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(List<BranchEmployeeData> Items, int TotalCount)> GetEmployeesPagedAsync(
        int branchId,
        string? search,
        string? status,
        string? role,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(user => user.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(user =>
                EF.Functions.ILike(user.FullName, pattern) ||
                EF.Functions.ILike(user.Email, pattern) ||
                EF.Functions.ILike(user.Phone, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(user => user.Status.ToUpper() == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToUpperInvariant();
            query = query.Where(user => user.Role.RoleName.ToUpper() == normalizedRole);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(user => user.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new BranchEmployeeData
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.RoleName,
                Status = user.Status
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<Branch>> GetAssignableBranchesAsync()
    {
        return await _context.Branches
            .AsNoTracking()
            .Include(branch => branch.UserBranches)
                .ThenInclude(user => user.Role)
            .Where(branch => branch.Status == "ACTIVE")
            .OrderBy(branch => branch.BranchName)
            .ToListAsync();
    }

    public async Task<Branch?> GetEntityByIdAsync(int id)
    {
        return await _context.Branches
            .Include(b => b.Region)
            .FirstOrDefaultAsync(b => b.BranchId == id);
    }
}
