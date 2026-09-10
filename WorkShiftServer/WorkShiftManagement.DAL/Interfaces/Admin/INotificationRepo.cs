using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface INotificationRepo
{
    Task<(List<Notification> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder);

    Task<Notification?> GetByIdAsync(int notificationId);

    Task<User?> GetActiveAdminByEmailAsync(string email);

    Task<bool> IsActiveRegionAsync(int regionId);

    Task<bool> IsActiveBranchAsync(int branchId);

    Task<List<User>> GetRecipientsAsync(string targetType, int? regionId, int? branchId);

    Task CreateAsync(Notification notification, IReadOnlyCollection<Guid> recipientIds, bool createInAppNotifications);
}
