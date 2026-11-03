using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.Interfaces.Admin;

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
                await scope.ServiceProvider.GetRequiredService<IHolidayPayRuleService>().SynchronizeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Unable to activate due holiday pay rules. Will retry."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
