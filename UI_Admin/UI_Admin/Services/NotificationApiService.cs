using System.Net.Http.Json;
using UI_Admin.Models;

namespace UI_Admin.Services;

public sealed class NotificationApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public NotificationApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<NotificationListPageViewModel> GetPagedAsync(
        NotificationListPageViewModel query,
        CancellationToken cancellationToken)
    {
        var parameters = new List<string>
        {
            $"page={query.Page}",
            $"pageSize={query.PageSize}",
            $"sortBy={Uri.EscapeDataString(query.SortBy)}",
            $"sortOrder={Uri.EscapeDataString(query.SortOrder)}"
        };

        AddParameter(parameters, "search", query.Search);
        AddParameter(parameters, "type", query.Type);
        AddParameter(parameters, "dateFrom", query.DateFrom?.ToString("O"));
        AddParameter(parameters, "dateTo", query.DateTo?.ToString("O"));

        using var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/admin/notifications?{string.Join('&', parameters)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<NotificationListPageViewModel>(cancellationToken);
        return result ?? new NotificationListPageViewModel();
    }

    public async Task<NotificationDetailViewModel?> GetByIdAsync(int notificationId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/admin/notifications/{notificationId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NotificationDetailViewModel>(cancellationToken);
    }

    public async Task<NotificationAudienceOptionsViewModel> GetAudienceOptionsAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/admin/notifications/audience-options",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NotificationAudienceOptionsViewModel>(cancellationToken)
            ?? new NotificationAudienceOptionsViewModel();
    }

    public async Task<BroadcastResultViewModel> BroadcastAsync(
        SendNotificationViewModel request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/admin/notifications/broadcast",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BroadcastResultViewModel>(cancellationToken)
            ?? throw new InvalidOperationException("The notification API returned an empty response.");
    }

    private static void AddParameter(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
    }
}
