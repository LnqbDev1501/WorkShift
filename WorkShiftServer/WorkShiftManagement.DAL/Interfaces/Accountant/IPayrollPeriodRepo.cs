using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public interface IPayrollPeriodRepo
{
    Task<User?> GetActorAsync(
        Guid id, 
        CancellationToken ct);
    Task<(List<PayrollPeriod> Items, int Count)> GetPagedAsync(
        int branchId, 
        string? search,
        string? status,
        DateOnly? from,
        DateOnly? to,
        int page, 
        int pageSize, 
        CancellationToken ct);
    Task<(List<WorkScheduleWeek> Items, int Count)> GetAvailableWeeksAsync(
        int branchId,
        DateOnly? from, 
        DateOnly? to, 
        int page, 
        int pageSize, 
        CancellationToken ct);
    Task<PayrollPeriod?> GetByIdAsync(
        int id, 
        int? branchId, 
        bool tracking, 
        CancellationToken ct);
    Task<WorkScheduleWeek?> GetWeekAsync(
        int id, 
        int branchId, 
        bool lockRow, 
        CancellationToken ct);
    Task<bool> WeekHasPeriodAsync(
        int weekId, 
        int? excludingId, 
        CancellationToken ct);
    Task<bool> HasDependenciesAsync(
        int id, CancellationToken ct);
    Task<int> ExpectedEmployeeCountAsync(
        PayrollPeriod period, CancellationToken ct);
    Task<Dictionary<int, int>> GetExpectedEmployeeCountsAsync(
        IReadOnlyList<PayrollPeriod> periods, CancellationToken ct);
    Task<IDbContextTransaction> BeginWriteAsync(CancellationToken ct);
    void Add(PayrollPeriod period);
    void Remove(PayrollPeriod period);
    void Audit(Guid actorId, string action, PayrollPeriod period, object? before);
    Task SaveAsync(CancellationToken ct);
}
