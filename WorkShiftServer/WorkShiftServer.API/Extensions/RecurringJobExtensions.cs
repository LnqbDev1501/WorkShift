using Hangfire;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftServer.API.Services;

namespace WorkShiftServer.API.Extensions;

public static class RecurringJobExtensions
{
    public static WebApplication RegisterRecurringJobs(this WebApplication app)
    {
        try
        {
            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IStaffShiftSwapService>("shift-swap-expiry",
                    service => service.ExpireDueAsync(CancellationToken.None), Cron.Minutely());
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Failed to register Hangfire recurring job 'shift-swap-expiry'.");
        }

        // Register independently: another recurring job's lock timeout must not skip salary activation.
        try
        {
            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<ISalaryChangeService>("salary-change-activation",
                    service => service.ActivateDueAsync(CancellationToken.None), Cron.Minutely());
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Failed to register Hangfire recurring job 'salary-change-activation'.");
        }

        foreach (var registerPayrollJob in new Action<IRecurringJobManager>[]
        {
            jobs => jobs.AddOrUpdate<IPayrollFeedbackService>("payroll-preview-expiry", service => service.ExpireDueAsync(CancellationToken.None), "*/5 * * * *"),
            jobs => jobs.AddOrUpdate<PayrollSystemNotificationJob>("payroll-system-notifications", job => job.ExecuteAsync(CancellationToken.None), "*/5 * * * *"),
            jobs =>
            {
                jobs.RemoveIfExists("initial-payslip-publication");
                jobs.AddOrUpdate<PayslipPublicationJob>("approved-payslip-recovery",
                    job => job.ExecuteAsync(CancellationToken.None), "*/5 * * * *");
            }
        })
        {
            try
            {
                registerPayrollJob(app.Services.GetRequiredService<IRecurringJobManager>());
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Failed to register a payroll workflow recurring job.");
            }
        }

        try
        {
            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<PayrollClosureNotificationJob>(
                    "payroll-closure-notification-recovery",
                    job => job.SendPendingAsync(CancellationToken.None),
                    "*/5 * * * *");

            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IWorkingTimeRuleService>(
                    "working-time-rules-activation",
                    service => service.ActivateDueAsync(CancellationToken.None),
                    "*/30 * * * *");

            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IAttendanceRuleService>(
                    "attendance-rules-activation",
                    service => service.ActivateDueAsync(CancellationToken.None),
                    "*/30 * * * *");

            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IAttendancePenaltyRuleService>(
                    "attendance-penalty-rules-activation",
                    service => service.ActivateDueAsync(CancellationToken.None),
                    "*/30 * * * *");

            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IStaffEmploymentContractService>(
                    "scheduled-contracts-activation",
                    service => service.ActivateDueScheduledContractsAsync(CancellationToken.None),
                    "*/15 * * * *");

            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IStaffEmploymentContractService>(
                    "expired-contracts-worker",
                    service => service.ExpireDueContractsAsync(CancellationToken.None),
                    "*/15 * * * *");

            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IStaffEmploymentContractService>(
                    "overdue-contracts-cancellation",
                    service => service.CancelOverduePendingContractsAsync(CancellationToken.None),
                    "*/15 * * * *");

            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IManagerTimesheetService>(
                    "timesheet-deadline-autolock",
                    service => service.AutoLockExpiredTimesheetsAsync(CancellationToken.None),
                    "*/5 * * * *");

            app.Services.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IStaffEmployeeRequestService>(
                    "overdue-requests-cancellation",
                    service => service.CancelOverduePendingRequestsAsync(CancellationToken.None),
                    "*/5 * * * *");
        }

        catch (Exception ex)
        {
            // A Hangfire distributed-lock timeout (e.g. Supabase pooler) must not kill API startup.
            app.Logger.LogError(ex, "Failed to register Hangfire recurring jobs.");
        }

        return app;
    }
}
