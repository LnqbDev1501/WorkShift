using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IEmploymentContractRepo
{
    Task<IReadOnlyList<User>> GetEligibleStaffAsync(int branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetStaffWithoutContractAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(User User, EmploymentContract Contract)>> GetStaffForRenewalAsync(int branchId, CancellationToken cancellationToken = default);
    Task<User?> GetCreationUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingContractAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyContractAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<EmploymentContract?> GetLatestContractForStaffInBranchAsync(Guid userId, int branchId, CancellationToken cancellationToken = default);
    Task CreateDraftAsync(EmploymentContract contract, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EmploymentContract> Items, int TotalCount)> GetPagedAsync(
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
        CancellationToken cancellationToken = default);

    Task<EmploymentContract?> GetByIdAsync(int contractId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EmploymentContract> Items, int TotalCount)> GetManagerPagedAsync(
        int branchId,
        Guid? staffUserId,
        string? status,
        string? contractNumber,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task UpdateDraftAsync(EmploymentContract contract, Guid managerId, object oldValue, CancellationToken cancellationToken = default);
    Task CancelDraftAsync(int contractId, Guid managerId, string reason, CancellationToken cancellationToken = default);
    Task SendContractPdfAsync(int contractId, string filePath, string templateVersion, DateTime generatedAt, DateTime sentAt, Guid managerId, CancellationToken cancellationToken = default);
    Task AddNotificationAsync(Notification notification, IEnumerable<Guid> targetUserIds, CancellationToken cancellationToken = default);

    Task<EmploymentContract?> GetStaffContractByIdAsync(int contractId, Guid staffUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmploymentContract>> GetStaffContractsAsync(Guid staffUserId, CancellationToken cancellationToken = default);
    Task StaffAcceptContractAsync(int contractId, Guid staffUserId, string newStatus, DateTime acceptedAtUtc, CancellationToken cancellationToken = default);
    Task<int> ActivateDueScheduledContractsAsync(DateOnly todayVnDate, CancellationToken cancellationToken = default);
    Task<bool> HasActiveContractForShiftRegistrationAsync(Guid staffUserId, int branchId, DateTime scheduledStartAtUtc, DateTime scheduledEndAtUtc, CancellationToken cancellationToken = default);
}
