using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftServer.API.Services;

/// <summary>
/// Background worker that periodically checks for:
/// 1. SCHEDULED employment contracts whose start date is due and activates them.
/// 2. ACTIVE employment contracts past their end date and expires them.
/// 3. PENDING_STAFF employment contracts past their start date without acceptance and cancels them.
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

                var expiredCount = await contractService.ExpireDueContractsAsync(stoppingToken);
                if (expiredCount > 0)
                {
                    _logger.LogInformation("Employment Contract Activation Worker expired {Count} contract(s) past end date.", expiredCount);
                }

                var cancelledCount = await contractService.CancelOverduePendingContractsAsync(stoppingToken);
                if (cancelledCount > 0)
                {
                    _logger.LogInformation("Employment Contract Activation Worker auto-cancelled {Count} overdue pending contract(s).", cancelledCount);
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
