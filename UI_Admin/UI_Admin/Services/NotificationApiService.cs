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

    public async Task<PersonalNotificationListPageViewModel> GetMyNotificationsAsync(
        PersonalNotificationListPageViewModel query,
        CancellationToken cancellationToken)
    {
        var parameters = new List<string>
        {
            $"page={query.Page}",
            $"pageSize={query.PageSize}",
            $"sortBy={Uri.EscapeDataString(query.SortBy)}",
            $"sortOrder={Uri.EscapeDataString(query.SortOrder)}",
            $"filter={Uri.EscapeDataString(query.Filter)}"
        };

        AddParameter(parameters, "search", query.Search);

        using var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/notifications/personal?{string.Join('&', parameters)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PersonalNotificationListPageViewModel>(cancellationToken);
        return result ?? new PersonalNotificationListPageViewModel();
    }

    public async Task<PersonalNotificationDetailViewModel?> GetMyNotificationByIdAsync(int userNotificationId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/notifications/personal/{userNotificationId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PersonalNotificationDetailViewModel>(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/notifications/personal/unread-count",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UnreadCountResponse>(cancellationToken);
        return result?.Count ?? 0;
    }

    public async Task<bool> MarkAsReadAsync(int userNotificationId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/notifications/personal/{userNotificationId}/read",
            null,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<BroadcastHistoryPageViewModel> GetBroadcastHistoryAsync(
        BroadcastHistoryPageViewModel query,
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
            $"{_apiBaseUrl.TrimEnd('/')}/api/notifications/broadcast?{string.Join('&', parameters)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BroadcastHistoryPageViewModel>(cancellationToken);
        return result ?? new BroadcastHistoryPageViewModel();
    }

    public async Task<PersonalNotificationDetailViewModel?> GetBroadcastByIdAsync(int notificationId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/notifications/broadcast/{notificationId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PersonalNotificationDetailViewModel>(cancellationToken);
    }

    private static void AddParameter(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
    }

    private sealed class UnreadCountResponse
    {
        public int Count { get; set; }
    }
}
