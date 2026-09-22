using System.Net;
using System.Net.Http.Json;
using UI_Admin.Models.RegionalMinimumWages;

namespace UI_Admin.Services;

public class RegionalMinimumWageApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public RegionalMinimumWageApiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');
    }

    public async Task<List<RegionalMinimumWageListItemViewModel>> GetAllAsync(
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            queryParams.Add($"status={Uri.EscapeDataString(status.Trim().ToUpperInvariant())}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var url = $"{_apiBaseUrl}/api/admin/regional-minimum-wages{queryString}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        var list = await response.Content.ReadFromJsonAsync<List<RegionalMinimumWageListItemViewModel>>(cancellationToken: cancellationToken);
        return list ?? [];
    }

    public async Task<RegionalMinimumWageDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regional-minimum-wages/{id}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<RegionalMinimumWageDetailViewModel>(cancellationToken: cancellationToken);
    }

    public async Task<(bool Success, RegionalMinimumWageDetailViewModel? Data, string? ErrorMessage, bool IsConflict)> CreateAsync(
        CreateRegionalMinimumWageViewModel model,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regional-minimum-wages";
        var payload = new
        {
            wageName = model.WageName.Trim(),
            minimumHourlyRate = model.MinimumHourlyRate,
            description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            replacedWageId = model.ReplacedWageId,
            effectiveFrom = model.EffectiveFrom
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<RegionalMinimumWageDetailViewModel>(cancellationToken: cancellationToken);
            return (true, data, null, false);
        }

        var isConflict = response.StatusCode == HttpStatusCode.Conflict;
        var error = await ReadErrorMessageAsync(response, cancellationToken);
        return (false, null, error, isConflict);
    }

    public async Task<(bool Success, RegionalMinimumWageDetailViewModel? Data, string? ErrorMessage, bool IsConflict, bool IsNotFound)> UpdateAsync(
        int id,
        UpdateRegionalMinimumWageViewModel model,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regional-minimum-wages/{id}";
        var payload = new
        {
            wageName = model.WageName.Trim(),
            minimumHourlyRate = model.MinimumHourlyRate,
            description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            replacedWageId = model.ReplacedWageId,
            effectiveFrom = model.EffectiveFrom
        };

        using var response = await _httpClient.PutAsJsonAsync(url, payload, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<RegionalMinimumWageDetailViewModel>(cancellationToken: cancellationToken);
            return (true, data, null, false, false);
        }

        var isConflict = response.StatusCode == HttpStatusCode.Conflict;
        var isNotFound = response.StatusCode == HttpStatusCode.NotFound;
        var error = await ReadErrorMessageAsync(response, cancellationToken);
        return (false, null, error, isConflict, isNotFound);
    }

    public async Task<(bool Success, RegionalMinimumWageDetailViewModel? Data, string? ErrorMessage)> PublishAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regional-minimum-wages/{id}/publish";
        using var response = await _httpClient.PostAsync(url, null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<RegionalMinimumWageDetailViewModel>(cancellationToken: cancellationToken);
            return (true, data, null);
        }

        var error = await ReadErrorMessageAsync(response, cancellationToken);
        return (false, null, error);
    }

    public async Task<(bool Success, RegionalMinimumWageDetailViewModel? Data, string? ErrorMessage)> CancelAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regional-minimum-wages/{id}/cancel";
        using var response = await _httpClient.PostAsync(url, null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<RegionalMinimumWageDetailViewModel>(cancellationToken: cancellationToken);
            return (true, data, null);
        }

        var error = await ReadErrorMessageAsync(response, cancellationToken);
        return (false, null, error);
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return problem.Detail;
            if (!string.IsNullOrWhiteSpace(problem?.Title))
                return problem.Title;
        }
        catch { }

        return $"Request failed with status code {(int)response.StatusCode}.";
    }

    private class ProblemDetailsResponse
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}
