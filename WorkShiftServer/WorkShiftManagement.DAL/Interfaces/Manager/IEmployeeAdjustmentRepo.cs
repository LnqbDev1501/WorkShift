using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Entities;
namespace WorkShiftManagement.DAL.Interfaces.Manager;

public sealed record EmployeeAdjustmentFilter(string? Keyword, Guid? UserId, int? PeriodId, int? BranchId,
    string? Status, DateTime? From, DateTime? ToExclusive, int Page, int PageSize);
public interface IEmployeeAdjustmentRepo
{
    Task<User?> ActorAsync(Guid id, CancellationToken ct);
    Task<User?> StaffAsync(Guid id, int branchId, CancellationToken ct);
    Task<PayrollPeriod?> PeriodAsync(int id, int? branchId, CancellationToken ct);
    Task<List<PayrollPeriod>> PeriodsAsync(int branchId, IReadOnlyCollection<string> statuses, CancellationToken ct);
    Task<EmployeeRewardsPenalty?> FindAsync(int id, string type, int? branchId, bool tracking, CancellationToken ct);
    Task<(List<EmployeeRewardsPenalty> Items, int Count)> ListAsync(string type, EmployeeAdjustmentFilter filter, CancellationToken ct);
    Task<bool> HasPayrollSourceAsync(int id, CancellationToken ct);
    Task<IDbContextTransaction> BeginChangeAsync(CancellationToken ct);
    void Add(EmployeeRewardsPenalty item);
    void Audit(Guid actor, string action, EmployeeRewardsPenalty item, string? oldValue);
    Task SaveAsync(CancellationToken ct);
}
