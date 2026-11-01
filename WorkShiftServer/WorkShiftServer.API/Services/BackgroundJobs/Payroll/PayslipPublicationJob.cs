using Hangfire;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Services;

public sealed class PayslipPublicationJob(IPayslipService service, ILogger<PayslipPublicationJob> logger)
{
    [DisableConcurrentExecution(300)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var failed = new List<Exception>();
        foreach (var id in await service.DuePeriodIdsAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            try { await service.PublishInitialAsync(id, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to publish initial payslips for payroll period {PeriodId}.", id);
                failed.Add(ex);
            }
        }
        // Hangfire records failure and retries; a failed branch does not prevent other branches publishing.
        if (failed.Count > 0) throw new AggregateException("Failed to publish payslips for some payroll periods.", failed);
    }
}
