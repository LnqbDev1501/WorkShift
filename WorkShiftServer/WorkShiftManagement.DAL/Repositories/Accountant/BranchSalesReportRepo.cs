using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public class BranchSalesReportRepo : IBranchSalesReportRepo
{
    private readonly AppDbContext _context;

    public BranchSalesReportRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Branch?> GetBranchByIdAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BranchId == branchId, cancellationToken);
    }

    public async Task<List<BranchSalesOrderRecord>> GetBranchSalesOrdersAsync(
        int branchId,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.BranchId == branchId && o.Status != "CANCELLED");

        if (fromUtc.HasValue)
        {
            query = query.Where(o => o.OrderDate >= fromUtc.Value);
        }

        if (toUtcExclusive.HasValue)
        {
            query = query.Where(o => o.OrderDate < toUtcExclusive.Value);
        }

        return await query
            .OrderBy(o => o.OrderDate)
            .Select(o => new BranchSalesOrderRecord(
                o.OrderNumber,
                o.OrderDate,
                o.Status,
                o.TotalAmount,
                o.CreatedByNavigation.FullName))
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> GetManagerBranchIdAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == managerId && u.Status == "ACTIVE" && u.DeletedAt == null)
            .Select(u => u.BranchId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Branch?> GetUserBranchAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && u.Status == "ACTIVE" && u.DeletedAt == null && u.BranchId != null)
            .Select(u => u.Branch)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountBranchOrdersAsync(
        int branchId,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.BranchId == branchId && o.Status != "CANCELLED");

        if (fromUtc.HasValue)
        {
            query = query.Where(o => o.OrderDate >= fromUtc.Value);
        }

        if (toUtcExclusive.HasValue)
        {
            query = query.Where(o => o.OrderDate < toUtcExclusive.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}
