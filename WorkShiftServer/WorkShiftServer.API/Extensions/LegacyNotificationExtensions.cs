using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftServer.API.Extensions;

public static class LegacyNotificationExtensions
{
    public static async Task NormalizeLegacyNotificationsAsync(this WebApplication app)
    {
        try
        {
            using var cleanupScope = app.Services.CreateScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifsToClean = await dbContext.Notifications
                .Where(n => n.Content.Contains("NOT_ACCEPTED_BY_START_DATE"))
                .ToListAsync();
            if (notifsToClean.Count > 0)
            {
                foreach (var n in notifsToClean)
                {
                    n.Content = n.Content
                        .Replace("Lý do: NOT_ACCEPTED_BY_START_DATE (quá ngày bắt đầu", "Lý do: Quá ngày bắt đầu")
                        .Replace("Lý do: NOT_ACCEPTED_BY_START_DATE (Quá ngày bắt đầu", "Lý do: Quá ngày bắt đầu")
                        .Replace("chưa xác nhận chấp thuận).", "chưa xác nhận chấp thuận.")
                        .Replace("chưa xác nhận chấp thuận)", "chưa xác nhận chấp thuận")
                        .Replace("NOT_ACCEPTED_BY_START_DATE", "Quá ngày bắt đầu mà nhân viên chưa xác nhận chấp thuận");
                }
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to clean up legacy notification messages.");
        }
    }
}
