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

    public async Task<IReadOnlyList<User>> GetStaffWithoutContractAsync(CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking()
            .Where(u => u.DeletedAt == null &&
                u.Role.RoleName.ToUpper() == "STAFF" && (u.Status == "ACTIVE" || u.Status == "PENDING") &&
                !_context.EmploymentContracts.Any(c => c.UserId == u.UserId))
            .OrderBy(u => u.FullName).ThenBy(u => u.UserId)
            .Select(u => new User { UserId = u.UserId, FullName = u.FullName })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(User User, EmploymentContract Contract)>> GetStaffForRenewalAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var eligibleUsers = await _context.Users.AsNoTracking()
            .Where(u => u.BranchId == branchId && u.DeletedAt == null &&
                u.Role.RoleName.ToUpper() == "STAFF" && (u.Status == "ACTIVE" || u.Status == "PENDING") &&
                _context.EmploymentContracts.Any(c => c.UserId == u.UserId && c.BranchId == branchId) &&
                !_context.EmploymentContracts.Any(c => c.UserId == u.UserId &&
                    (c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL")))
            .OrderBy(u => u.FullName).ThenBy(u => u.UserId)
            .ToListAsync(cancellationToken);

        var results = new List<(User User, EmploymentContract Contract)>();
        foreach (var user in eligibleUsers)
        {
            var latestContract = await _context.EmploymentContracts.AsNoTracking()
                .Where(c => c.UserId == user.UserId && c.BranchId == branchId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.ContractId)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestContract != null)
            {
                results.Add((user, latestContract));
            }
        }
        return results;
    }

    public Task<User?> GetCreationUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().Include(u => u.Role)
            .Include(u => u.Branch).ThenInclude(b => b!.Region).ThenInclude(r => r.RegionalMinimumWage)
            .SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<bool> HasPendingContractAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.EmploymentContracts.AnyAsync(c => c.UserId == userId &&
            (c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL"), cancellationToken);

    public Task<bool> HasAnyContractAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.EmploymentContracts.AnyAsync(c => c.UserId == userId, cancellationToken);

    public Task<EmploymentContract?> GetLatestContractForStaffInBranchAsync(Guid userId, int branchId, CancellationToken cancellationToken = default) =>
        _context.EmploymentContracts.AsNoTracking()
            .Where(c => c.UserId == userId && c.BranchId == branchId)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.ContractId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task CreateDraftAsync(EmploymentContract contract, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (contract.ReplacesContractId == null && await _context.EmploymentContracts.AnyAsync(c => c.UserId == contract.UserId, cancellationToken))
            {
                throw new InvalidOperationException("Staff already has an existing employment contract. For existing staff, please use contract renewal instead.");
            }

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
                    contract.EmployeePositionSnapshot, contract.Status, contract.CreatedBy, contract.CreatedAt,
                    contract.ReplacesContractId
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
                .ThenInclude(r => r.RegionalMinimumWage)
            .Include(c => c.CreatedByNavigation)
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);
    }

    public async Task<(IReadOnlyList<EmploymentContract> Items, int TotalCount)> GetManagerPagedAsync(
        int branchId,
        Guid? staffUserId,
        string? status,
        string? contractNumber,
        string? search,
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
            .Where(c => c.BranchId == branchId);

        if (staffUserId.HasValue && staffUserId.Value != Guid.Empty)
        {
            query = query.Where(c => c.UserId == staffUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedStatus = status.Trim().ToUpper();
            query = query.Where(c => c.Status.ToUpper() == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(contractNumber))
        {
            var pattern = $"%{contractNumber.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.ContractNumber, pattern));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.ContractNumber, searchPattern) ||
                (!string.IsNullOrWhiteSpace(c.EmployeeFullNameSnapshot)
                    ? EF.Functions.ILike(c.EmployeeFullNameSnapshot, searchPattern)
                    : (c.User != null && EF.Functions.ILike(c.User.FullName, searchPattern)))
            );
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

    public async Task UpdateDraftAsync(EmploymentContract contract, Guid managerId, object oldValue, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var existing = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.ContractId == contract.ContractId, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (existing.Status != "DRAFT")
            throw new InvalidOperationException("Only draft contracts can be edited.");

        existing.StartDate = contract.StartDate;
        existing.EndDate = contract.EndDate;
        existing.EmployeePositionSnapshot = contract.EmployeePositionSnapshot;
        existing.ContractTerms = contract.ContractTerms;
        existing.HourlyRate = contract.HourlyRate;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "UPDATE",
            TableName = "employment_contracts",
            RecordId = existing.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(oldValue),
            NewValue = JsonSerializer.Serialize(new
            {
                existing.StartDate,
                existing.EndDate,
                existing.HourlyRate,
                existing.EmployeePositionSnapshot,
                existing.ContractTerms,
                existing.UpdatedAt
            }),
            CreatedAt = existing.UpdatedAt
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CancelDraftAsync(int contractId, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var existing = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (existing.Status != "DRAFT")
            throw new InvalidOperationException("Only draft contracts can be cancelled.");

        var oldStatus = existing.Status;
        var now = DateTime.UtcNow;
        existing.Status = "CANCELLED";
        existing.TerminationReason = reason;
        existing.UpdatedAt = now;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "CANCEL",
            TableName = "employment_contracts",
            RecordId = existing.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { Status = oldStatus }),
            NewValue = JsonSerializer.Serialize(new { Status = "CANCELLED", Reason = reason }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SendContractPdfAsync(int contractId, string filePath, string templateVersion, DateTime generatedAt, DateTime sentAt, Guid managerId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var existing = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (existing.Status != "DRAFT")
            throw new InvalidOperationException("Only draft contracts can be sent to staff.");

        var oldStatus = existing.Status;
        existing.ContractFilePath = filePath;
        existing.ContractTemplateVersion = templateVersion;
        existing.PdfGeneratedAt = generatedAt;
        existing.SentToStaffAt = sentAt;
        existing.StaffAcceptedAt = null;
        existing.Status = "PENDING_STAFF";
        existing.UpdatedAt = sentAt;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "SEND_CONTRACT",
            TableName = "employment_contracts",
            RecordId = existing.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { Status = oldStatus }),
            NewValue = JsonSerializer.Serialize(new
            {
                Status = "PENDING_STAFF",
                ContractFilePath = filePath,
                ContractTemplateVersion = templateVersion,
                PdfGeneratedAt = generatedAt,
                SentToStaffAt = sentAt
            }),
            CreatedAt = sentAt
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddNotificationAsync(Notification notification, IEnumerable<Guid> targetUserIds, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var userId in targetUserIds)
        {
            _context.UserNotifications.Add(new UserNotification
            {
                NotificationId = notification.NotificationId,
                UserId = userId,
                IsRead = false
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmploymentContract?> GetStaffContractByIdAsync(int contractId, Guid staffUserId, CancellationToken cancellationToken = default)
    {
        return await _context.EmploymentContracts
            .AsNoTracking()
            .Include(c => c.Branch)
            .Include(c => c.Region)
            .Include(c => c.CreatedByNavigation)
            .FirstOrDefaultAsync(c => c.ContractId == contractId && c.UserId == staffUserId && c.Status != "DRAFT", cancellationToken);
    }

    public async Task<IReadOnlyList<EmploymentContract>> GetStaffContractsAsync(Guid staffUserId, CancellationToken cancellationToken = default)
    {
        return await _context.EmploymentContracts
            .AsNoTracking()
            .Include(c => c.Branch)
            .Include(c => c.Region)
            .Where(c => c.UserId == staffUserId && c.Status != "DRAFT")
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task StaffAcceptContractAsync(int contractId, Guid staffUserId, string newStatus, DateTime acceptedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var existing = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.ContractId == contractId && c.UserId == staffUserId, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (existing.Status != "PENDING_STAFF")
            throw new InvalidOperationException("Only contracts in PENDING_STAFF status can be accepted.");

        var oldStatus = existing.Status;
        existing.Status = newStatus;
        existing.StaffAcceptedAt = acceptedAtUtc;
        existing.UpdatedAt = acceptedAtUtc;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = staffUserId,
            Action = "STAFF_ACCEPT_CONTRACT",
            TableName = "employment_contracts",
            RecordId = existing.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { Status = oldStatus }),
            NewValue = JsonSerializer.Serialize(new { Status = newStatus, StaffAcceptedAt = acceptedAtUtc }),
            CreatedAt = acceptedAtUtc
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> ActivateDueScheduledContractsAsync(DateOnly todayVnDate, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var dueContracts = await _context.EmploymentContracts
            .Where(c => c.Status == "SCHEDULED" &&
                        c.StaffAcceptedAt != null &&
                        c.StartDate <= todayVnDate &&
                        (c.EndDate == null || c.EndDate >= todayVnDate))
            .ToListAsync(cancellationToken);

        if (dueContracts.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        foreach (var c in dueContracts)
        {
            var oldStatus = c.Status;
            c.Status = "ACTIVE";
            c.UpdatedAt = now;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = c.UserId,
                Action = "WORKER_ACTIVATE_CONTRACT",
                TableName = "employment_contracts",
                RecordId = c.ContractId.ToString(),
                OldValue = JsonSerializer.Serialize(new { Status = oldStatus }),
                NewValue = JsonSerializer.Serialize(new { Status = "ACTIVE", ActivatedAt = now }),
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return dueContracts.Count;
    }

    public async Task<bool> HasActiveContractForShiftRegistrationAsync(
        Guid staffUserId, int branchId, DateTime scheduledStartAtUtc, DateTime scheduledEndAtUtc, CancellationToken cancellationToken = default)
    {
        var shiftStartVnDate = DateOnly.FromDateTime(scheduledStartAtUtc.AddHours(7));
        var shiftEndVnDate = DateOnly.FromDateTime(scheduledEndAtUtc.AddHours(7));

        return await _context.EmploymentContracts.AsNoTracking()
            .AnyAsync(c => c.UserId == staffUserId &&
                           c.Status == "ACTIVE" &&
                           c.BranchId == branchId &&
                           c.StartDate <= shiftStartVnDate &&
                           c.StartDate <= shiftEndVnDate &&
                           (c.EndDate == null || (shiftStartVnDate <= c.EndDate.Value && shiftEndVnDate <= c.EndDate.Value)),
                      cancellationToken);
    }
}
