using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WorkShiftServer.API.Hubs;

// Payroll closure events must never be sent through the anonymous admin realtime hub.
[Authorize(Roles = "ADMIN")]
public sealed class PayrollNotificationHub : Hub { }
