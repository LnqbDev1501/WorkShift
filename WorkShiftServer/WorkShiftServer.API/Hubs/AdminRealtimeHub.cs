using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WorkShiftServer.API.Hubs;

[AllowAnonymous]
public class AdminRealtimeHub : Hub
{
    public const string AdminGroup = "ADMIN_GROUP";

    public Task JoinAdminGroup()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
    }

    public Task LeaveAdminGroup()
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminGroup);
    }
}
