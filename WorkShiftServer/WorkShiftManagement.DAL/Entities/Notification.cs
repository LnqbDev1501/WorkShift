namespace WorkShiftManagement.DAL.Entities;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string NotificationType { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string TargetType { get; set; } = null!;

    public int? TargetRegionId { get; set; }

    public int? TargetBranchId { get; set; }

    public List<string> Channels { get; set; } = null!;

    public List<string> TargetRoles { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Branch? TargetBranch { get; set; }

    public virtual Region? TargetRegion { get; set; }

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
}
