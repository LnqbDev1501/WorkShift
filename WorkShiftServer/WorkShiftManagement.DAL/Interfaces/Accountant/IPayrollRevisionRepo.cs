using WorkShiftManagement.DAL.Entities;
namespace WorkShiftManagement.DAL.Interfaces.Accountant;
public interface IPayrollRevisionRepo
{
    Task<Payroll?> GetAsync(int id, int? branchId, CancellationToken ct);
    Task<List<Payroll>> VersionsAsync(Guid userId, int periodId, CancellationToken ct);
    Task<PayrollFeedbackRequest?> FeedbackAsync(int id, CancellationToken ct);
    Task<PayrollFeedbackRequest?> CorrectionFeedbackAsync(int payrollId, CancellationToken ct);
    Task<Payslip?> PublishedAsync(Guid userId, int periodId, CancellationToken ct);
    void Add(Payslip payslip);
    void Audit(Guid actor, string action, Payroll payroll);
}
