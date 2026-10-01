using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class EmploymentContractRepo : IEmploymentContractRepo
{
    private readonly AppDbContext _context;

    public EmploymentContractRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<EmploymentContract> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? status,
        int? branchId,
        int? regionId,
        DateOnly? startDateFrom,
        DateOnly? startDateTo,
        DateOnly? endDateFrom,
        DateOnly? endDateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmploymentContracts
            .AsNoTracking()
            .Include(c => c.User)
                .ThenInclude(u => u.EmployeeType)
            .Include(c => c.Branch)
            .Include(c => c.Region)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.ContractNumber, pattern) ||
                (
                    !string.IsNullOrWhiteSpace(c.EmployeeFullNameSnapshot)
                        ? EF.Functions.ILike(c.EmployeeFullNameSnapshot, pattern)
                        : (c.User != null && EF.Functions.ILike(c.User.FullName, pattern))
                )
            );
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status.ToUpper() == status);
        }

        if (branchId.HasValue)
        {
            query = query.Where(c => c.BranchId == branchId.Value);
        }

        if (regionId.HasValue)
        {
            query = query.Where(c => c.RegionId == regionId.Value);
        }

        if (startDateFrom.HasValue)
        {
            query = query.Where(c => c.StartDate >= startDateFrom.Value);
        }

        if (startDateTo.HasValue)
        {
            query = query.Where(c => c.StartDate <= startDateTo.Value);
        }

        if (endDateFrom.HasValue)
        {
            query = query.Where(c => c.EndDate != null && c.EndDate.Value >= endDateFrom.Value);
        }

        if (endDateTo.HasValue)
        {
            query = query.Where(c => c.EndDate != null && c.EndDate.Value <= endDateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.ContractId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EmploymentContract?> GetByIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return await _context.EmploymentContracts
            .AsNoTracking()
            .Include(c => c.User)
                .ThenInclude(u => u.EmployeeType)
            .Include(c => c.Branch)
            .Include(c => c.Region)
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);
    }
}
