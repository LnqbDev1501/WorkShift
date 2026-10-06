using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IAdminEmployeeRequestRepo
{
    Task<(IReadOnlyList<EmployeeRequest> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? requestType,
        string? status,
        int? branchId,
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest?> GetByIdAsync(int requestId, CancellationToken cancellationToken = default);
}
