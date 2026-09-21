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

    public async Task<List<RegionalMinimumWageListItemViewModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regional-minimum-wages";
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
            description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
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
            description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
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
