using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;
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

    public async Task<IReadOnlyList<User>> GetEligibleStaffAsync(int branchId, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking()
            .Where(u => u.BranchId == branchId && u.DeletedAt == null &&
                u.Role.RoleName.ToUpper() == "STAFF" && (u.Status == "ACTIVE" || u.Status == "PENDING"))
            .OrderBy(u => u.FullName).ThenBy(u => u.UserId)
            .Select(u => new User { UserId = u.UserId, FullName = u.FullName })
            .ToListAsync(cancellationToken);

    public Task<User?> GetCreationUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().Include(u => u.Role)
            .Include(u => u.Branch).ThenInclude(b => b!.Region).ThenInclude(r => r.RegionalMinimumWage)
            .SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<bool> HasPendingContractAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.EmploymentContracts.AnyAsync(c => c.UserId == userId &&
            (c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL"), cancellationToken);

    public async Task CreateDraftAsync(EmploymentContract contract, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _context.EmploymentContracts.Add(contract);
            await _context.SaveChangesAsync(cancellationToken);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = contract.CreatedBy,
                Action = "CREATE",
                TableName = "employment_contracts",
                RecordId = contract.ContractId.ToString(),
                NewValue = JsonSerializer.Serialize(new
                {
                    contract.ContractId, contract.ContractNumber, contract.UserId, contract.BranchId,
                    contract.RegionId, contract.StartDate, contract.EndDate, contract.HourlyRate,
                    contract.EmployeePositionSnapshot, contract.Status, contract.CreatedBy, contract.CreatedAt
                }),
                CreatedAt = contract.CreatedAt
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "uq_one_pending_contract_per_staff" })
        {
            throw new InvalidOperationException("Staff already has a draft or pending employment contract.", ex);
        }
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
