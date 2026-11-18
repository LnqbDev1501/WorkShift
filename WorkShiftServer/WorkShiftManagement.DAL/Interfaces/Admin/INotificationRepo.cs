using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface INotificationRepo
{
    Task<(List<WorkShiftManagement.DAL.Entities.Notification> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder);

    Task<WorkShiftManagement.DAL.Entities.Notification?> GetByIdAsync(int notificationId);

    Task<User?> GetActiveAdminByIdAsync(Guid userId);

    Task<User?> GetActiveAdminByEmailAsync(string email);

    Task<User?> GetAnyActiveAdminAsync();

    Task<bool> IsActiveRegionAsync(int regionId);

    Task<bool> IsActiveBranchAsync(int branchId);

    Task<List<User>> GetRecipientsAsync(IReadOnlyCollection<string> targetRoles, int? regionId, int? branchId);

    Task<NotificationAudienceOptionsData> GetAudienceOptionsAsync();

    Task CreateAsync(WorkShiftManagement.DAL.Entities.Notification notification, IReadOnlyCollection<Guid> recipientIds, bool createInAppNotifications);
}

public class NotificationAudienceOptionsData
{
    public List<NotificationAudienceRegionOptionData> Regions { get; set; } = [];
    public List<NotificationAudienceBranchOptionData> Branches { get; set; } = [];
}

public class NotificationAudienceRegionOptionData
{
    public int RegionId { get; set; }
    public string RegionName { get; set; } = null!;
}

public class NotificationAudienceBranchOptionData
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public int RegionId { get; set; }
}
