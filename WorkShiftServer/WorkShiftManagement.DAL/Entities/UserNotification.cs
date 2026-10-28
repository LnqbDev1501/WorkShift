namespace WorkShiftManagement.DAL.Entities;

public partial class UserNotification
{
    public int UserNotificationId { get; set; }

    public int NotificationId { get; set; }

    public Guid UserId { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Notification Notification { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
