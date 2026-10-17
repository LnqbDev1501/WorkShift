using Hangfire;
using Microsoft.AspNetCore.SignalR;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftServer.API.Hubs;

namespace WorkShiftServer.API.Services;

public sealed class HangfirePayrollClosureNotifier(IBackgroundJobClient jobs, ILogger<HangfirePayrollClosureNotifier> logger)
    : IPayrollClosureNotifier
{
    public Task NotifyAsync(int periodId)
    {
        try { jobs.Enqueue<PayrollClosureNotificationJob>(job => job.SendAsync(periodId, CancellationToken.None)); }
        catch (Exception ex)
        {
            // Settlement is already committed. The recurring scanner recovers the durable closure audit.
            logger.LogError(ex, "Cannot enqueue closure notification for payroll period {PeriodId}; recovery will retry.", periodId);
        }
        return Task.CompletedTask;
    }
}

public sealed class PayrollClosureNotificationJob(IPayrollSettlementRepo repo, IPayrollCalculationRepo calculation,
    IHubContext<PayrollNotificationHub> hub, TimeProvider clock, ILogger<PayrollClosureNotificationJob> logger)
{
    [DisableConcurrentExecution(300)]
    public async Task SendAsync(int periodId, CancellationToken ct)
    {
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var eventId = await repo.PendingClosureEventAsync(periodId, ct);
        if (eventId.HasValue)
        {
            var data = await repo.LoadAsync(periodId, null, true, ct);
            if (data?.Period.Status == "CLOSED")
            {
                var period = data.Period;
                await hub.Clients.Group("Payroll:ADMIN").SendAsync("PayrollPeriodClosed", new
                { eventId, period.PayrollPeriodId, period.PeriodCode, period.BranchId, period.ClosedAt, status = "CLOSED" }, ct);
                repo.Audit(null, "PAYROLL_CLOSURE_NOTIFIED", period, clock.GetUtcNow().UtcDateTime, new { eventId });
                await calculation.SaveAsync(ct);
            }
        }
        await tx.CommitAsync(ct);
    }

    public async Task SendPendingAsync(CancellationToken ct)
    {
        foreach (var id in await repo.PendingClosureIdsAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            try { await SendAsync(id, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError(ex, "Closure notification for payroll period {PeriodId} failed; remains pending for retry.", id); }
        }
    }
}
