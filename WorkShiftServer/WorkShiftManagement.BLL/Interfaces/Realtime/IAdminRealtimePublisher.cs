namespace WorkShiftManagement.BLL.Interfaces.Realtime;

public interface IAdminRealtimePublisher
{
    Task PublishDataChangedAsync(string eventName, object payload);
}
