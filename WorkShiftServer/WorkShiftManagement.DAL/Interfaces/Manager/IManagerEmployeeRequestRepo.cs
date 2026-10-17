using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IManagerEmployeeRequestRepo
{
    Task<User?> GetActiveManagerAsync(Guid managerId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EmployeeRequest> Items, int TotalCount)> GetPagedAsync(
        int branchId,
        string? search,
        string? requestType,
        string? status,
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest?> GetByIdAsync(
        int requestId,
        int branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetHistoryAsync(
        int requestId,
        int branchId,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest> ApproveRequestAsync(
        int requestId,
        int branchId,
        Guid managerId,
        string? reviewNote,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest> RejectRequestAsync(
        int requestId,
        int branchId,
        Guid managerId,
        string reviewNote,
        CancellationToken cancellationToken = default);

    Task<List<Guid>> GetEligibleStaffUserIdsAsync(
        int branchId,
        int employeeTypeId,
        Guid excludeStaffId,
        CancellationToken cancellationToken = default);
}
