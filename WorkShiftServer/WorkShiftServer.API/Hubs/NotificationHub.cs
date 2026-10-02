using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WorkShiftServer.API.Hubs;

[AllowAnonymous]
public class NotificationHub : Hub
{
    public Task RegisterUser(Guid userId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
    }
}
