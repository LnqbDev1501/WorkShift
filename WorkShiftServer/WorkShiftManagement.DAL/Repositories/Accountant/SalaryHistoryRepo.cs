using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class SalaryHistoryRepo : ISalaryHistoryRepo
{
    private readonly AppDbContext _context;

    public SalaryHistoryRepo(AppDbContext context) => _context = context;

    public Task<int?> GetActiveAccountantBranchIdAsync(Guid accountantId, CancellationToken ct = default)
        => _context.Users.AsNoTracking()
            .Where(u => u.UserId == accountantId && u.Status == "ACTIVE" && u.DeletedAt == null
                && u.Role.Status == "ACTIVE" && u.Role.RoleName == "ACCOUNTANT")
            .Select(u => u.BranchId)
            .SingleOrDefaultAsync(ct);

    private IQueryable<StaffSalaryHistory> InBranch(int branchId)
        => _context.StaffSalaryHistories.AsNoTracking()
            .Where(h => h.User.BranchId == branchId && h.Contract.BranchId == branchId);

    private static IQueryable<StaffSalaryHistory> WithDetails(IQueryable<StaffSalaryHistory> query)
        => query.Include(h => h.User).Include(h => h.Contract).Include(h => h.CreatedByNavigation);

    public async Task<(IReadOnlyList<StaffSalaryHistory> Items, int TotalCount)> 
    GetPagedAsync(
        int branchId,
        string? keyword,
        Guid? userId, 
        string? status, 
        string? changeType,
        DateOnly? effectiveFrom, 
        DateOnly? effectiveTo, 
        int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = InBranch(branchId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var literal = keyword.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{literal}%";
            if (Guid.TryParse(keyword.Trim(), out var employeeId))
                query = query.Where(h => h.UserId == employeeId
                    || EF.Functions.ILike(h.User.FullName, pattern, "\\"));
            else
                query = query.Where(h => EF.Functions.ILike(h.User.FullName, pattern, "\\"));
        }
        if (userId.HasValue) query = query.Where(h => h.UserId == userId.Value);
        if (status != null) query = query.Where(h => h.Status == status);
        if (changeType != null) query = query.Where(h => h.ChangeType == changeType);

        // Inclusive date intervals overlap when neither interval ends before the other starts.
        // NULL effective_to means the salary interval has no end date.
        if (effectiveFrom.HasValue)
            query = query.Where(h => h.EffectiveTo == null || h.EffectiveTo >= effectiveFrom.Value);
        if (effectiveTo.HasValue)
            query = query.Where(h => h.EffectiveFrom <= effectiveTo.Value);

        var count = await query.CountAsync(ct);
        var items = await WithDetails(query)
            .OrderByDescending(h => h.EffectiveFrom).ThenByDescending(h => h.SalaryHistoryId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, count);
    }

    public Task<StaffSalaryHistory?> GetByIdAsync(int id, int branchId, CancellationToken ct = default)
        => WithDetails(InBranch(branchId)).SingleOrDefaultAsync(h => h.SalaryHistoryId == id, ct);
}
