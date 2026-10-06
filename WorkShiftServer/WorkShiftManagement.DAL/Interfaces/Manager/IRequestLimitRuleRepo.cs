using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IRequestLimitRuleRepo
{
    Task<User?> GetManagerAsync(Guid id, CancellationToken ct);
    Task<(List<RequestLimitRule> Items, int Total)> GetPagedAsync(int branchId, string? type, string? status, int page, int size, CancellationToken ct);
    Task<RequestLimitRule?> GetAsync(int branchId, int id, CancellationToken ct);
    Task<RequestLimitRule> MutateAsync(int branchId, Func<List<RequestLimitRule>, RequestLimitRule> mutation, CancellationToken ct);
    Task SynchronizeAsync(DateOnly today, CancellationToken ct);
}
