using Microsoft.AspNetCore.SignalR;

namespace WorkShiftServer.API.Hubs;

public class NotificationHub : Hub
{
    public Task RegisterUser(Guid userId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
    }
}
