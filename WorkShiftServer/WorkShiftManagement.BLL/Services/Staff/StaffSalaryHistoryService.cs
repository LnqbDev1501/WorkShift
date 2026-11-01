using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.BLL.Services.Staff;

public class StaffSalaryHistoryService(IStaffSalaryHistoryRepo repo) : IStaffSalaryHistoryService
{
    public async Task<StaffSalaryHistoryDto> GetEffectiveSalaryForDateAsync(Guid userId, DateOnly workDate, CancellationToken cancellationToken = default)
    {
        var salary = await repo.GetEffectiveSalaryHistoryAsync(userId, workDate, cancellationToken);
        if (salary == null)
        {
            throw new InvalidOperationException(
                $"No valid salary rate found for staff member ID '{userId}' on work date {workDate:yyyy-MM-dd}. Payroll calculation cannot proceed with zero rate.");
        }

        if (salary.HourlyRate <= 0)
        {
            throw new InvalidOperationException(
                $"Hourly rate ({salary.HourlyRate:N0} VND) for staff member ID '{userId}' on work date {workDate:yyyy-MM-dd} is invalid. Payroll calculation cannot proceed with zero rate.");
        }

        return MapToDto(salary);
    }

    public async Task<(int SalaryHistoryId, decimal HourlyRateSnapshot)> ResolveSalarySnapshotForWorkDateAsync(Guid userId, DateOnly workDate, CancellationToken cancellationToken = default)
    {
        var salary = await repo.GetEffectiveSalaryHistoryAsync(userId, workDate, cancellationToken);
        if (salary == null)
        {
            throw new InvalidOperationException(
                $"No valid salary rate found for staff member ID '{userId}' on work date {workDate:yyyy-MM-dd}. Payroll calculation cannot proceed with zero rate.");
        }

        if (salary.HourlyRate <= 0)
        {
            throw new InvalidOperationException(
                $"Hourly rate ({salary.HourlyRate:N0} VND) for staff member ID '{userId}' on work date {workDate:yyyy-MM-dd} is invalid. Payroll calculation cannot proceed with zero rate.");
        }

        return (salary.SalaryHistoryId, salary.HourlyRate);
    }

    public async Task<IReadOnlyList<StaffSalaryHistoryDto>> GetByContractIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        var list = await repo.GetByContractIdAsync(contractId, cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<StaffSalaryHistoryDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await repo.GetByUserIdAsync(userId, cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    private static StaffSalaryHistoryDto MapToDto(StaffSalaryHistory s) => new()
    {
        SalaryHistoryId = s.SalaryHistoryId,
        UserId = s.UserId,
        ContractId = s.ContractId,
        ContractNumber = s.Contract?.ContractNumber ?? string.Empty,
        HourlyRate = s.HourlyRate,
        PreviousHourlyRate = s.PreviousHourlyRate,
        ChangeType = s.ChangeType,
        EffectiveFrom = s.EffectiveFrom,
        EffectiveTo = s.EffectiveTo,
        Status = s.Status,
        Reason = s.Reason,
        CreatedBy = s.CreatedBy,
        CreatedAt = s.CreatedAt
    };
}
