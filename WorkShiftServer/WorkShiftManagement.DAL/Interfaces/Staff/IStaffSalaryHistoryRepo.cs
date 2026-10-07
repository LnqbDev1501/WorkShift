using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IStaffSalaryHistoryRepo
{
    Task<StaffSalaryHistory?> GetEffectiveSalaryHistoryAsync(Guid userId, DateOnly workDate, CancellationToken cancellationToken = default);
    Task<StaffSalaryHistory?> GetActiveSalaryHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<StaffSalaryHistory?> GetByIdAsync(int salaryHistoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffSalaryHistory>> GetByContractIdAsync(int contractId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffSalaryHistory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasInitialSalaryHistoryAsync(int contractId, CancellationToken cancellationToken = default);
}
