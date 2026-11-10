using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;
using NotificationEntity = WorkShiftManagement.DAL.Entities.Notification;

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
            .Where(u => u.BranchId != null && u.BranchId == branchId && u.DeletedAt == null &&
                u.Role.RoleName.ToUpper() == "STAFF" && u.Status == "ACTIVE")
            .OrderBy(u => u.FullName).ThenBy(u => u.UserId)
            .Select(u => new User { UserId = u.UserId, FullName = u.FullName })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> GetStaffWithoutContractAsync(int? branchId = null, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking()
            .Where(u => u.BranchId != null && (branchId == null || u.BranchId == branchId.Value) &&
                u.DeletedAt == null &&
                u.Role.RoleName.ToUpper() == "STAFF" && u.Status == "ACTIVE" &&
                !_context.EmploymentContracts.Any(c => c.UserId == u.UserId &&
                    (c.Status == "ACTIVE" || c.Status == "SCHEDULED" ||
                     c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL")))
            .OrderBy(u => u.FullName).ThenBy(u => u.UserId)
            .Select(u => new User
            {
                UserId = u.UserId,
                FullName = u.FullName,
                EmployeeType = u.EmployeeType != null ? new EmployeeType
                {
                    EmployeeTypeId = u.EmployeeType.EmployeeTypeId,
                    TypeName = u.EmployeeType.TypeName
                } : null
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(User User, EmploymentContract Contract, decimal? LatestSalary)>> GetStaffForRenewalAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var eligibleUsers = await _context.Users.AsNoTracking()
            .Include(u => u.EmployeeType)
            .Where(u => u.BranchId != null && u.BranchId == branchId && u.DeletedAt == null &&
                u.Role.RoleName.ToUpper() == "STAFF" && u.Status == "ACTIVE" &&
                _context.EmploymentContracts.Any(c => c.UserId == u.UserId && c.BranchId == branchId && c.Status != "CANCELLED") &&
                !_context.EmploymentContracts.Any(c => c.UserId == u.UserId &&
                    (c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL")))
            .OrderBy(u => u.FullName).ThenBy(u => u.UserId)
            .ToListAsync(cancellationToken);

        var results = new List<(User User, EmploymentContract Contract, decimal? LatestSalary)>();
        foreach (var user in eligibleUsers)
        {
            var latestContract = await _context.EmploymentContracts.AsNoTracking()
                .Where(c => c.UserId == user.UserId && c.BranchId == branchId && c.Status != "CANCELLED")
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.ContractId)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestContract != null)
            {
                var salaryHistory = await _context.StaffSalaryHistories.AsNoTracking()
                    .Where(s => (s.ContractId == latestContract.ContractId || s.UserId == user.UserId) && s.Status != "SCHEDULED")
                    .OrderByDescending(s => s.EffectiveFrom)
                    .ThenByDescending(s => s.SalaryHistoryId)
                    .FirstOrDefaultAsync(cancellationToken);

                decimal? latestSalary = salaryHistory?.HourlyRate ?? latestContract.HourlyRate;
                results.Add((user, latestContract, latestSalary));
            }
        }
        return results;
    }

    public Task<User?> GetCreationUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().Include(u => u.Role)
            .Include(u => u.EmployeeType)
            .Include(u => u.Branch).ThenInclude(b => b!.Region).ThenInclude(r => r.RegionalMinimumWage)
            .SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<bool> HasPendingContractAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.EmploymentContracts.AnyAsync(c => c.UserId == userId &&
            (c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL"), cancellationToken);

    public Task<bool> HasAnyContractAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.EmploymentContracts.AnyAsync(c => c.UserId == userId, cancellationToken);

    public Task<bool> HasActiveOrScheduledContractAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.EmploymentContracts.AnyAsync(c => c.UserId == userId &&
            (c.Status == "ACTIVE" || c.Status == "SCHEDULED"), cancellationToken);

    public async Task<IReadOnlyList<string>> GetActivePositionNamesAsync(CancellationToken cancellationToken = default) =>
        await _context.EmployeeTypes.AsNoTracking()
            .Where(et => et.Status == "ACTIVE")
            .OrderBy(et => et.TypeName)
            .Select(et => et.TypeName.Trim())
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .ToListAsync(cancellationToken);

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
            if (contract.ReplacesContractId == null && await _context.EmploymentContracts.AnyAsync(c => c.UserId == contract.UserId && (c.Status == "ACTIVE" || c.Status == "SCHEDULED"), cancellationToken))
            {
                throw new InvalidOperationException("Staff already has an active or scheduled employment contract. For existing active staff, please use contract renewal instead.");
            }

            _context.EmploymentContracts.Add(contract);
            await _context.SaveChangesAsync(cancellationToken);

            var action = contract.ReplacesContractId.HasValue ? "CREATE_RENEWAL_CONTRACT" : "CREATE_DRAFT";
            var note = contract.ReplacesContractId.HasValue
                ? $"Create renewal or replacement for contract #{contract.ReplacesContractId.Value}."
                : "Create a new employment contract draft.";

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = contract.CreatedBy,
                Action = action,
                TableName = "employment_contracts",
                RecordId = contract.ContractId.ToString(),
                OldValue = null,
                NewValue = JsonSerializer.Serialize(new
                {
                    contractId = contract.ContractId,
                    contractNumber = contract.ContractNumber,
                    fromStatus = (string?)null,
                    toStatus = "DRAFT",
                    startDate = contract.StartDate,
                    endDate = contract.EndDate,
                    hourlyRate = contract.HourlyRate,
                    employeePosition = contract.EmployeePositionSnapshot,
                    replacesContractId = contract.ReplacesContractId,
                    note = note
                }),
                CreatedAt = contract.CreatedAt
            });

            if (contract.ReplacesContractId.HasValue)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = contract.CreatedBy,
                    Action = "LINKED_REPLACEMENT_CONTRACT",
                    TableName = "employment_contracts",
                    RecordId = contract.ReplacesContractId.Value.ToString(),
                    OldValue = null,
                    NewValue = JsonSerializer.Serialize(new
                    {
                        contractId = contract.ReplacesContractId.Value,
                        replacementContractId = contract.ContractId,
                        replacementContractNumber = contract.ContractNumber,
                        note = $"Replacement contract #{contract.ContractNumber} was drafted."
                    }),
                    CreatedAt = contract.CreatedAt
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            await transaction.RollbackAsync(cancellationToken);
            if (string.Equals(pg.ConstraintName, "employment_contracts_pkey", StringComparison.OrdinalIgnoreCase))
            {
                await SyncEmploymentContractSequenceAsync(cancellationToken);
                throw new InvalidOperationException("Primary key collision on employment contract. Sequence has been synchronized; please try again.", ex);
            }
            if (string.Equals(pg.ConstraintName, "uq_one_pending_contract_per_staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Staff already has a draft or pending employment contract.", ex);
            }
            if (string.Equals(pg.ConstraintName, "uq_one_active_contract_per_staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Staff already has an active employment contract.", ex);
            }
            if (string.Equals(pg.ConstraintName, "employment_contracts_contract_number_key", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Contract number already exists. Please try again.", ex);
            }
            throw new InvalidOperationException("Database unique constraint violation: " + pg.MessageText, ex);
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
            .Include(c => c.Branch)
            .Include(c => c.Region)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(c =>
                c.ContractNumber.ToLower().Contains(searchLower) ||
                c.EmployeeFullNameSnapshot.ToLower().Contains(searchLower) ||
                c.EmployeeIdentityNoSnapshot.ToLower().Contains(searchLower) ||
                c.EmployeePhoneSnapshot.ToLower().Contains(searchLower) ||
                c.EmployeePositionSnapshot.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpper();
            query = query.Where(c => c.Status.ToUpper() == normalizedStatus);
        }

        if (branchId.HasValue)
            query = query.Where(c => c.BranchId == branchId.Value);

        if (regionId.HasValue)
            query = query.Where(c => c.RegionId == regionId.Value);

        if (startDateFrom.HasValue)
            query = query.Where(c => c.StartDate >= startDateFrom.Value);

        if (startDateTo.HasValue)
            query = query.Where(c => c.StartDate <= startDateTo.Value);

        if (endDateFrom.HasValue)
            query = query.Where(c => c.EndDate.HasValue && c.EndDate.Value >= endDateFrom.Value);

        if (endDateTo.HasValue)
            query = query.Where(c => c.EndDate.HasValue && c.EndDate.Value <= endDateTo.Value);

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
            .Include(c => c.Branch).ThenInclude(b => b!.Region).ThenInclude(r => r.RegionalMinimumWage)
            .Include(c => c.Region).ThenInclude(r => r.RegionalMinimumWage)
            .Include(c => c.CreatedByNavigation)
            .Include(c => c.ContractChangeRequest)
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
            .Include(c => c.Branch)
            .Include(c => c.Region)
            .Where(c => c.BranchId == branchId);

        if (staffUserId.HasValue && staffUserId.Value != Guid.Empty)
            query = query.Where(c => c.UserId == staffUserId.Value);

        if (!string.IsNullOrWhiteSpace(contractNumber))
            query = query.Where(c => c.ContractNumber.ToLower().Contains(contractNumber.Trim().ToLower()));

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedStatus = status.Trim().ToUpper();
            query = query.Where(c => c.Status.ToUpper() == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(c =>
                c.ContractNumber.ToLower().Contains(searchLower) ||
                c.EmployeeFullNameSnapshot.ToLower().Contains(searchLower) ||
                c.EmployeeIdentityNoSnapshot.ToLower().Contains(searchLower) ||
                c.EmployeePhoneSnapshot.ToLower().Contains(searchLower) ||
                c.EmployeePositionSnapshot.ToLower().Contains(searchLower));
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
            throw new InvalidOperationException("Only draft contracts can be updated.");

        var hasApprovedCr = await _context.ContractChangeRequests
            .AnyAsync(cr => cr.ContractId == contract.ContractId && cr.Status == "APPROVED", cancellationToken);

        var action = hasApprovedCr ? "UPDATE_DRAFT_AFTER_APPROVAL" : "UPDATE_DRAFT";
        var note = hasApprovedCr
            ? "Update the contract after an approved change request."
            : "Update the contract draft.";

        var now = DateTime.UtcNow;
        existing.StartDate = contract.StartDate;
        existing.EndDate = contract.EndDate;
        existing.HourlyRate = contract.HourlyRate;
        existing.EmployeePositionSnapshot = contract.EmployeePositionSnapshot;
        existing.ContractTerms = contract.ContractTerms;
        existing.UpdatedAt = now;

        var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == existing.UserId, cancellationToken);
        if (staffUser != null && !string.IsNullOrWhiteSpace(existing.EmployeePositionSnapshot))
        {
            var positionTrimmed = existing.EmployeePositionSnapshot.Trim();
            var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync(cancellationToken);
            var matchedType = allEmployeeTypes.FirstOrDefault(t =>
                string.Equals(t.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase))
                ?? allEmployeeTypes.FirstOrDefault(t =>
                string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase));

            if (matchedType != null && staffUser.EmployeeTypeId != matchedType.EmployeeTypeId)
            {
                staffUser.EmployeeTypeId = matchedType.EmployeeTypeId;
                staffUser.UpdatedAt = now;
            }
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = action,
            TableName = "employment_contracts",
            RecordId = existing.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(oldValue),
            NewValue = JsonSerializer.Serialize(new
            {
                fromStatus = "DRAFT",
                toStatus = "DRAFT",
                startDate = existing.StartDate,
                endDate = existing.EndDate,
                hourlyRate = existing.HourlyRate,
                employeePosition = existing.EmployeePositionSnapshot,
                contractTerms = existing.ContractTerms,
                note = note,
                updatedAt = now
            }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task CancelDraftAsync(int contractId, Guid managerId, string reason, CancellationToken cancellationToken = default)
        => CancelContractAsync(contractId, managerId, reason, cancellationToken);

    public async Task CancelContractAsync(int contractId, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existing = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException("Employment contract was not found.");

        var cancellableStatuses = new[] { "DRAFT", "PENDING_STAFF", "PENDING_MANAGER_APPROVAL", "SCHEDULED" };
        if (!cancellableStatuses.Contains(existing.Status, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot cancel a contract with status {existing.Status}. Use Terminate contract for ACTIVE contracts.");

        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (todayVn > existing.StartDate)
            throw new InvalidOperationException("Contracts can only be cancelled on or before their start date.");

        var oldStatus = existing.Status;
        var now = DateTime.UtcNow;
        existing.Status = "CANCELLED";
        await CancelIncompleteAssignmentsAsync(existing, managerId, now, cancellationToken);
        existing.CancellationReason = reason;
        existing.CancelledBy = managerId;
        existing.CancelledAt = now;
        existing.UpdatedAt = now;

        // Reject any pending change request on this contract (DRAFT or SUBMITTED)
        var pendingChangeRequests = await _context.ContractChangeRequests
            .Where(cr => cr.ContractId == contractId && (cr.Status == "DRAFT" || cr.Status == "SUBMITTED"))
            .ToListAsync(cancellationToken);
        foreach (var cr in pendingChangeRequests)
        {
            cr.Status = "REJECTED";
            cr.ReviewedBy = managerId;
            cr.ReviewedAt = now;
            cr.ReviewComment = "Automatically rejected because the contract was cancelled.";
            cr.UpdatedAt = now;
        }

        // Deactivate salary history if any
        var salaries = await _context.StaffSalaryHistories
            .Where(s => s.ContractId == contractId)
            .ToListAsync(cancellationToken);
        foreach (var s in salaries)
        {
            s.Status = "INACTIVE";
            s.Reason = (s.Reason ?? "") + $" (Contract cancelled: {reason})";
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "CANCEL_CONTRACT",
            TableName = "employment_contracts",
            RecordId = existing.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { fromStatus = oldStatus }),
            NewValue = JsonSerializer.Serialize(new
            {
                fromStatus = oldStatus,
                toStatus = "CANCELLED",
                reason = reason,
                cancelledAt = now,
                terminatedBy = managerId,
                note = $"Cancel the contract before its start date. Reason: {reason}"
            }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task TerminateContractAsync(int contractId, Guid managerId, string reason, DateTime effectiveAtUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existing = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException("Employment contract was not found.");

        if (!string.Equals(existing.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only ACTIVE contracts can be terminated. Use Cancel contract for contracts that are not yet active.");
        var warningCount = await _context.Notifications
            .CountAsync(n => n.ContractId == contractId
                          && n.NotificationType == "CONTRACT_TERMINATION_WARNING"
                          && !n.IsDeleted, cancellationToken);
        if (warningCount < 3)
            throw new InvalidOperationException($"Three violation warnings are required before termination. Current warnings: {warningCount}/3.");


        var oldStatus = existing.Status;
        var now = DateTime.UtcNow;
        existing.Status = "TERMINATED";
        await CancelIncompleteAssignmentsAsync(existing, managerId, now, cancellationToken);
        existing.TerminationReason = reason;
        existing.TerminatedBy = managerId;
        existing.TerminatedAt = now;
        existing.TerminationEffectiveAt = effectiveAtUtc;
        existing.UpdatedAt = now;

        // Reject any pending change request on this contract (DRAFT or SUBMITTED)
        var pendingChangeRequests = await _context.ContractChangeRequests
            .Where(cr => cr.ContractId == contractId && (cr.Status == "DRAFT" || cr.Status == "SUBMITTED"))
            .ToListAsync(cancellationToken);
        foreach (var cr in pendingChangeRequests)
        {
            cr.Status = "REJECTED";
            cr.ReviewedBy = managerId;
            cr.ReviewedAt = now;
            cr.ReviewComment = "Automatically rejected because the employment contract was terminated.";
            cr.UpdatedAt = now;
        }

        // Deactivate active salary history and adjust effective_to to termination date
        var effectiveVnDate = DateOnly.FromDateTime(effectiveAtUtc.AddHours(7));
        var activeSalaries = await _context.StaffSalaryHistories
            .Where(s => s.ContractId == contractId && s.Status == "ACTIVE")
            .ToListAsync(cancellationToken);
        foreach (var s in activeSalaries)
        {
            s.Status = "INACTIVE";
            if (s.EffectiveTo == null || s.EffectiveTo > effectiveVnDate)
            {
                s.EffectiveTo = effectiveVnDate >= s.EffectiveFrom ? effectiveVnDate : s.EffectiveFrom;
            }
            s.Reason = (s.Reason ?? "") + $" (Contract terminated: {reason})";
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "TERMINATE_CONTRACT",
            TableName = "employment_contracts",
            RecordId = existing.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { fromStatus = oldStatus }),
            NewValue = JsonSerializer.Serialize(new
            {
                fromStatus = oldStatus,
                toStatus = "TERMINATED",
                reason = reason,
                terminationEffectiveAt = effectiveAtUtc,
                terminatedAt = now,
                terminatedBy = managerId,
                note = $"Terminate the active contract. Reason: {reason}"
            }),
            CreatedAt = now
        });

        // Revoke active biometric face profile upon contract termination
        var activeFaceProfiles = await _context.FaceProfiles
            .Where(f => f.UserId == existing.UserId && f.Status == "ACTIVE")
            .ToListAsync(cancellationToken);
        foreach (var fp in activeFaceProfiles)
        {
            fp.Status = "REVOKED";
            fp.RevokedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task CancelIncompleteAssignmentsAsync(EmploymentContract contract, Guid? actor,
        DateTime now, CancellationToken cancellationToken)
    {
        var assignments = await _context.ShiftAssignments
            .Where(a => a.UserId == contract.UserId && a.Status == "ASSIGNED")
            .ToListAsync(cancellationToken);
        if (assignments.Count == 0) return;

        var reason = $"Employment contract {contract.ContractNumber} is {contract.Status}.";
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();
        var pendingSwaps = await _context.ShiftSwapRequests
            .Where(r => r.Status == "PENDING" &&
                (assignmentIds.Contains(r.SourceAssignmentId) || assignmentIds.Contains(r.TargetAssignmentId)))
            .ToListAsync(cancellationToken);
        foreach (var swap in pendingSwaps)
        {
            swap.Status = "CANCELLED";
            swap.RespondedBy = actor;
            swap.RespondedAt = now;
            swap.ResponseNote = reason;
            swap.UpdatedAt = now;
        }

        foreach (var assignment in assignments)
        {
            var oldValue = Manager.ShiftAssignmentRepo.Snapshot(assignment);
            assignment.Status = "CANCELLED";
            assignment.CancelledBy = actor;
            assignment.CancelledAt = now;
            assignment.CancelReason = reason;
            assignment.UpdatedAt = now;
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = actor,
                Action = "AUTO_CANCEL_SHIFT_ASSIGNMENT",
                TableName = "shift_assignments",
                RecordId = assignment.AssignmentId.ToString(),
                OldValue = oldValue,
                NewValue = Manager.ShiftAssignmentRepo.Snapshot(assignment),
                CreatedAt = now
            });
        }
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
        var isResend = existing.SentToStaffAt.HasValue;
        var action = isResend ? "RESEND_CONTRACT" : "SEND_CONTRACT";
        var note = isResend
            ? $"Resend contract version {templateVersion} to the employee."
            : $"Generate the PDF and send official contract version {templateVersion} to the employee.";

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
            Action = action,
            TableName = "employment_contracts",
            RecordId = existing.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new
            {
                fromStatus = oldStatus,
                contractFilePath = existing.ContractFilePath,
                contractTemplateVersion = existing.ContractTemplateVersion
            }),
            NewValue = JsonSerializer.Serialize(new
            {
                fromStatus = oldStatus,
                toStatus = "PENDING_STAFF",
                contractFilePath = filePath,
                contractTemplateVersion = templateVersion,
                pdfGeneratedAt = generatedAt,
                sentToStaffAt = sentAt,
                note = note
            }),
            CreatedAt = sentAt
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddNotificationAsync(WorkShiftManagement.DAL.Entities.Notification notification, IEnumerable<Guid> targetUserIds, CancellationToken cancellationToken = default)
    {
        notification.TargetType = string.IsNullOrWhiteSpace(notification.TargetType) ? "All" : notification.TargetType;
        notification.Channels ??= new() { "InApp" };
        notification.TargetRoles ??= new();
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

    public async Task<int> GetTerminationWarningCountAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.ContractId == contractId
                          && n.NotificationType == "CONTRACT_TERMINATION_WARNING"
                          && !n.IsDeleted, cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetTerminationWarningCountsAsync(IEnumerable<int> contractIds, CancellationToken cancellationToken = default)
    {
        var ids = contractIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, int>();

        var list = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.ContractId.HasValue
                     && ids.Contains(n.ContractId.Value)
                     && n.NotificationType == "CONTRACT_TERMINATION_WARNING"
                     && !n.IsDeleted)
            .GroupBy(n => n.ContractId!.Value)
            .Select(g => new { ContractId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return list.ToDictionary(x => x.ContractId, x => x.Count);
    }

    public async Task<IReadOnlyList<WorkShiftManagement.DAL.Entities.Notification>> GetTerminationWarningsByContractIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Include(n => n.CreatedByNavigation)
            .Where(n => n.ContractId == contractId
                     && n.NotificationType == "CONTRACT_TERMINATION_WARNING"
                     && !n.IsDeleted)
            .OrderBy(n => n.TerminationNoticeNo)
            .ThenBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkShiftManagement.DAL.Entities.Notification> AddTerminationWarningAsync(
        int contractId, Guid managerId, string title, string content, short warningNo, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var contract = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);

        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found.");

        if (!string.Equals(contract.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Violation warnings can only be sent for ACTIVE contracts.");

        var existingWarnings = await _context.Notifications
            .Where(n => n.ContractId == contractId
                     && n.NotificationType == "CONTRACT_TERMINATION_WARNING"
                     && !n.IsDeleted)
            .ToListAsync(cancellationToken);

        if (existingWarnings.Count >= 3)
            throw new InvalidOperationException("This contract has already received all three violation warnings.");

        short calculatedNextNo = (short)(existingWarnings.Count + 1);
        if (warningNo != calculatedNextNo)
        {
            throw new InvalidOperationException($"Invalid warning number. The next warning must be number {calculatedNextNo}.");
        }

        var now = DateTime.UtcNow;
        var notification = new WorkShiftManagement.DAL.Entities.Notification
        {
            NotificationType = "CONTRACT_TERMINATION_WARNING",
            Title = title.Trim(),
            Content = content.Trim(),
            CreatedBy = managerId,
            TargetBranchId = contract.BranchId,
            TargetType = "Branch",
            Channels = new() { "InApp" },
            TargetRoles = new(),
            ContractId = contractId,
            TerminationNoticeNo = calculatedNextNo,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _context.UserNotifications.Add(new UserNotification
        {
            NotificationId = notification.NotificationId,
            UserId = contract.UserId,
            IsRead = false,
            CreatedAt = now,
            IsDeleted = false
        });

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "SEND_TERMINATION_WARNING",
            TableName = "employment_contracts",
            RecordId = contractId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                contractId,
                warningNo = calculatedNextNo,
                title = title.Trim(),
                content = content.Trim(),
                sentAt = now,
                sentToStaffUserId = contract.UserId,
                note = $"Send contract violation warning {calculatedNextNo}/3 to the employee."
            }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return notification;
    }

    public async Task<EmploymentContract?> GetStaffContractByIdAsync(int contractId, Guid staffUserId, CancellationToken cancellationToken = default)
    {
        return await _context.EmploymentContracts
            .AsNoTracking()
            .Include(c => c.Branch).ThenInclude(b => b!.Region).ThenInclude(r => r.RegionalMinimumWage)
            .Include(c => c.Region).ThenInclude(r => r.RegionalMinimumWage)
            .Include(c => c.CreatedByNavigation)
            .Include(c => c.ContractChangeRequest)
            .FirstOrDefaultAsync(c => c.ContractId == contractId && c.UserId == staffUserId && c.Status != "DRAFT", cancellationToken);
    }

    public async Task<IReadOnlyList<EmploymentContract>> GetStaffContractsAsync(Guid staffUserId, string? status = null, string? search = null, DateOnly? startDateFrom = null, DateOnly? startDateTo = null, CancellationToken cancellationToken = default)
    {
        var query = _context.EmploymentContracts
            .AsNoTracking()
            .Include(c => c.Branch)
            .Include(c => c.Region)
            .Where(c => c.UserId == staffUserId && c.Status != "DRAFT");

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = status.Trim().ToUpper();
            query = query.Where(c => c.Status.ToUpper() == normalized);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.ContractNumber.ToLower().Contains(term) ||
                                     c.EmployeePositionSnapshot.ToLower().Contains(term) ||
                                     c.BranchNameSnapshot.ToLower().Contains(term));
        }

        if (startDateFrom.HasValue)
        {
            query = query.Where(c => c.StartDate >= startDateFrom.Value);
        }

        if (startDateTo.HasValue)
        {
            query = query.Where(c => c.StartDate <= startDateTo.Value);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task StaffAcceptContractAsync(int contractId, Guid staffUserId, string newStatus, DateTime acceptedAtUtc, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            EmploymentContract? existing;
            if (_context.Database.IsRelational())
            {
                try
                {
                    var lockedList = await _context.EmploymentContracts
                        .FromSqlInterpolated($"SELECT * FROM public.employment_contracts WHERE contract_id = {contractId} AND user_id = {staffUserId} FOR UPDATE")
                        .AsTracking()
                        .ToListAsync(cancellationToken);
                    existing = lockedList.FirstOrDefault();
                }
                catch
                {
                    existing = await _context.EmploymentContracts
                        .FirstOrDefaultAsync(c => c.ContractId == contractId && c.UserId == staffUserId, cancellationToken);
                }
            }
            else
            {
                existing = await _context.EmploymentContracts
                    .FirstOrDefaultAsync(c => c.ContractId == contractId && c.UserId == staffUserId, cancellationToken);
            }

            if (existing == null)
                throw new KeyNotFoundException("Employment contract was not found.");
            if (existing.Status != "PENDING_STAFF")
                throw new InvalidOperationException("Only contracts in PENDING_STAFF status can be accepted.");

            var todayVn = GetVietnamDate(acceptedAtUtc);
            if (todayVn > existing.StartDate)
            {
                throw new InvalidOperationException("The contract start date has passed. The contract can no longer be accepted.");
            }

            if (newStatus == "ACTIVE" && existing.ReplacesContractId.HasValue)
            {
                var oldActive = await _context.EmploymentContracts
                    .FirstOrDefaultAsync(c => c.ContractId == existing.ReplacesContractId.Value && c.Status == "ACTIVE", cancellationToken);
                if (oldActive != null)
                {
                    var oldStatusPre = oldActive.Status;
                    oldActive.Status = "EXPIRED";
                    await CancelIncompleteAssignmentsAsync(oldActive, staffUserId, acceptedAtUtc, cancellationToken);
                    // Do not alter oldActive.EndDate - preserve the contractual end date.
                    oldActive.UpdatedAt = acceptedAtUtc;

                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserId = staffUserId,
                        Action = "AUTO_EXPIRE_REPLACED_CONTRACT",
                        TableName = "employment_contracts",
                        RecordId = oldActive.ContractId.ToString(),
                        OldValue = JsonSerializer.Serialize(new { fromStatus = oldStatusPre }),
                        NewValue = JsonSerializer.Serialize(new
                        {
                            fromStatus = oldStatusPre,
                            toStatus = "EXPIRED",
                            expiredAt = acceptedAtUtc,
                            reason = $"The contract expired automatically because replacement contract {existing.ContractNumber} took effect upon acceptance.",
                            replacedByContractId = existing.ContractId
                        }),
                        CreatedAt = acceptedAtUtc
                    });

                    // Save expiration first before activating existing to satisfy uq_one_active_contract_per_staff
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            var oldStatus = existing.Status;
            existing.Status = newStatus;
            existing.StaffAcceptedAt = acceptedAtUtc;
            existing.UpdatedAt = acceptedAtUtc;

            // Reject any pending change request since staff has accepted the contract
            var openChangeRequests = await _context.ContractChangeRequests
                .Where(cr => cr.ContractId == contractId && (cr.Status == "DRAFT" || cr.Status == "SUBMITTED"))
                .ToListAsync(cancellationToken);
            foreach (var cr in openChangeRequests)
            {
                cr.Status = "REJECTED";
                cr.ReviewedBy = staffUserId;
                cr.ReviewedAt = acceptedAtUtc;
                cr.ReviewComment = "Automatically rejected because the employee accepted the contract.";
                cr.UpdatedAt = acceptedAtUtc;
            }

            var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == staffUserId, cancellationToken);
            if (staffUser != null)
            {
                staffUser.BranchId = existing.BranchId;
                var positionTrimmed = existing.EmployeePositionSnapshot?.Trim();
                if (!string.IsNullOrWhiteSpace(positionTrimmed))
                {
                    var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync(cancellationToken);
                    var matchedType = allEmployeeTypes.FirstOrDefault(t =>
                        string.Equals(t.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase))
                        ?? allEmployeeTypes.FirstOrDefault(t =>
                        string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase));

                    if (matchedType != null)
                    {
                        staffUser.EmployeeTypeId = matchedType.EmployeeTypeId;
                    }
                }
                staffUser.UpdatedAt = acceptedAtUtc;
            }

            var note = newStatus == "ACTIVE"
                ? "The employee accepted the contract. It takes effect immediately."
                : "The employee accepted the contract. It is SCHEDULED to activate on its start date.";

            var salaryStatus = (newStatus == "ACTIVE") ? "ACTIVE" : "SCHEDULED";

            if (salaryStatus == "ACTIVE")
            {
                var otherActiveSalaries = await _context.StaffSalaryHistories
                    .Where(s => s.UserId == staffUserId && s.Status == "ACTIVE")
                    .ToListAsync(cancellationToken);

                foreach (var oldSalary in otherActiveSalaries)
                {
                    oldSalary.Status = "INACTIVE";
                    if (oldSalary.EffectiveTo == null || oldSalary.EffectiveTo >= existing.StartDate)
                    {
                        var dayBefore = existing.StartDate.AddDays(-1);
                        if (dayBefore >= oldSalary.EffectiveFrom)
                        {
                            oldSalary.EffectiveTo = dayBefore;
                        }
                    }
                }
            }
            else if (existing.ReplacesContractId.HasValue)
            {
                var predSalaries = await _context.StaffSalaryHistories
                    .Where(s => s.ContractId == existing.ReplacesContractId.Value && s.UserId == staffUserId)
                    .ToListAsync(cancellationToken);

                foreach (var predSalary in predSalaries)
                {
                    if (predSalary.EffectiveTo == null || predSalary.EffectiveTo >= existing.StartDate)
                    {
                        var dayBefore = existing.StartDate.AddDays(-1);
                        if (dayBefore >= predSalary.EffectiveFrom)
                        {
                            predSalary.EffectiveTo = dayBefore;
                        }
                    }
                }
            }

            var existingSalary = await _context.StaffSalaryHistories
                .FirstOrDefaultAsync(s => s.ContractId == contractId && s.ChangeType == "INITIAL", cancellationToken);

            if (existingSalary == null)
            {
                await SyncSalaryHistorySequenceAsync(cancellationToken);

                var salaryHistory = new StaffSalaryHistory
                {
                    UserId = existing.UserId,
                    ContractId = existing.ContractId,
                    HourlyRate = existing.HourlyRate,
                    PreviousHourlyRate = null,
                    ChangeType = "INITIAL",
                    EffectiveFrom = existing.StartDate,
                    EffectiveTo = existing.EndDate,
                    Status = salaryStatus,
                    Reason = $"Initial hourly rate under employment contract {existing.ContractNumber}.",
                    CreatedBy = existing.CreatedBy,
                    CreatedAt = acceptedAtUtc
                };
                _context.StaffSalaryHistories.Add(salaryHistory);
            }
            else
            {
                // A proposal refers to this history as the immutable hourly-rate source.
                // Never resync a changed contract rate into an existing salary record.
                if (existingSalary.HourlyRate != existing.HourlyRate
                    || existingSalary.EffectiveFrom != existing.StartDate
                    || existingSalary.UserId != existing.UserId)
                    throw new InvalidOperationException("The initial salary record already exists and does not match the contract. Use the salary or contract change workflow instead of overwriting the rate or start date.");
                existingSalary.Status = salaryStatus;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = staffUserId,
                Action = "STAFF_ACCEPT_CONTRACT",
                TableName = "employment_contracts",
                RecordId = existing.ContractId.ToString(),
                OldValue = JsonSerializer.Serialize(new { fromStatus = oldStatus }),
                NewValue = JsonSerializer.Serialize(new
                {
                    fromStatus = oldStatus,
                    toStatus = newStatus,
                    staffAcceptedAt = acceptedAtUtc,
                    note = note
                }),
                CreatedAt = acceptedAtUtc
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("staff_salary_history_pkey") == true)
            {
                await SyncSalaryHistorySequenceAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task<int> ActivateDueScheduledContractsAsync(DateOnly todayVnDate, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var now = DateTime.UtcNow;

        var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync(cancellationToken);

        // Heal / synchronize any staff members whose active or accepted scheduled contract belongs to a branch but user.BranchId or EmployeeTypeId is not yet set
        var acceptedContracts = await _context.EmploymentContracts
            .Include(c => c.User)
            .Where(c => (c.Status == "ACTIVE" || (c.Status == "SCHEDULED" && c.StaffAcceptedAt != null)) && c.User != null)
            .ToListAsync(cancellationToken);

        // Group by user and prioritize ACTIVE contract that is still valid (not past EndDate).
        // If no valid ACTIVE, pick nearest SCHEDULED contract (or any ACTIVE).
        var contractsToSync = acceptedContracts
            .GroupBy(c => c.UserId)
            .Select(g => g.FirstOrDefault(c => c.Status == "ACTIVE" && (c.EndDate == null || c.EndDate >= todayVnDate))
                      ?? g.FirstOrDefault(c => c.Status == "ACTIVE")
                      ?? g.OrderBy(c => c.StartDate).First())
            .ToList();

        bool anyHealed = false;
        foreach (var c in contractsToSync)
        {
            bool userChanged = false;
            if (c.User.BranchId != c.BranchId)
            {
                c.User.BranchId = c.BranchId;
                userChanged = true;
            }

            var positionTrimmed = c.EmployeePositionSnapshot?.Trim();
            if (!string.IsNullOrWhiteSpace(positionTrimmed))
            {
                var matchedType = allEmployeeTypes.FirstOrDefault(t =>
                    string.Equals(t.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase))
                    ?? allEmployeeTypes.FirstOrDefault(t =>
                    string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase));

                if (matchedType != null && c.User.EmployeeTypeId != matchedType.EmployeeTypeId)
                {
                    c.User.EmployeeTypeId = matchedType.EmployeeTypeId;
                    userChanged = true;
                }
            }

            if (userChanged)
            {
                c.User.UpdatedAt = now;
                anyHealed = true;
            }
        }

        List<EmploymentContract> dueContracts;
        if (_context.Database.IsRelational())
        {
            try
            {
                dueContracts = await _context.EmploymentContracts
                    .FromSqlInterpolated($"SELECT * FROM public.employment_contracts WHERE status = 'SCHEDULED' AND staff_accepted_at IS NOT NULL AND start_date <= {todayVnDate} AND (end_date IS NULL OR end_date >= {todayVnDate}) FOR UPDATE")
                    .ToListAsync(cancellationToken);
            }
            catch
            {
                dueContracts = await _context.EmploymentContracts
                    .Where(c => c.Status == "SCHEDULED" &&
                                c.StaffAcceptedAt != null &&
                                c.StartDate <= todayVnDate &&
                                (c.EndDate == null || c.EndDate >= todayVnDate))
                    .ToListAsync(cancellationToken);
            }
        }
        else
        {
            dueContracts = await _context.EmploymentContracts
                .Where(c => c.Status == "SCHEDULED" &&
                            c.StaffAcceptedAt != null &&
                            c.StartDate <= todayVnDate &&
                            (c.EndDate == null || c.EndDate >= todayVnDate))
                .ToListAsync(cancellationToken);
        }

        if (dueContracts.Count == 0 && !anyHealed)
        {
            await transaction.CommitAsync(cancellationToken);
            return 0;
        }

        int totalActivated = 0;
        var dueContractsByUser = dueContracts
            .GroupBy(c => c.UserId)
            .ToList();

        foreach (var userGroup in dueContractsByUser)
        {
            var userId = userGroup.Key;

            // In case there are multiple due SCHEDULED contracts for this user:
            // Order them chronologically by StartDate then ContractId.
            var orderedDue = userGroup.OrderBy(c => c.StartDate).ThenBy(c => c.ContractId).ToList();

            // At most one target contract can become ACTIVE for today.
            // Pick the latest valid contract covering today.
            var targetContract = orderedDue.Last();

            // Existing ACTIVE contracts for this user
            var existingActiveContracts = await _context.EmploymentContracts
                .Where(p => p.UserId == userId && p.ContractId != targetContract.ContractId && p.Status == "ACTIVE")
                .ToListAsync(cancellationToken);

            bool canActivate = true;
            var contractsToExpire = new List<EmploymentContract>();

            foreach (var oldContract in existingActiveContracts)
            {
                // Has the old contract expired naturally by reaching its end date?
                bool termEnded = oldContract.EndDate.HasValue && oldContract.EndDate.Value < todayVnDate;

                // Or does targetContract explicitly replace oldContract?
                bool isAuthorizedReplacement = targetContract.ReplacesContractId.HasValue &&
                                               targetContract.ReplacesContractId.Value == oldContract.ContractId;

                if (termEnded || isAuthorizedReplacement)
                {
                    contractsToExpire.Add(oldContract);
                }
                else
                {
                    // The old contract is ACTIVE, still in effect (EndDate >= today or indefinite),
                    // and targetContract does NOT replace it!
                    // Do NOT expire oldContract or shorten its EndDate!
                    canActivate = false;
                    break;
                }
            }

            if (!canActivate)
            {
                // Cannot activate targetContract because an active, unreplaced contract is still valid.
                continue;
            }

            // Step 1: Expire old contracts FIRST and SaveChanges.
            // This satisfies PostgreSQL partial unique index uq_one_active_contract_per_staff.
            if (contractsToExpire.Count > 0)
            {
                foreach (var oldContract in contractsToExpire)
                {
                    var oldContractStatus = oldContract.Status;
                    oldContract.Status = "EXPIRED";
                    await CancelIncompleteAssignmentsAsync(oldContract, null, now, cancellationToken);
                    // DO NOT shorten oldContract.EndDate! Preserve the legal contract term!
                    oldContract.UpdatedAt = now;

                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserId = null,
                        Action = "AUTO_EXPIRE_REPLACED_CONTRACT",
                        TableName = "employment_contracts",
                        RecordId = oldContract.ContractId.ToString(),
                        OldValue = JsonSerializer.Serialize(new { fromStatus = oldContractStatus }),
                        NewValue = JsonSerializer.Serialize(new
                        {
                            fromStatus = oldContractStatus,
                            toStatus = "EXPIRED",
                            expiredAt = now,
                            reason = $"The contract expired automatically because replacement contract {targetContract.ContractNumber} took effect on {targetContract.StartDate:yyyy-MM-dd}.",
                            replacedByContractId = targetContract.ContractId
                        }),
                        CreatedAt = now
                    });
                }

                // Save expiration FIRST to guarantee zero active contracts before activating the new one
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Step 2: Now activate targetContract
            var oldStatus = targetContract.Status;
            targetContract.Status = "ACTIVE";
            targetContract.UpdatedAt = now;

            var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (staffUser != null)
            {
                staffUser.BranchId = targetContract.BranchId;
                var positionTrimmed = targetContract.EmployeePositionSnapshot?.Trim();
                if (!string.IsNullOrWhiteSpace(positionTrimmed))
                {
                    var matchedType = allEmployeeTypes.FirstOrDefault(t =>
                        string.Equals(t.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase))
                        ?? allEmployeeTypes.FirstOrDefault(t =>
                        string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase));

                    if (matchedType != null)
                    {
                        staffUser.EmployeeTypeId = matchedType.EmployeeTypeId;
                    }
                }
                staffUser.UpdatedAt = now;
            }

            // Salary history handling
            var salaryHistory = await _context.StaffSalaryHistories
                .FirstOrDefaultAsync(s => s.ContractId == targetContract.ContractId && s.ChangeType == "INITIAL", cancellationToken);

            if (salaryHistory != null)
            {
                salaryHistory.Status = "ACTIVE";
            }
            else
            {
                await SyncSalaryHistorySequenceAsync(cancellationToken);

                salaryHistory = new StaffSalaryHistory
                {
                    UserId = targetContract.UserId,
                    ContractId = targetContract.ContractId,
                    HourlyRate = targetContract.HourlyRate,
                    PreviousHourlyRate = null,
                    ChangeType = "INITIAL",
                    EffectiveFrom = targetContract.StartDate,
                    EffectiveTo = targetContract.EndDate,
                    Status = "ACTIVE",
                    Reason = $"Initial hourly rate under employment contract {targetContract.ContractNumber}.",
                    CreatedBy = targetContract.CreatedBy,
                    CreatedAt = now
                };
                _context.StaffSalaryHistories.Add(salaryHistory);
            }

            var otherActiveSalaries = await _context.StaffSalaryHistories
                .Where(s => s.UserId == userId && (salaryHistory == null || s.SalaryHistoryId != salaryHistory.SalaryHistoryId) && s.Status == "ACTIVE")
                .ToListAsync(cancellationToken);

            foreach (var otherSalary in otherActiveSalaries)
            {
                otherSalary.Status = "INACTIVE";
                if (otherSalary.EffectiveTo == null || otherSalary.EffectiveTo >= targetContract.StartDate)
                {
                    var dayBefore = targetContract.StartDate.AddDays(-1);
                    if (dayBefore >= otherSalary.EffectiveFrom)
                    {
                        otherSalary.EffectiveTo = dayBefore;
                    }
                }
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = null,
                Action = "AUTO_ACTIVATE_CONTRACT",
                TableName = "employment_contracts",
                RecordId = targetContract.ContractId.ToString(),
                OldValue = JsonSerializer.Serialize(new { fromStatus = oldStatus }),
                NewValue = JsonSerializer.Serialize(new
                {
                    fromStatus = oldStatus,
                    toStatus = "ACTIVE",
                    activatedAt = now,
                    reason = "The system automatically activated the contract on its start date.",
                    note = "The contract is now ACTIVE."
                }),
                CreatedAt = now
            });

            // Save activation SECOND
            await _context.SaveChangesAsync(cancellationToken);
            totalActivated++;
        }

        await transaction.CommitAsync(cancellationToken);
        return totalActivated;
    }

    public async Task<int> ExpireDueContractsAsync(DateOnly todayVnDate, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var expiredContracts = await _context.EmploymentContracts
            .Where(c => c.Status == "ACTIVE" &&
                        c.EndDate != null &&
                        c.EndDate < todayVnDate)
            .ToListAsync(cancellationToken);

        if (expiredContracts.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        foreach (var c in expiredContracts)
        {
            var oldStatus = c.Status;
            c.Status = "EXPIRED";
            await CancelIncompleteAssignmentsAsync(c, null, now, cancellationToken);
            c.UpdatedAt = now;

            var activeSalaries = await _context.StaffSalaryHistories
                .Where(s => s.ContractId == c.ContractId && s.Status == "ACTIVE")
                .ToListAsync(cancellationToken);
            foreach (var sh in activeSalaries)
            {
                sh.Status = "INACTIVE";
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = null,
                Action = "AUTO_EXPIRE_CONTRACT",
                TableName = "employment_contracts",
                RecordId = c.ContractId.ToString(),
                OldValue = JsonSerializer.Serialize(new { fromStatus = oldStatus }),
                NewValue = JsonSerializer.Serialize(new
                {
                    fromStatus = oldStatus,
                    toStatus = "EXPIRED",
                    expiredAt = now,
                    reason = "The system automatically updated the status after the contract end date.",
                    note = "The contract is now EXPIRED."
                }),
                CreatedAt = now
            });
        }

        // Revoke active biometric face profiles for staff whose contracts have expired
        var expiredUserIds = expiredContracts.Select(c => c.UserId).Distinct().ToList();
        var expiredActiveFaceProfiles = await _context.FaceProfiles
            .Where(f => expiredUserIds.Contains(f.UserId) && f.Status == "ACTIVE")
            .ToListAsync(cancellationToken);
        foreach (var fp in expiredActiveFaceProfiles)
        {
            fp.Status = "REVOKED";
            fp.RevokedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return expiredContracts.Count;
    }

    public async Task<CancelOverdueContractsResult> CancelOverduePendingContractsAsync(DateOnly todayVnDate, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            List<EmploymentContract> overdueContracts;
            if (_context.Database.IsRelational())
            {
                try
                {
                    overdueContracts = await _context.EmploymentContracts
                        .FromSqlInterpolated($"SELECT * FROM public.employment_contracts WHERE status = 'PENDING_STAFF' AND start_date < {todayVnDate} FOR UPDATE SKIP LOCKED")
                        .Include(c => c.CreatedByNavigation)
                        .Include(c => c.User)
                        .AsTracking()
                        .ToListAsync(cancellationToken);
                }
                catch
                {
                    overdueContracts = await _context.EmploymentContracts
                        .Where(c => c.Status == "PENDING_STAFF" && c.StartDate < todayVnDate)
                        .Include(c => c.CreatedByNavigation)
                        .Include(c => c.User)
                        .ToListAsync(cancellationToken);
                }
            }
            else
            {
                overdueContracts = await _context.EmploymentContracts
                    .Where(c => c.Status == "PENDING_STAFF" && c.StartDate < todayVnDate)
                    .Include(c => c.CreatedByNavigation)
                    .Include(c => c.User)
                    .ToListAsync(cancellationToken);
            }

            if (overdueContracts.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return new CancelOverdueContractsResult { CancelledCount = 0 };
            }

            var now = DateTime.UtcNow;
            var dispatches = new List<OverdueContractNotificationDispatch>();
            var cancelledContractsCount = 0;

            foreach (var c in overdueContracts)
            {
                if (c.Status != "PENDING_STAFF")
                    continue;

                var oldStatus = c.Status;
                c.Status = "CANCELLED";
                await CancelIncompleteAssignmentsAsync(c, null, now, cancellationToken);
                c.CancellationReason = "NOT_ACCEPTED_BY_START_DATE";
                c.CancelledBy = null;
                c.CancelledAt = now;
                c.UpdatedAt = now;
                cancelledContractsCount++;

                var pendingChangeRequests = await _context.ContractChangeRequests
                    .Where(cr => cr.ContractId == c.ContractId && (cr.Status == "DRAFT" || cr.Status == "SUBMITTED"))
                    .ToListAsync(cancellationToken);
                foreach (var cr in pendingChangeRequests)
                {
                    cr.Status = "REJECTED";
                    cr.ReviewedBy = c.CreatedBy;
                    cr.ReviewedAt = now;
                    cr.ReviewComment = "Automatically rejected because the contract was cancelled after its acceptance deadline.";
                    cr.UpdatedAt = now;
                }

                // CRITICAL INVARIANT: Only deactivate salary histories strictly tied to THIS cancelled contract (s.ContractId == c.ContractId).
                // Do NOT query or update by UserId.
                // If this is a replacing contract (c.ReplacesContractId != null), the previous/active contract and its salary history
                // (s.ContractId == c.ReplacesContractId) MUST remain ACTIVE with effective_to completely unchanged.
                var salaries = await _context.StaffSalaryHistories
                    .Where(s => s.ContractId == c.ContractId)
                    .ToListAsync(cancellationToken);
                foreach (var s in salaries)
                {
                    s.Status = "INACTIVE";
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = null,
                    Action = "AUTO_CANCEL_CONTRACT",
                    TableName = "employment_contracts",
                    RecordId = c.ContractId.ToString(),
                    OldValue = JsonSerializer.Serialize(new { fromStatus = oldStatus }),
                    NewValue = JsonSerializer.Serialize(new
                    {
                        fromStatus = oldStatus,
                        toStatus = "CANCELLED",
                        reason = "NOT_ACCEPTED_BY_START_DATE",
                        cancelledAt = now,
                        note = "The system automatically cancelled the contract because the employee did not accept before the start date."
                    }),
                    CreatedAt = now
                });

                // Idempotency check: Don't create duplicate notification if already created
                var alreadyNotified = await _context.Notifications.AnyAsync(n =>
                    n.TargetBranchId == c.BranchId &&
                    n.NotificationType == "SYSTEM" &&
                    n.Content.Contains(c.ContractNumber) &&
                    (n.Content.Contains("tự động hủy") || n.Content.Contains("chưa xác nhận chấp thuận") || n.Content.Contains("NOT_ACCEPTED_BY_START_DATE")), cancellationToken);

                if (!alreadyNotified)
                {
                    // Branch Manager(s) for THIS branch only. DO NOT send to managers in other branches!
                    var branchManagers = await _context.Users
                        .Include(u => u.Role)
                        .Where(u => u.BranchId == c.BranchId &&
                                    u.Role.RoleName.ToUpper() == "MANAGER" &&
                                    (u.Status == "ACTIVE" || u.Status == "PENDING") &&
                                    u.DeletedAt == null)
                        .ToListAsync(cancellationToken);

                    var targetUserIds = new HashSet<Guid>();
                    var targetEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // 1. Staff who owns the contract
                    if (c.UserId != Guid.Empty)
                    {
                        targetUserIds.Add(c.UserId);
                        if (!string.IsNullOrWhiteSpace(c.User?.Email))
                        {
                            targetEmails.Add(c.User.Email.Trim());
                        }
                    }

                    // 2. Manager(s) of this branch
                    foreach (var mgr in branchManagers)
                    {
                        targetUserIds.Add(mgr.UserId);
                        if (!string.IsNullOrWhiteSpace(mgr.Email))
                        {
                            targetEmails.Add(mgr.Email.Trim());
                        }
                    }

                    var notifTitle = $"Employment contract {c.ContractNumber} automatically cancelled";
                    var notifContent = $"Employment contract {c.ContractNumber} was automatically CANCELLED because the employee did not accept by {c.StartDate:yyyy-MM-dd} (NOT_ACCEPTED_BY_START_DATE).";

                    var notif = new NotificationEntity
                    {
                        NotificationType = "SYSTEM",
                        Title = notifTitle,
                        Content = notifContent,
                        CreatedBy = c.CreatedBy != Guid.Empty ? c.CreatedBy : c.UserId,
                        TargetBranchId = c.BranchId,
                        TargetType = "Branch",
                        Channels = new() { "InApp", "Email" },
                        TargetRoles = new(),
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsDeleted = false
                    };

                    _context.Notifications.Add(notif);
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var targetId in targetUserIds)
                    {
                        _context.UserNotifications.Add(new UserNotification
                        {
                            NotificationId = notif.NotificationId,
                            UserId = targetId,
                            IsRead = false,
                            CreatedAt = now
                        });
                    }

                    dispatches.Add(new OverdueContractNotificationDispatch
                    {
                        NotificationId = notif.NotificationId,
                        ContractId = c.ContractId,
                        ContractNumber = c.ContractNumber,
                        Title = notifTitle,
                        Content = notifContent,
                        CreatedAt = now,
                        BranchId = c.BranchId,
                        RecipientUserIds = targetUserIds.ToList(),
                        RecipientEmails = targetEmails.ToList(),
                        HasEmailChannel = true
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CancelOverdueContractsResult
            {
                CancelledCount = cancelledContractsCount,
                Dispatches = dispatches
            };
        });
    }

    private static DateOnly GetVietnamDate(DateTime? utcDateTime = null)
    {
        var dt = utcDateTime ?? DateTime.UtcNow;
        var utcOffset = new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
        TimeZoneInfo vnTimeZone;
        try
        {
            vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        var vnTime = TimeZoneInfo.ConvertTime(utcOffset, vnTimeZone);
        return DateOnly.FromDateTime(vnTime.DateTime);
    }

    private async Task SyncSalaryHistorySequenceAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsRelational())
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    @"SELECT setval(
                        pg_get_serial_sequence('public.staff_salary_history', 'salary_history_id'),
                        COALESCE((SELECT MAX(salary_history_id) FROM public.staff_salary_history), 0) + 1,
                        false
                    );", cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sequence Sync Warning] Failed to sync sequence for staff_salary_history: {ex.Message}");
            }
        }
    }

    private async Task SyncEmploymentContractSequenceAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsRelational())
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    @"SELECT setval(
                        pg_get_serial_sequence('public.employment_contracts', 'contract_id'),
                        COALESCE((SELECT MAX(contract_id) FROM public.employment_contracts), 0) + 1,
                        false
                    );", cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sequence Sync Warning] Failed to sync sequence for employment_contracts: {ex.Message}");
            }
        }
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

    public Task<ContractChangeRequest?> GetOpenChangeRequestByContractIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return _context.ContractChangeRequests
            .AsNoTracking()
            .Include(cr => cr.ContractChangeRequestItems)
            .FirstOrDefaultAsync(cr => cr.ContractId == contractId && (cr.Status == "DRAFT" || cr.Status == "SUBMITTED" || cr.Status == "APPROVED"), cancellationToken);
    }

    public Task<ContractChangeRequest?> GetChangeRequestByIdAsync(int changeRequestId, CancellationToken cancellationToken = default)
    {
        return _context.ContractChangeRequests
            .Include(cr => cr.ContractChangeRequestItems)
            .Include(cr => cr.Contract).ThenInclude(c => c.Branch).ThenInclude(b => b!.Region).ThenInclude(r => r.RegionalMinimumWage)
            .Include(cr => cr.Contract).ThenInclude(c => c.Region).ThenInclude(r => r.RegionalMinimumWage)
            .Include(cr => cr.RequestedByNavigation)
            .Include(cr => cr.ReviewedByNavigation)
            .FirstOrDefaultAsync(cr => cr.ChangeRequestId == changeRequestId, cancellationToken);
    }

    public async Task<IReadOnlyList<ContractChangeRequest>> GetChangeRequestsByContractIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return await _context.ContractChangeRequests
            .AsNoTracking()
            .Include(cr => cr.ContractChangeRequestItems)
            .Include(cr => cr.ReviewedByNavigation)
            .Where(cr => cr.ContractId == contractId)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ContractChangeRequest> Items, int TotalCount)> GetManagerChangeRequestsPagedAsync(
        int branchId, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ContractChangeRequests
            .AsNoTracking()
            .Include(cr => cr.ContractChangeRequestItems)
            .Include(cr => cr.Contract)
            .Include(cr => cr.RequestedByNavigation)
            .Include(cr => cr.ReviewedByNavigation)
            .Where(cr => cr.Contract.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = status.Trim().ToUpper();
            query = query.Where(cr => cr.Status.ToUpper() == normalized);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task CreateChangeRequestAsync(ContractChangeRequest changeRequest, string? previousContractStatus, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.ContractChangeRequests.Add(changeRequest);

        EmploymentContract? contract = null;
        if (string.Equals(previousContractStatus, "PENDING_STAFF", StringComparison.OrdinalIgnoreCase))
        {
            contract = await _context.EmploymentContracts.FirstOrDefaultAsync(c => c.ContractId == changeRequest.ContractId, cancellationToken);
            if (contract != null)
            {
                contract.Status = "PENDING_MANAGER_APPROVAL";
                contract.UpdatedAt = changeRequest.CreatedAt;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = changeRequest.RequestedBy,
            Action = "SUBMIT_CHANGE_REQUEST",
            TableName = "employment_contracts",
            RecordId = changeRequest.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { fromStatus = previousContractStatus }),
            NewValue = JsonSerializer.Serialize(new
            {
                contractId = changeRequest.ContractId,
                changeRequestId = changeRequest.ChangeRequestId,
                requestType = changeRequest.RequestType,
                fromStatus = previousContractStatus,
                toStatus = contract != null ? contract.Status : previousContractStatus,
                reason = changeRequest.StaffComment,
                staffComment = changeRequest.StaffComment,
                note = "The employee requested changes to the contract terms.",
                items = changeRequest.ContractChangeRequestItems.Select(i => new
                {
                    changeRequestItemId = i.ChangeRequestItemId,
                    fieldName = i.FieldName,
                    originalValue = i.OriginalValue,
                    proposedValue = i.ProposedValue,
                    reason = i.Reason
                })
            }),
            CreatedAt = changeRequest.CreatedAt
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ApproveChangeRequestAsync(int changeRequestId, Guid managerId, string? comment, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var changeRequest = await _context.ContractChangeRequests
            .Include(cr => cr.Contract)
            .FirstOrDefaultAsync(cr => cr.ChangeRequestId == changeRequestId, cancellationToken);

        if (changeRequest == null)
            throw new KeyNotFoundException("Contract change request was not found.");
        if (changeRequest.Status != "SUBMITTED")
            throw new InvalidOperationException("Only SUBMITTED change requests can be approved.");

        var now = DateTime.UtcNow;
        changeRequest.Status = "APPROVED";
        changeRequest.ReviewedBy = managerId;
        changeRequest.ReviewedAt = now;
        changeRequest.ReviewComment = comment;
        changeRequest.UpdatedAt = now;

        // If contract is PENDING_MANAGER_APPROVAL, set it back to DRAFT so manager can edit and resend
        if (changeRequest.Contract.Status == "PENDING_MANAGER_APPROVAL")
        {
            changeRequest.Contract.Status = "DRAFT";
            changeRequest.Contract.UpdatedAt = now;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "APPROVE_CHANGE_REQUEST",
            TableName = "employment_contracts",
            RecordId = changeRequest.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { fromStatus = "PENDING_MANAGER_APPROVAL" }),
            NewValue = JsonSerializer.Serialize(new
            {
                contractId = changeRequest.ContractId,
                changeRequestId = changeRequestId,
                fromStatus = "PENDING_MANAGER_APPROVAL",
                toStatus = "DRAFT",
                reason = comment,
                reviewComment = comment,
                note = "The manager approved the change request. The contract returned to DRAFT for updates."
            }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ApproveChangeRequestWithContractUpdateAsync(
        int changeRequestId,
        Guid managerId,
        string? comment,
        EmploymentContract contract,
        object? auditDetails,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var changeRequest = await _context.ContractChangeRequests
            .FirstOrDefaultAsync(cr => cr.ChangeRequestId == changeRequestId, cancellationToken);

        if (changeRequest == null)
            throw new KeyNotFoundException("Contract change request was not found.");
        if (changeRequest.Status != "SUBMITTED")
            throw new InvalidOperationException("Only SUBMITTED change requests can be approved.");

        var now = DateTime.UtcNow;
        changeRequest.Status = "APPROVED";
        changeRequest.ReviewedBy = managerId;
        changeRequest.ReviewedAt = now;
        changeRequest.ReviewComment = comment;
        changeRequest.UpdatedAt = now;

        _context.EmploymentContracts.Update(contract);

        var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == contract.UserId, cancellationToken);
        if (staffUser != null && !string.IsNullOrWhiteSpace(contract.EmployeePositionSnapshot))
        {
            var positionTrimmed = contract.EmployeePositionSnapshot.Trim();
            var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync(cancellationToken);
            var matchedType = allEmployeeTypes.FirstOrDefault(t =>
                string.Equals(t.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase))
                ?? allEmployeeTypes.FirstOrDefault(t =>
                string.Equals(t.TypeName.Trim(), positionTrimmed, StringComparison.OrdinalIgnoreCase));

            if (matchedType != null)
            {
                staffUser.EmployeeTypeId = matchedType.EmployeeTypeId;
                staffUser.UpdatedAt = now;
            }
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "APPROVE_CHANGE_REQUEST",
            TableName = "employment_contracts",
            RecordId = contract.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { fromStatus = "PENDING_MANAGER_APPROVAL" }),
            NewValue = auditDetails != null ? JsonSerializer.Serialize(auditDetails) : JsonSerializer.Serialize(new
            {
                contractId = contract.ContractId,
                changeRequestId = changeRequestId,
                toStatus = contract.Status,
                reviewComment = comment,
                templateVersion = contract.ContractTemplateVersion
            }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RejectChangeRequestAsync(int changeRequestId, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var changeRequest = await _context.ContractChangeRequests
            .Include(cr => cr.Contract)
            .FirstOrDefaultAsync(cr => cr.ChangeRequestId == changeRequestId, cancellationToken);

        if (changeRequest == null)
            throw new KeyNotFoundException("Contract change request was not found.");
        if (changeRequest.Status != "SUBMITTED")
            throw new InvalidOperationException("Only SUBMITTED change requests can be rejected.");

        var now = DateTime.UtcNow;
        changeRequest.Status = "REJECTED";
        changeRequest.ReviewedBy = managerId;
        changeRequest.ReviewedAt = now;
        changeRequest.ReviewComment = reason;
        changeRequest.UpdatedAt = now;

        // If contract was PENDING_MANAGER_APPROVAL, revert to PENDING_STAFF so staff can accept original
        if (changeRequest.Contract.Status == "PENDING_MANAGER_APPROVAL")
        {
            changeRequest.Contract.Status = "PENDING_STAFF";
            changeRequest.Contract.UpdatedAt = now;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = managerId,
            Action = "REJECT_CHANGE_REQUEST",
            TableName = "employment_contracts",
            RecordId = changeRequest.ContractId.ToString(),
            OldValue = JsonSerializer.Serialize(new { fromStatus = "PENDING_MANAGER_APPROVAL" }),
            NewValue = JsonSerializer.Serialize(new
            {
                contractId = changeRequest.ContractId,
                changeRequestId = changeRequestId,
                fromStatus = "PENDING_MANAGER_APPROVAL",
                toStatus = "PENDING_STAFF",
                reason = reason,
                reviewComment = reason,
                note = $"The manager rejected the contract change request. Reason: {reason}"
            }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmploymentContract>> GetStaffContractsInBranchAsync(Guid staffUserId, int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.EmploymentContracts
            .AsNoTracking()
            .Where(c => c.UserId == staffUserId && c.BranchId == branchId)
            .OrderBy(c => c.StartDate)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<EmploymentContract?> GetSuccessorContractAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return _context.EmploymentContracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ReplacesContractId == contractId, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetContractAuditLogsAsync(int contractId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User).ThenInclude(u => u!.Role)
            .Where(a => a.TableName == "employment_contracts" && a.RecordId == contractId.ToString())
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.AuditLogId)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetContractAuditLogsPagedAsync(
        int contractId, int pageNumber, int pageSize, string? sortOrder = "desc", CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User).ThenInclude(u => u!.Role)
            .Where(a => a.TableName == "employment_contracts" && a.RecordId == contractId.ToString());

        if (string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase))
        {
            query = query.OrderBy(a => a.CreatedAt).ThenBy(a => a.AuditLogId);
        }
        else
        {
            query = query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.AuditLogId);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<decimal?> GetRegionalMinimumWageAsync(int regionId, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        if (regionId <= 0) return null;
        var targetDate = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var region = await _context.Regions
            .AsNoTracking()
            .Include(r => r.RegionalMinimumWage)
            .FirstOrDefaultAsync(r => r.RegionId == regionId, cancellationToken);

        if (region?.RegionalMinimumWage != null &&
            region.RegionalMinimumWage.Status == "ACTIVE" &&
            region.RegionalMinimumWage.MinimumHourlyRate > 0)
        {
            var wage = region.RegionalMinimumWage;
            if ((wage.EffectiveFrom == null || wage.EffectiveFrom <= targetDate) &&
                (wage.EffectiveTo == null || wage.EffectiveTo >= targetDate))
            {
                return wage.MinimumHourlyRate;
            }
            return wage.MinimumHourlyRate;
        }

        if (region?.RegionalMinimumWageId.HasValue == true)
        {
            var wage = await _context.RegionalMinimumWages
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.RegionalMinimumWageId == region.RegionalMinimumWageId.Value && w.Status == "ACTIVE", cancellationToken);
            if (wage != null && wage.MinimumHourlyRate > 0)
                return wage.MinimumHourlyRate;
        }

        return null;
    }

    public async Task<decimal?> GetBranchRegionalMinimumWageAsync(int branchId, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        if (branchId <= 0) return null;
        var targetDate = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var branch = await _context.Branches
            .AsNoTracking()
            .Include(b => b.Region).ThenInclude(r => r!.RegionalMinimumWage)
            .FirstOrDefaultAsync(b => b.BranchId == branchId, cancellationToken);

        if (branch?.Region?.RegionalMinimumWage != null &&
            branch.Region.RegionalMinimumWage.Status == "ACTIVE" &&
            branch.Region.RegionalMinimumWage.MinimumHourlyRate > 0)
        {
            return branch.Region.RegionalMinimumWage.MinimumHourlyRate;
        }

        if (branch?.RegionId > 0)
        {
            return await GetRegionalMinimumWageAsync(branch.RegionId, targetDate, cancellationToken);
        }

        return null;
    }
}
