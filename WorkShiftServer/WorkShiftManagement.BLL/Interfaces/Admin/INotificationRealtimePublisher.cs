using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface INotificationRealtimePublisher
{
    Task PublishAsync(Guid userId, RealtimeNotificationResponse notification);
}
