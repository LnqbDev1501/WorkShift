using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public sealed record PayslipScope(int? BranchId, Guid? UserId);
public sealed class PayslipSearch
{
    public string? Keyword { get; init; }
    public Guid? UserId { get; init; }
    public int? BranchId { get; init; }
    public int? PayrollPeriodId { get; init; }
    public string? Status { get; init; }
    public bool IncludeHistory { get; init; }
    public DateOnly? PeriodFrom { get; init; }
    public DateOnly? PeriodTo { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public interface IPayslipRepo
{
    Task<(List<Payslip> Items, int Count)> ListAsync(PayslipScope scope, PayslipSearch query, CancellationToken ct);
    Task<Payslip?> GetAsync(PayslipScope scope, int id, CancellationToken ct);
    Task<List<Payslip>> VersionsAsync(PayslipScope scope, Guid userId, int periodId, CancellationToken ct);
    Task<HashSet<(Guid UserId, int PeriodId)>> UnresolvedAsync(IEnumerable<Payslip> slips, CancellationToken ct);
    Task<List<int>> DuePeriodIdsAsync(CancellationToken ct);
    Task<List<Payroll>> PublicationPayrollsAsync(int periodId, CancellationToken ct);
    Task<HashSet<Guid>> ExpectedEmployeesAsync(PayrollPeriod period, CancellationToken ct);
    void Add(Payslip slip);
    void RecordPublicationAudit(int periodId, DateTime scheduledAt, DateTime publishedAt, int count);
}
