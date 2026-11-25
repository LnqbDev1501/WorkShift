using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftServer.API.Services;

public sealed class RequestLimitRuleActivationWorker(IServiceScopeFactory scopes, ILogger<RequestLimitRuleActivationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var activated = await scope.ServiceProvider.GetRequiredService<IRequestLimitRuleRepo>()
                    .SynchronizeAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)), stoppingToken);
                if (activated > 0)
                    await scope.ServiceProvider.GetRequiredService<IAdminRealtimePublisher>()
                        .PublishDataChangedAsync(AdminRealtimeEvents.DataChanged,
                            new { resource = "RequestLimitRules", action = "ACTIVATED" });
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Unable to activate due request limit rules. Will retry."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
