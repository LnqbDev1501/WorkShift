using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IStaffEmployeeRequestRepo
{
    Task<User?> GetActiveStaffAsync(Guid staffId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EmployeeRequest> Items, int TotalCount)> GetPagedAsync(
        Guid staffId,
        string? reasonSearch,
        string? requestType,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest?> GetByIdAsync(
        int requestId,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftAssignment>> GetEligibleShiftsAsync(
        Guid staffId,
        int branchId,
        string requestType,
        DateOnly todayVn,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest> SubmitLeaveRequestAsync(
        Guid staffId,
        int branchId,
        List<int> assignmentIds,
        string reason,
        DateOnly todayVn,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest> SubmitLateArrivalRequestAsync(
        Guid staffId,
        int branchId,
        int assignmentId,
        DateTime requestedTimeUtc,
        string reason,
        DateOnly todayVn,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest> SubmitEarlyLeaveRequestAsync(
        Guid staffId,
        int branchId,
        int assignmentId,
        DateTime requestedTimeUtc,
        string reason,
        DateOnly todayVn,
        CancellationToken cancellationToken = default);

    Task<EmployeeRequest> CancelRequestAsync(
        int requestId,
        Guid staffId,
        CancellationToken cancellationToken = default);
}
