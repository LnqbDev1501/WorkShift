using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftServer.API.Hubs;

namespace WorkShiftServer.API.Services;
public sealed class PayrollSystemNotificationJob(IPayrollSystemNotificationService reminders, IPayrollSystemEventRepo events,
    IPayrollCalculationRepo calculation, IHubContext<PayrollNotificationHub> hub, TimeProvider clock)
{
    [DisableConcurrentExecution(300)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        await reminders.QueueRemindersAsync(ct);
        foreach (var row in await events.PendingAsync(ct))
        {
            await using var tx = await calculation.BeginCalculationAsync(ct);
            if (await events.IsDeliveredAsync(row.EventId, ct)) continue;
            var payload = JsonSerializer.Deserialize<PayrollSystemMessage>(row.Payload) ?? throw new InvalidOperationException("Invalid payroll system event.");
            var group = payload.UserId.HasValue ? $"Payroll:user:{payload.UserId}" : payload.AudienceRole == "ADMIN" ? "Payroll:ADMIN" : $"Payroll:{payload.AudienceRole}:{payload.BranchId}";
            // At-least-once delivery: clients deduplicate by EventId; offline clients read the durable API feed.
            await hub.Clients.Group(group).SendAsync("PayrollWorkflowChanged", new { row.EventId, row.CreatedAt, Notification = payload }, ct);
            events.Delivered(row.EventId, clock.GetUtcNow().UtcDateTime); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        }
    }
}
