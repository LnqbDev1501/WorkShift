using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.BLL.Services.Accountant;
public sealed class PayrollRevisionService(IPayrollCalculationRepo calculation, IPayrollRevisionRepo revisions, IPayrollFeedbackService reviews) : IPayrollRevisionService
{
    public Task<PayrollCalculationResponse> RecalculateAsync(Guid actor, int id, PayrollCorrectionRequest request, CancellationToken ct) => reviews.RecalculateAsync(actor, id, request, ct);
    public Task<PayrollCalculationResponse> ApproveCorrectionAsync(Guid actor, int id, CancellationToken ct)
        => throw new InvalidOperationException("Workflow requires Admin full-period approval; modifications prohibited after official payslip publication.");
    public async Task<PayrollVersionHistoryResponse> VersionsAsync(Guid actor, int id, CancellationToken ct)
    {
        var u = await calculation.GetActorAsync(actor, ct);
        if (u == null || u.Status != "ACTIVE" || u.DeletedAt != null || u.Role.Status != "ACTIVE" || u.Role.RoleName is not ("ADMIN" or "ACCOUNTANT")
            || u.Role.RoleName == "ACCOUNTANT" && u.BranchId == null) throw new UnauthorizedAccessException("User is not authorized to query Payroll.");
        var p = await revisions.GetAsync(id, u.Role.RoleName == "ACCOUNTANT" ? u.BranchId : null, ct) ?? throw new KeyNotFoundException("Payroll record not found.");
        var rows = await revisions.VersionsAsync(p.UserId, p.PayrollPeriodId, ct);
        return new() { UserId = p.UserId, PayrollPeriodId = p.PayrollPeriodId, LatestCandidatePayrollId = rows.Last().PayrollId,
            EffectivePayrollId = rows.LastOrDefault(p => p.Status is "APPROVED" or "PAID")?.PayrollId, Versions = rows.Select(PayrollCalculationService.Map).ToList() };
    }
}
