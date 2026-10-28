using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public class StaffSalaryHistoryRepo : IStaffSalaryHistoryRepo
{
    private readonly AppDbContext _context;

    public StaffSalaryHistoryRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StaffSalaryHistory?> GetEffectiveSalaryHistoryAsync(Guid userId, DateOnly workDate, CancellationToken cancellationToken = default)
    {
        return await _context.StaffSalaryHistories
            .AsNoTracking()
            .Include(s => s.Contract)
            .Where(s => s.UserId == userId &&
                        s.Status != "SCHEDULED" &&
                        s.EffectiveFrom <= workDate &&
                        (s.EffectiveTo == null || s.EffectiveTo >= workDate))
            .OrderByDescending(s => s.EffectiveFrom)
            .ThenByDescending(s => s.SalaryHistoryId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StaffSalaryHistory?> GetActiveSalaryHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffSalaryHistories
            .AsNoTracking()
            .Include(s => s.Contract)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "ACTIVE", cancellationToken);
    }

    public async Task<StaffSalaryHistory?> GetByIdAsync(int salaryHistoryId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffSalaryHistories
            .AsNoTracking()
            .Include(s => s.Contract)
            .FirstOrDefaultAsync(s => s.SalaryHistoryId == salaryHistoryId, cancellationToken);
    }

    public async Task<IReadOnlyList<StaffSalaryHistory>> GetByContractIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffSalaryHistories
            .AsNoTracking()
            .Include(s => s.Contract)
            .Where(s => s.ContractId == contractId)
            .OrderByDescending(s => s.EffectiveFrom)
            .ThenByDescending(s => s.SalaryHistoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StaffSalaryHistory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffSalaryHistories
            .AsNoTracking()
            .Include(s => s.Contract)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.EffectiveFrom)
            .ThenByDescending(s => s.SalaryHistoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasInitialSalaryHistoryAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffSalaryHistories
            .AnyAsync(s => s.ContractId == contractId && s.ChangeType == "INITIAL", cancellationToken);
    }
}
