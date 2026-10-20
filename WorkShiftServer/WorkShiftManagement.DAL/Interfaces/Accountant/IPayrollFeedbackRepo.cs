using WorkShiftManagement.DAL.Entities;
namespace WorkShiftManagement.DAL.Interfaces.Accountant;
public sealed class PayrollReviewData
{
    public required PayrollPeriod Period { get; init; }
    public List<Payroll> Payrolls { get; init; } = [];
    public List<PayrollFeedbackRequest> Reviews { get; init; } = [];
    public List<PayrollCorrectionRequest> Corrections { get; init; } = [];
}
public interface IPayrollFeedbackRepo
{
    Task<PayrollReviewData?> LoadAsync(int periodId, CancellationToken ct);
    Task<int?> PeriodForReviewAsync(int id, CancellationToken ct);
    Task<int?> PeriodForCorrectionAsync(int id, CancellationToken ct);
    Task<int?> PeriodForPayrollAsync(int id, CancellationToken ct);
    Task<List<int>> ReviewPeriodIdsAsync(int? branch, Guid? employee, CancellationToken ct);
    Task<List<int>> DueReviewPeriodsAsync(DateTime now, CancellationToken ct);
    Task<string?> CorrectionImpactAsync(int correctionRequestId, CancellationToken ct);
    void Add(PayrollFeedbackRequest row);
    void Add(PayrollCorrectionRequest row);
    void Add(Payslip row);
    void Audit(Guid? actor, string action, string table, int id, object payload, DateTime now);
    void NotifyInApp(int periodId, int branchId, string role, Guid? user, string title, string message, DateTime now, Guid? sender = null);
    Task<List<PayrollCorrectionRequest>> QueryIssuesAsync(int? branchId, int? periodId, string? status, CancellationToken ct);
}
