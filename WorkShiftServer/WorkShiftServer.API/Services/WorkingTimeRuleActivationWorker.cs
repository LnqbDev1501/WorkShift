using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Services;

public sealed class WorkingTimeRuleActivationWorker : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkingTimeRuleActivationWorker> _logger;

    public WorkingTimeRuleActivationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkingTimeRuleActivationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        try
        {
            do
            {
                await ActivateDueVersionsAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown, including cancellation while waiting for a tick.
        }
    }

    private async Task ActivateDueVersionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IWorkingTimeRuleService>();
            await service.ActivateDueAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to activate due working time versions. Retrying on the next tick.");
        }
    }
}
