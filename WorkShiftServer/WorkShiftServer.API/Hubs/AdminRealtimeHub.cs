using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WorkShiftServer.API.Hubs;

[AllowAnonymous]
public class AdminRealtimeHub : Hub
{
}
