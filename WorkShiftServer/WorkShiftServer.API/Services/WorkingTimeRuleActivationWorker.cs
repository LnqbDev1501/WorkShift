using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Services;

public sealed class WorkingTimeRuleActivationWorker(
    IServiceScopeFactory scopeFactory, ILogger<WorkingTimeRuleActivationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IWorkingTimeRuleService>().ActivateDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unable to activate due working time versions. Retrying on the next tick.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
