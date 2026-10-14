using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public sealed record PayrollFeedbackScope(int? BranchId, Guid? EmployeeId);
public sealed class PayrollFeedbackSearch
{
    public string? Keyword { get; init; }
    public Guid? UserId { get; init; }
    public int? PayrollPeriodId { get; init; }
    public int? PayslipId { get; init; }
    public string? Status { get; init; }
    public string? CorrectionStatus { get; init; }
    public string? ReasonCode { get; init; }
    public DateTime? CreatedFromUtc { get; init; }
    public DateTime? CreatedToExclusiveUtc { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public interface IPayrollFeedbackRepo
{
    Task<Payslip?> GetPayslipAsync(int id, PayrollFeedbackScope scope, CancellationToken ct);
    Task<bool> IsCurrentPayslipAsync(Payslip slip, CancellationToken ct);
    Task<bool> HasUnresolvedAsync(Guid userId, int periodId, CancellationToken ct);
    Task<PayrollFeedbackRequest?> GetAsync(int id, PayrollFeedbackScope scope, bool tracking, CancellationToken ct);
    Task<(List<PayrollFeedbackRequest> Items, int Count)> ListAsync(PayrollFeedbackScope scope, PayrollFeedbackSearch query, CancellationToken ct);
    void Add(PayrollFeedbackRequest feedback);
    void Audit(Guid actor, string action, PayrollFeedbackRequest feedback, DateTime now);
}
