using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Services;

/// <summary>
/// Background worker that periodically checks for PUBLISH regional minimum wages whose effective date is due and activates them.
/// </summary>
public class RegionalMinimumWageActivationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegionalMinimumWageActivationWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    /// <summary>
    /// Initializes a new instance of <see cref="RegionalMinimumWageActivationWorker"/>.
    /// </summary>
    /// <param name="scopeFactory">Service scope factory dependency.</param>
    /// <param name="logger">Logger instance.</param>
    public RegionalMinimumWageActivationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RegionalMinimumWageActivationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Regional Minimum Wage Activation Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var wageService = scope.ServiceProvider.GetRequiredService<IRegionalMinimumWageService>();
                var activatedCount = await wageService.ActivateDueWagesAsync();

                if (activatedCount > 0)
                {
                    await scope.ServiceProvider.GetRequiredService<IAdminRealtimePublisher>()
                        .PublishDataChangedAsync(AdminRealtimeEvents.DataChanged,
                            new { resource = "RegionalMinimumWages", action = "ACTIVATED" });
                    _logger.LogInformation("Regional Minimum Wage Activation Worker activated {Count} due wage(s).", activatedCount);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while executing Regional Minimum Wage Activation Worker.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Regional Minimum Wage Activation Worker stopping.");
    }
}
