using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class NotificationRepo : INotificationRepo
{
    private readonly AppDbContext _context;

    public NotificationRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<WorkShiftManagement.DAL.Entities.Notification> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Include(notification => notification.TargetRegion)
            .Include(notification => notification.TargetBranch)
            .Where(notification => !notification.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(notification =>
                EF.Functions.ILike(notification.Title, pattern) ||
                EF.Functions.ILike(notification.Content, pattern));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(notification => notification.NotificationType == type.Trim());
        }

        if (dateFrom.HasValue)
        {
            var fromUtc = ConvertVnDateToUtcStartOfDay(DateOnly.FromDateTime(dateFrom.Value));
            query = query.Where(notification => notification.CreatedAt >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtcExclusive = ConvertVnDateToUtcStartOfNextDay(DateOnly.FromDateTime(dateTo.Value));
            query = query.Where(notification => notification.CreatedAt < toUtcExclusive);
        }

        query = (sortBy, sortOrder) switch
        {
            ("Title", "asc") => query.OrderBy(notification => notification.Title),
            ("Title", "desc") => query.OrderByDescending(notification => notification.Title),
            (_, "asc") => query.OrderBy(notification => notification.CreatedAt),
            _ => query.OrderByDescending(notification => notification.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public Task<WorkShiftManagement.DAL.Entities.Notification?> GetByIdAsync(int notificationId)
    {
        return _context.Notifications
            .AsNoTracking()
            .Include(notification => notification.TargetRegion)
            .Include(notification => notification.TargetBranch)
            .Include(notification => notification.CreatedByNavigation)
            .Include(notification => notification.UserNotifications)
            .FirstOrDefaultAsync(notification => notification.NotificationId == notificationId && !notification.IsDeleted);
    }

    public Task<User?> GetActiveAdminByIdAsync(Guid userId)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.UserId == userId &&
                user.Status == "ACTIVE" &&
                user.DeletedAt == null &&
                user.Role.RoleName == "ADMIN");
    }

    public Task<User?> GetActiveAdminByEmailAsync(string email)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Email == email &&
                user.Status == "ACTIVE" &&
                user.DeletedAt == null &&
                user.Role.RoleName == "ADMIN");
    }

    public Task<User?> GetAnyActiveAdminAsync()
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Status == "ACTIVE" &&
                user.DeletedAt == null &&
                user.Role.RoleName == "ADMIN");
    }

    public Task<bool> IsActiveRegionAsync(int regionId)
    {
        return _context.Regions.AnyAsync(region => region.RegionId == regionId && region.IsActive);
    }

    public Task<bool> IsActiveBranchAsync(int branchId)
    {
        return _context.Branches.AnyAsync(branch =>
            branch.BranchId == branchId &&
            branch.Status == "ACTIVE" &&
            branch.Region.IsActive);
    }

    public Task<List<User>> GetRecipientsAsync(IReadOnlyCollection<string> targetRoles, int? regionId, int? branchId)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(user => user.Status == "ACTIVE"
                        && user.DeletedAt == null
                        && targetRoles.Contains(user.Role.RoleName)
                        && (user.Branch == null || (user.Branch.Status == "ACTIVE" && user.Branch.Region.IsActive)));

        if (regionId.HasValue)
        {
            query = query.Where(user => user.Branch != null && user.Branch.RegionId == regionId.Value);
        }

        if (branchId.HasValue)
        {
            query = query.Where(user => user.BranchId == branchId.Value);
        }

        return query.ToListAsync();
    }

    public async Task<NotificationAudienceOptionsData> GetAudienceOptionsAsync()
    {
        return new NotificationAudienceOptionsData
        {
            Regions = await _context.Regions
                .AsNoTracking()
                .Where(region => region.IsActive)
                .OrderBy(region => region.RegionName)
                .Select(region => new NotificationAudienceRegionOptionData
                {
                    RegionId = region.RegionId,
                    RegionName = region.RegionName
                })
                .ToListAsync(),
            Branches = await _context.Branches
                .AsNoTracking()
                .Where(branch => branch.Status == "ACTIVE" && branch.Region.IsActive)
                .OrderBy(branch => branch.BranchName)
                .Select(branch => new NotificationAudienceBranchOptionData
                {
                    BranchId = branch.BranchId,
                    BranchName = branch.BranchName,
                    RegionId = branch.RegionId
                })
                .ToListAsync()
        };
    }


    public async Task CreateAsync(WorkShiftManagement.DAL.Entities.Notification notification, IReadOnlyCollection<Guid> recipientIds, bool createInAppNotifications)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        if (createInAppNotifications)
        {
            var userNotifications = recipientIds.Select(userId => new UserNotification
            {
                NotificationId = notification.NotificationId,
                UserId = userId,
                IsRead = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.UserNotifications.AddRange(userNotifications);
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
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
