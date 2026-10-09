using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Staff.Response;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

/// <summary>
/// Service interface for managing and querying staff salary history records.
/// </summary>
public interface IStaffSalaryHistoryService
{
    /// <summary>
    /// Retrieves the effective salary history record for a staff member on a specific work date.
    /// </summary>
    /// <param name="userId">The unique identifier of the staff user.</param>
    /// <param name="workDate">The specific date of work.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The effective staff salary history DTO.</returns>
    Task<StaffSalaryHistoryDto> GetEffectiveSalaryForDateAsync(Guid userId, DateOnly workDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the salary history identifier and hourly rate snapshot for payroll calculation on a specific work date.
    /// Throws an exception if no valid or non-zero salary record is found.
    /// </summary>
    /// <param name="userId">The unique identifier of the staff user.</param>
    /// <param name="workDate">The specific date of work.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A tuple containing the salary history ID and the hourly rate snapshot.</returns>
    Task<(int SalaryHistoryId, decimal HourlyRateSnapshot)> ResolveSalarySnapshotForWorkDateAsync(Guid userId, DateOnly workDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all salary history records associated with a specific employment contract.
    /// </summary>
    /// <param name="contractId">The employment contract identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of salary history DTOs for the specified contract.</returns>
    Task<IReadOnlyList<StaffSalaryHistoryDto>> GetByContractIdAsync(int contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all salary history records associated with a specific staff user.
    /// </summary>
    /// <param name="userId">The unique identifier of the staff user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of salary history DTOs for the specified user.</returns>
    Task<IReadOnlyList<StaffSalaryHistoryDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
