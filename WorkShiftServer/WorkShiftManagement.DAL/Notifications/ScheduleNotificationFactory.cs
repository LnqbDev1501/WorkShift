using System.Globalization;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Notifications;

public static class ScheduleNotificationFactory
{
    public static Notification RegistrationOpened(WorkScheduleWeek week, Guid actor,
        IEnumerable<Guid> recipients, DateTime timestamp) => Create(actor, recipients, timestamp,
        "ScheduleRegistration", "Shift registration opened",
        $"Registration is open for schedule #{week.ScheduleWeekId}, " +
        $"{week.WeekStartDate:dd/MM/yyyy}–{week.WeekEndDate:dd/MM/yyyy}. " +
        $"Register before {LocalTime(week.RegistrationDeadline)} (UTC+7).");

    public static Notification SwapChanged(ShiftSwapRequest swap, Guid actor, DateTime timestamp)
    {
        var (title, message) = swap.Status switch
        {
            "PENDING" => ("New shift swap request", "You have received a shift swap request."),
            "ACCEPTED" => ("Shift swap accepted", "The shift swap was accepted and both assignments have been exchanged."),
            "REJECTED" => ("Shift swap rejected", "The shift swap request was rejected."),
            "CANCELLED" => ("Shift swap cancelled", "The shift swap request was cancelled."),
            _ => throw new ArgumentException("Unsupported shift swap notification status.")
        };
        var recipients = swap.Status == "PENDING"
            ? new[] { swap.TargetUserId }
            : new[] { swap.RequesterId, swap.TargetUserId };
        var note = swap.Status == "PENDING" ? swap.Reason : swap.ResponseNote;
        return Create(actor, recipients, timestamp, "ShiftSwap", title,
            $"{message} Request #{swap.SwapRequestId}: " +
            $"{Describe(swap.SourceAssignment.Shift)} ↔ {Describe(swap.TargetAssignment.Shift)}." +
            (string.IsNullOrWhiteSpace(note) ? "" : $" Note: {note}"));
    }

    public static Notification SwapCompletedForManagers(ShiftSwapRequest swap, Guid actor,
        IEnumerable<Guid> managers, DateTime timestamp)
    {
        if (swap.Status != "ACCEPTED")
            throw new ArgumentException("Manager notifications require a completed shift swap.");
        var notification = Create(actor, managers, timestamp, "ShiftSwap", "Staff shift swap completed",
            $"{swap.Requester.FullName} and {swap.TargetUser.FullName} have completed shift swap #{swap.SwapRequestId}: " +
            $"{Describe(swap.SourceAssignment.Shift)} ↔ {Describe(swap.TargetAssignment.Shift)}. " +
            $"{swap.Requester.FullName} now works shift #{swap.TargetAssignment.Shift.ShiftId}; " +
            $"{swap.TargetUser.FullName} now works shift #{swap.SourceAssignment.Shift.ShiftId}.");
        notification.TargetType = "Manager";
        return notification;
    }

    private static string Describe(WorkShift shift) =>
        $"{shift.ShiftNameSnapshot} (shift #{shift.ShiftId}), " +
        $"{LocalTime(shift.ScheduledStartAt)}–{LocalTime(shift.ScheduledEndAt)} (UTC+7)";

    private static string LocalTime(DateTime timestamp) =>
        timestamp.AddHours(7).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    private static Notification Create(Guid actor, IEnumerable<Guid> recipients, DateTime timestamp,
        string type, string title, string content) => new()
    {
        Title = title, Content = content, NotificationType = type,
        CreatedBy = actor, CreatedAt = timestamp, UpdatedAt = timestamp,
        TargetType = "Staff", TargetRoles = [], Channels = ["InApp"],
        UserNotifications = recipients.Distinct().Select(id => new UserNotification
        {
            UserId = id, CreatedAt = timestamp, IsRead = false, IsDeleted = false
        }).ToList()
    };
}
