using System.Text.Json;
using UI_Admin.Models;

namespace UI_Admin.Services;

public sealed class WorkingTimeRuleApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public WorkingTimeRuleApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<WorkingTimeRuleListViewModel> GetListAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(BuildUrl("api/working-time-rules"), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var items = await response.Content.ReadFromJsonAsync<List<WorkingTimeRuleListItemViewModel>>(
            cancellationToken: cancellationToken)
            ?? throw new JsonException("The working time API returned an empty response.");

        return new WorkingTimeRuleListViewModel { Items = items };
    }

    public async Task CreateAsync(WorkingTimeRuleCreateViewModel model, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            RuleName = model.RuleName,
            MaxDailyHours = model.MaxDailyHours,
            MaxWeeklyHours = model.MaxWeeklyHours
        };
        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl("api/working-time-rules"), request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var message = "Backend API request failed. Please try again later.";
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken: cancellationToken);
            message = error?.Errors?.Count > 0
                ? string.Join(" ", error.Errors.Values.SelectMany(errors => errors))
                : error?.Message ?? error?.Detail ?? message;
        }
        catch (JsonException)
        {
            // A proxy or server can return a non-JSON error response.
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private string BuildUrl(string relativeUrl) =>
        $"{_apiBaseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
