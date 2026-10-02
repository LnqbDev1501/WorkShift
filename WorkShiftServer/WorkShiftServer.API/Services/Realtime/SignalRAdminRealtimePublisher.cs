using Microsoft.AspNetCore.SignalR;
using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftServer.API.Hubs;

namespace WorkShiftServer.API.Services.Realtime;

public class SignalRAdminRealtimePublisher : IAdminRealtimePublisher
{
    private readonly IHubContext<AdminRealtimeHub> _hubContext;
    private readonly ILogger<SignalRAdminRealtimePublisher> _logger;

    public SignalRAdminRealtimePublisher(
        IHubContext<AdminRealtimeHub> hubContext,
        ILogger<SignalRAdminRealtimePublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishDataChangedAsync(string eventName, object payload)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync(eventName, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast realtime data sync event {EventName}", eventName);
        }
    }
}
