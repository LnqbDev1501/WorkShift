using Microsoft.AspNetCore.SignalR;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftServer.API.Hubs;

namespace WorkShiftServer.API.Services;

public class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationRealtimePublisher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishAsync(Guid userId, RealtimeNotificationResponse notification)
    {
        return _hubContext.Clients.Group(userId.ToString()).SendAsync("NotificationReceived", notification);
    }
}
