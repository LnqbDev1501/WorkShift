using WorkShiftManagement.BLL.Services.Admin;

namespace WorkShiftServer.API.Services;

public sealed class HolidayRuleActivationWorker(IServiceScopeFactory scopes, ILogger<HolidayRuleActivationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<HolidayPayRuleService>().SynchronizeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Unable to activate due holiday pay rules. Will retry."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
