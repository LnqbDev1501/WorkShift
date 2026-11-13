using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IEmploymentContractRepo
{
    Task<IReadOnlyList<User>> GetEligibleStaffAsync(int branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetStaffWithoutContractAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(User User, EmploymentContract Contract, decimal? LatestSalary)>> GetStaffForRenewalAsync(int branchId, CancellationToken cancellationToken = default);
    Task<User?> GetCreationUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingContractAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyContractAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveOrScheduledContractAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetActivePositionNamesAsync(CancellationToken cancellationToken = default);
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
    Task CancelContractAsync(int contractId, Guid managerId, string reason, CancellationToken cancellationToken = default);
    Task TerminateContractAsync(int contractId, Guid managerId, string reason, DateTime effectiveAtUtc, CancellationToken cancellationToken = default);
    Task SendContractPdfAsync(int contractId, string filePath, string templateVersion, DateTime generatedAt, DateTime sentAt, Guid managerId, CancellationToken cancellationToken = default);
    Task AddNotificationAsync(WorkShiftManagement.DAL.Entities.Notification notification, IEnumerable<Guid> targetUserIds, CancellationToken cancellationToken = default);

    Task<int> GetTerminationWarningCountAsync(int contractId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetTerminationWarningCountsAsync(IEnumerable<int> contractIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkShiftManagement.DAL.Entities.Notification>> GetTerminationWarningsByContractIdAsync(int contractId, CancellationToken cancellationToken = default);
    Task<WorkShiftManagement.DAL.Entities.Notification> AddTerminationWarningAsync(int contractId, Guid managerId, string title, string content, short warningNo, CancellationToken cancellationToken = default);

    Task<EmploymentContract?> GetStaffContractByIdAsync(int contractId, Guid staffUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmploymentContract>> GetStaffContractsAsync(Guid staffUserId, string? status = null, string? search = null, DateOnly? startDateFrom = null, DateOnly? startDateTo = null, CancellationToken cancellationToken = default);
    Task StaffAcceptContractAsync(int contractId, Guid staffUserId, string newStatus, DateTime acceptedAtUtc, CancellationToken cancellationToken = default);
    Task<int> ActivateDueScheduledContractsAsync(DateOnly todayVnDate, CancellationToken cancellationToken = default);
    Task<int> ExpireDueContractsAsync(DateOnly todayVnDate, CancellationToken cancellationToken = default);
    Task<CancelOverdueContractsResult> CancelOverduePendingContractsAsync(DateOnly todayVnDate, CancellationToken cancellationToken = default);
    Task<bool> HasActiveContractForShiftRegistrationAsync(Guid staffUserId, int branchId, DateTime scheduledStartAtUtc, DateTime scheduledEndAtUtc, CancellationToken cancellationToken = default);
    Task<(bool IsInShift, string? ShiftInfo)> IsStaffCurrentlyInShiftAsync(Guid staffUserId, DateTime? currentUtc = null, CancellationToken cancellationToken = default);

    // Contract Change Requests
    Task<ContractChangeRequest?> GetOpenChangeRequestByContractIdAsync(int contractId, CancellationToken cancellationToken = default);
    Task<ContractChangeRequest?> GetChangeRequestByIdAsync(int changeRequestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContractChangeRequest>> GetChangeRequestsByContractIdAsync(int contractId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ContractChangeRequest> Items, int TotalCount)> GetManagerChangeRequestsPagedAsync(int branchId, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task CreateChangeRequestAsync(ContractChangeRequest changeRequest, string? previousContractStatus, CancellationToken cancellationToken = default);
    Task ApproveChangeRequestAsync(int changeRequestId, Guid managerId, string? comment, CancellationToken cancellationToken = default);
    Task ApproveChangeRequestWithContractUpdateAsync(int changeRequestId, Guid managerId, string? comment, EmploymentContract contract, object? auditDetails, CancellationToken cancellationToken = default);
    Task RejectChangeRequestAsync(int changeRequestId, Guid managerId, string reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmploymentContract>> GetStaffContractsInBranchAsync(Guid staffUserId, int branchId, CancellationToken cancellationToken = default);
    Task<EmploymentContract?> GetSuccessorContractAsync(int contractId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetContractAuditLogsAsync(int contractId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetContractAuditLogsPagedAsync(
        int contractId, int pageNumber, int pageSize, string? sortOrder = "desc", CancellationToken cancellationToken = default);

    Task<decimal?> GetRegionalMinimumWageAsync(int regionId, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default);
    Task<decimal?> GetBranchRegionalMinimumWageAsync(int branchId, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default);
}
