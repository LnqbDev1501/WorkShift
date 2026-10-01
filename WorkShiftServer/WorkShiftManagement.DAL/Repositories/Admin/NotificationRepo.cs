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

    public async Task<(List<Notification> Items, int TotalCount)> GetPagedAsync(
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
            query = query.Where(notification => notification.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(notification => notification.CreatedAt < dateTo.Value.Date.AddDays(1));
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

    public Task<Notification?> GetByIdAsync(int notificationId)
    {
        return _context.Notifications
            .AsNoTracking()
            .Include(notification => notification.TargetRegion)
            .Include(notification => notification.TargetBranch)
            .Include(notification => notification.CreatedByNavigation)
            .Include(notification => notification.UserNotifications)
            .FirstOrDefaultAsync(notification => notification.NotificationId == notificationId && !notification.IsDeleted);
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
                        && (user.Role.RoleName != "STAFF" || (user.EmploymentContractUser != null && user.EmploymentContractUser.Status == "ACTIVE")));

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


    public async Task CreateAsync(Notification notification, IReadOnlyCollection<Guid> recipientIds, bool createInAppNotifications)
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
}
