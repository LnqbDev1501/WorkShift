using Hangfire;
using WorkShiftServer.API.Extensions;
using WorkShiftServer.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiPresentation()
    .AddApiAuthentication(builder.Configuration)
    .AddPersistenceAndBackgroundProcessing(builder.Configuration)
    .AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.RegisterRecurringJobs();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard();
}

// Allow HTTP for local development and reverse-proxy hosting.
app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<AdminRealtimeHub>("/hubs/admin-realtime");
app.MapHub<PayrollNotificationHub>("/hubs/payroll-notifications");

await app.NormalizeLegacyNotificationsAsync();

app.Run();
