using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Services;

/// <summary>
/// Background worker that periodically checks for pending orders from previous days (based on Vietnam UTC+7 date)
/// and automatically updates their status to COMPLETED.
/// </summary>
public sealed class OrderAutoCompletionWorker(IServiceScopeFactory scopes, ILogger<OrderAutoCompletionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var updatedCount = await orderService.AutoCompletePastPendingOrdersAsync(stoppingToken);
                if (updatedCount > 0)
                {
                    logger.LogInformation("OrderAutoCompletionWorker: Successfully auto-completed {Count} pending order(s) from previous days.", updatedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OrderAutoCompletionWorker: Error occurred while auto-completing pending orders. Will retry on next tick.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
