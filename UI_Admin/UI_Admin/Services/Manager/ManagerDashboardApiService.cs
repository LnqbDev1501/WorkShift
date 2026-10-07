using System.Text.Json;
using UI_Admin.Models.Manager;

namespace UI_Admin.Services.Manager;

public sealed class ManagerDashboardApiService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<ManagerDashboardData> GetAsync(CancellationToken cancellationToken)
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("WorkShiftApi:BaseUrl must be a valid HTTP or HTTPS URL.");

        using var response = await httpClient.GetAsync($"{baseUrl!.TrimEnd('/')}/api/manager/dashboard", cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<ManagerDashboardData>(cancellationToken)
            ?? throw new JsonException("The dashboard API returned an empty response.");
        if (data.TodayAttendance is null || data.TodayAttendance.ByStatus is null || data.PendingRequestsByType is null || data.RecentRequests is null)
            throw new JsonException("The dashboard API returned an invalid response.");
        return data;
    }
}

