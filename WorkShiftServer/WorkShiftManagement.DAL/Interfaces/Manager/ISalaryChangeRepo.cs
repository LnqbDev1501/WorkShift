using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public sealed record SalaryChangeFilter(string? Keyword, Guid? UserId, int? BranchId, string? Status,
    DateTime? CreatedFrom, DateTime? CreatedToExclusive, DateOnly? EffectiveFrom, DateOnly? EffectiveTo,
    int Page, int PageSize);

public interface ISalaryChangeRepo
{
    Task<User?> ActorAsync(Guid id, CancellationToken ct);
    Task<User?> StaffAsync(Guid id, int branchId, CancellationToken ct);
    Task<SalaryChangeRequest?> FindAsync(int id, int? branchId, bool tracking, CancellationToken ct);
    Task<(List<SalaryChangeRequest> Items, int Count)> ListAsync(SalaryChangeFilter filter, CancellationToken ct);
    Task<List<StaffSalaryHistory>> SalariesAsync(IEnumerable<Guid> users, bool tracking, CancellationToken ct);
    Task<EmployeeRequest?> EmployeeRequestAsync(int id, CancellationToken ct);
    Task<bool> PendingAsync(Guid user, CancellationToken ct);
    Task<bool> HasFinalPayrollAsync(Guid user, DateOnly from, CancellationToken ct);
    Task<IDbContextTransaction> BeginChangeAsync(CancellationToken ct);
    Task<List<StaffSalaryHistory>> DueAsync(DateOnly today, CancellationToken ct);
    void Add(SalaryChangeRequest item);
    void Add(StaffSalaryHistory item);
    void Audit(Guid? actor, string action, string table, string recordId, string? oldValue, string newValue, DateTime now);
    Task SaveAsync(CancellationToken ct);
}
