using System.Globalization;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Rules;

namespace WorkShiftManagement.DAL.Notifications;

public static class WorkingTimeRuleNotificationFactory
{
    public static Notification Create(WorkingTimeRule rule, Guid actor, IEnumerable<Guid> recipients, DateTime timestamp)
    {
        var cancelled = rule.Status switch
        {
            WorkingTimeRuleLifecycle.Published => false,
            WorkingTimeRuleLifecycle.Cancelled => true,
            _ => throw new ArgumentException("Only published or cancelled rules can be announced.")
        };
        var date = rule.EffectiveFrom?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var daily = (rule.MaxDailyMinutes / 60m).ToString("0.##", CultureInfo.InvariantCulture);
        var weekly = (rule.MaxWeeklyMinutes / 60m).ToString("0.##", CultureInfo.InvariantCulture);
        return new Notification
        {
            Title = cancelled ? "Working time rule cancelled" : "Working time rule published",
            Content = $"Working time rule V{rule.WorkingTimeRuleId} ({rule.RuleName}) " +
                (cancelled ? $"scheduled for {date} has been cancelled."
                    : $"has been published, effective from {date}. Maximum working time: {daily} hours/day, {weekly} hours/week."),
            NotificationType = "WorkingTimeRule", TargetType = "All", TargetRoles = [], Channels = ["InApp"],
            CreatedBy = actor, CreatedAt = timestamp, UpdatedAt = timestamp,
            UserNotifications = recipients.Distinct().Select(id => new UserNotification
            {
                UserId = id, CreatedAt = timestamp, IsRead = false, IsDeleted = false
            }).ToList()
        };
    }
}
