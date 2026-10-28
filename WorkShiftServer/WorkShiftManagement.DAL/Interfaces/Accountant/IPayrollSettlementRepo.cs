using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public sealed class PayrollSettlementData
{
    public required PayrollPeriod Period { get; init; }
    public List<Payroll> Payrolls { get; init; } = [];
    public HashSet<Guid> ExpectedEmployees { get; init; } = [];
    public HashSet<Guid> UnsettledPriorEmployees { get; init; } = [];
    public int UnresolvedFeedbackCount { get; init; }
    public List<PayrollCarryForwardDeduction> CarryForwards { get; init; } = [];
}
public sealed class CarryForwardSearch
{
    public string? Keyword { get; init; }
    public Guid? UserId { get; init; }
    public string? Status { get; init; }
    public int? SourcePayrollId { get; init; }
    public int? SourcePayrollPeriodId { get; init; }
    public DateTime? CreatedFromUtc { get; init; }
    public DateTime? CreatedToExclusiveUtc { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
public interface IPayrollSettlementRepo
{
    Task<PayrollSettlementData?> LoadAsync(int periodId, int? branchId, bool tracking, CancellationToken ct);
    Task<(List<PayrollCarryForwardDeduction> Items, int Count)> ListCarryAsync(int branchId, CarryForwardSearch query, CancellationToken ct);
    Task<PayrollCarryForwardDeduction?> GetCarryAsync(int id, int branchId, CancellationToken ct);
    void Add(PayrollCarryForwardDeduction carry);
    void Add(PayrollCarryForwardApplication application);
    void Audit(Guid? actor, string action, PayrollPeriod period, DateTime now, object payload);
    Task<List<int>> PendingClosureIdsAsync(CancellationToken ct);
    Task<long?> PendingClosureEventAsync(int periodId, CancellationToken ct);
}
