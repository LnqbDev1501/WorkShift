using Microsoft.AspNetCore.Mvc.Filters;
using WorkShiftManagement.BLL.Interfaces.Realtime;

namespace WorkShiftServer.API.Services.Realtime;

// Publish successful workspace mutations through the shared invalidation channel.
public sealed class AdminDataChangedFilter(IAdminRealtimePublisher publisher) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var executed = await next();
        var request = context.HttpContext.Request;
        var controllerNamespace = context.Controller.GetType().Namespace ?? "";
        if (executed.Exception != null || executed.Canceled ||
            context.HttpContext.Response.StatusCode is < 200 or >= 300 ||
            !(HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) ||
              HttpMethods.IsPatch(request.Method) || HttpMethods.IsDelete(request.Method)) ||
            !(controllerNamespace.EndsWith(".Admin") || controllerNamespace.EndsWith(".Catalog") ||
              controllerNamespace.EndsWith(".Accountant") || controllerNamespace.EndsWith(".Profile") ||
              controllerNamespace.EndsWith(".Manager") || controllerNamespace.EndsWith(".Staff") ||
              controllerNamespace.EndsWith(".Notification")))
            return;

        await publisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged, new
        {
            resource = context.RouteData.Values["controller"]?.ToString(),
            action = context.RouteData.Values["action"]?.ToString()
        });
    }
}
