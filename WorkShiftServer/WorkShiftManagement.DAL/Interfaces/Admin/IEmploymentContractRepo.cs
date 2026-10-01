using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IEmploymentContractRepo
{
    Task<IReadOnlyList<User>> GetEligibleStaffAsync(int branchId, CancellationToken cancellationToken = default);
    Task<User?> GetCreationUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingContractAsync(Guid userId, CancellationToken cancellationToken = default);
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
}
