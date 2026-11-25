using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Notification;

namespace WorkShiftManagement.DAL.Repositories.Notification;

public class PersonalNotificationRepo : IPersonalNotificationRepo
{
    private readonly AppDbContext _context;

    public PersonalNotificationRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<UserNotification> Items, int TotalCount)> GetUserNotificationsAsync(
        Guid userId,
        string? search,
        string? filter,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder)
    {
        var query = _context.UserNotifications
            .AsNoTracking()
            .Include(un => un.Notification)
                .ThenInclude(n => n.TargetRegion)
            .Include(un => un.Notification)
                .ThenInclude(n => n.TargetBranch)
            .Include(un => un.Notification)
                .ThenInclude(n => n.CreatedByNavigation)
            .Where(un => un.UserId == userId && !un.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(un =>
                EF.Functions.ILike(un.Notification.Title, pattern) ||
                EF.Functions.ILike(un.Notification.Content, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = filter switch
            {
                "unread" => query.Where(un => !un.IsRead),
                "read" => query.Where(un => un.IsRead),
                _ => query
            };
        }

        if (dateFrom.HasValue)
        {
            var fromUtc = ConvertVnDateToUtcStartOfDay(DateOnly.FromDateTime(dateFrom.Value));
            query = query.Where(un => un.CreatedAt >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtcExclusive = ConvertVnDateToUtcStartOfNextDay(DateOnly.FromDateTime(dateTo.Value));
            query = query.Where(un => un.CreatedAt < toUtcExclusive);
        }

        query = (sortBy, sortOrder) switch
        {
            ("Title", "asc") => query.OrderBy(un => un.Notification.Title),
            ("Title", "desc") => query.OrderByDescending(un => un.Notification.Title),
            (_, "asc") => query.OrderBy(un => un.CreatedAt),
            _ => query.OrderByDescending(un => un.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public async Task<UserNotification?> GetUserNotificationByIdAsync(Guid userId, int userNotificationId)
    {
        return await _context.UserNotifications
            .AsNoTracking()
            .Include(un => un.Notification)
                .ThenInclude(n => n.TargetRegion)
            .Include(un => un.Notification)
                .ThenInclude(n => n.TargetBranch)
            .Include(un => un.Notification)
                .ThenInclude(n => n.CreatedByNavigation)
            .FirstOrDefaultAsync(un => un.UserNotificationId == userNotificationId
                && un.UserId == userId
                && !un.IsDeleted);
    }

    public async Task<int> CountRecipientsAsync(int notificationId)
    {
        return await _context.UserNotifications
            .AsNoTracking()
            .CountAsync(un => un.NotificationId == notificationId && !un.IsDeleted);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, int userNotificationId)
    {
        var userNotification = await _context.UserNotifications
            .FirstOrDefaultAsync(un => un.UserNotificationId == userNotificationId
                && un.UserId == userId
                && !un.IsDeleted);

        if (userNotification == null || userNotification.IsRead)
        {
            return false;
        }

        userNotification.IsRead = true;
        userNotification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var unreadNotifications = await _context.UserNotifications
            .Where(un => un.UserId == userId && !un.IsDeleted && !un.IsRead)
            .ToListAsync();

        if (unreadNotifications.Count == 0)
        {
            return 0;
        }

        foreach (var un in unreadNotifications)
        {
            un.IsRead = true;
            un.ReadAt = now;
        }

        await _context.SaveChangesAsync();
        return unreadNotifications.Count;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.UserNotifications
            .AsNoTracking()
            .CountAsync(un => un.UserId == userId && !un.IsDeleted && !un.IsRead);
    }

    public async Task<(List<WorkShiftManagement.DAL.Entities.Notification> Items, int TotalCount)> GetBroadcastHistoryAsync(
        Guid userId,
        string? search,
        string? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder)
    {
        var userNotificationIds = _context.UserNotifications
            .AsNoTracking()
            .Where(un => un.UserId == userId && !un.IsDeleted)
            .Select(un => un.NotificationId);

        var query = _context.Notifications
            .AsNoTracking()
            .Include(n => n.TargetRegion)
            .Include(n => n.TargetBranch)
            .Include(n => n.CreatedByNavigation)
            .Include(n => n.UserNotifications)
            .Where(n => !n.IsDeleted && userNotificationIds.Contains(n.NotificationId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.Title, pattern) ||
                EF.Functions.ILike(n.Content, pattern));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(n => n.NotificationType == type.Trim());
        }

        if (dateFrom.HasValue)
        {
            var fromUtc = ConvertVnDateToUtcStartOfDay(DateOnly.FromDateTime(dateFrom.Value));
            query = query.Where(n => n.CreatedAt >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtcExclusive = ConvertVnDateToUtcStartOfNextDay(DateOnly.FromDateTime(dateTo.Value));
            query = query.Where(n => n.CreatedAt < toUtcExclusive);
        }

        query = (sortBy, sortOrder) switch
        {
            ("Title", "asc") => query.OrderBy(n => n.Title),
            ("Title", "desc") => query.OrderByDescending(n => n.Title),
            (_, "asc") => query.OrderBy(n => n.CreatedAt),
            _ => query.OrderByDescending(n => n.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public async Task<WorkShiftManagement.DAL.Entities.Notification?> GetBroadcastByIdAsync(Guid userId, int notificationId)
    {
        var userHasNotification = await _context.UserNotifications
            .AsNoTracking()
            .AnyAsync(un => un.UserId == userId && un.NotificationId == notificationId && !un.IsDeleted);

        if (!userHasNotification)
        {
            return null;
        }

        return await _context.Notifications
            .AsNoTracking()
            .Include(n => n.TargetRegion)
            .Include(n => n.TargetBranch)
            .Include(n => n.CreatedByNavigation)
            .Include(n => n.UserNotifications)
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && !n.IsDeleted);
    }

    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    private static DateTime ConvertVnDateToUtcStartOfDay(DateOnly date)
    {
        var vnDateTime = date.ToDateTime(TimeOnly.MinValue);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(vnDateTime, DateTimeKind.Unspecified), VietnamTimeZone);
    }

    private static DateTime ConvertVnDateToUtcStartOfNextDay(DateOnly date) =>
        ConvertVnDateToUtcStartOfDay(date.AddDays(1));

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("UTC+07:00", TimeSpan.FromHours(7), "Vietnam Standard Time", "Vietnam Standard Time");
            }
        }
    }
}