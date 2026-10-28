using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftServer.API.Hubs;

// Payroll closure events must never be sent through the anonymous admin realtime hub.
[Authorize(Roles = "ADMIN,ACCOUNTANT,MANAGER,STAFF")]
public sealed class PayrollNotificationHub(IPayrollCalculationRepo repo) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (!Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub"), out var id)) { Context.Abort(); return; }
        var u = await repo.GetActorAsync(id, Context.ConnectionAborted);
        if (u == null || u.Status != "ACTIVE" || u.DeletedAt != null || u.Role.Status != "ACTIVE"
            || u.Role.RoleName is not ("ADMIN" or "ACCOUNTANT" or "MANAGER" or "STAFF")) { Context.Abort(); return; }
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Payroll:user:{id}");
        if (u.Role.RoleName == "ADMIN") await Groups.AddToGroupAsync(Context.ConnectionId, "Payroll:ADMIN");
        else if (u.BranchId.HasValue) await Groups.AddToGroupAsync(Context.ConnectionId, $"Payroll:{u.Role.RoleName}:{u.BranchId}");
        await base.OnConnectedAsync();
    }
}
