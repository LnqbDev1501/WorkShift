using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftServer.API.Services;

/// <summary>
/// Background worker that periodically checks for SCHEDULED employment contracts whose start date is due and activates them.
/// </summary>
public class EmploymentContractActivationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmploymentContractActivationWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public EmploymentContractActivationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<EmploymentContractActivationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Employment Contract Activation Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var contractService = scope.ServiceProvider.GetRequiredService<IStaffEmploymentContractService>();
                var activatedCount = await contractService.ActivateDueScheduledContractsAsync(stoppingToken);

                if (activatedCount > 0)
                {
                    _logger.LogInformation("Employment Contract Activation Worker activated {Count} due scheduled contract(s).", activatedCount);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while executing Employment Contract Activation Worker.");
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

        _logger.LogInformation("Employment Contract Activation Worker stopping.");
    }
}
