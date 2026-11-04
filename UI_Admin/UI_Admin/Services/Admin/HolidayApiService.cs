using UI_Admin.Services;
using System.Net.Http.Json;
using System.Text.Json;
using UI_Admin.Models.Admin.Holidays;

namespace UI_Admin.Services.Admin;

public sealed class HolidayApiService(HttpClient httpClient, IConfiguration configuration)
{
    private string BaseUrl => configuration.GetWorkShiftApiBaseUrl();

    public async Task<List<HolidayRuleViewModel>> GetRulesAsync(CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<List<HolidayRuleViewModel>>($"{BaseUrl}/api/holiday-pay-rules", ct)
        ?? throw new JsonException("Unable to load pay rules.");

    public async Task<(bool Success, string? Error)> TransitionRuleAsync(int id, bool publish, CancellationToken ct)
    {
        using var response = await httpClient.PostAsync($"{BaseUrl}/api/holiday-pay-rules/{id}/{(publish ? "publish" : "cancel")}", null, ct);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(response, ct));
    }

    public async Task<(bool Success, string? Error)> SaveRuleAsync(HolidayRuleViewModel model, CancellationToken ct)
    {
        var payload = new { model.RuleName, model.PayMultiplier, model.EffectiveFrom };
        using var response = model.HolidayPayRuleId == 0
            ? await httpClient.PostAsJsonAsync($"{BaseUrl}/api/holiday-pay-rules", payload, ct)
            : await httpClient.PutAsJsonAsync($"{BaseUrl}/api/holiday-pay-rules/{model.HolidayPayRuleId}", payload, ct);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(response, ct));
    }

    public async Task<(bool Success, string? Error)> DeleteRuleAsync(int id, CancellationToken ct)
    {
        using var response = await httpClient.DeleteAsync($"{BaseUrl}/api/holiday-pay-rules/{id}", ct);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(response, ct));
    }

    public async Task<HolidayPageViewModel> GetPageAsync(
        HolidayListQueryViewModel query, CancellationToken cancellationToken = default)
    {
        var url = BuildListUrl(query);
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HolidayPageViewModel>(cancellationToken: cancellationToken)
            ?? throw new JsonException("The public holiday API returned an empty response.");
    }

    public async Task<List<HolidayItemViewModel>> GetAllAsync(
        int? year, int? month, string? search, bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var items = new List<HolidayItemViewModel>();
        var pageNumber = 1;
        do
        {
            var page = await GetPageAsync(new HolidayListQueryViewModel
            {
                PageNumber = pageNumber,
                Year = year,
                Month = month,
                Search = search,
                IncludeInactive = includeInactive
            }, cancellationToken);
            items.AddRange(page.Items);
            if (pageNumber >= page.TotalPages || page.Items.Count == 0) break;
            pageNumber++;
        } while (true);
        return items;
    }

    public async Task<HolidayItemViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"{BaseUrl}/api/public-holidays/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HolidayItemViewModel>(cancellationToken: cancellationToken)
            ?? throw new JsonException("The public holiday API returned an empty response.");
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        CreateHolidaysViewModel model, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            holidays = model.Holidays.SelectMany(item => Enumerable.Range(0,
                item.ToDate!.Value.DayNumber - item.FromDate!.Value.DayNumber + 1).Select(offset => new
                {
                    holidayName = item.HolidayName.Trim(),
                    holidayDate = item.FromDate.Value.AddDays(offset)

                }))
        };
        using var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/api/public-holidays", payload, cancellationToken);
        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await ReadErrorAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        int id, EditHolidayViewModel model, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            holidayName = model.HolidayName.Trim(),
            holidayDate = model.HolidayDate

        };
        using var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/api/public-holidays/{id}", payload, cancellationToken);
        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await ReadErrorAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"{BaseUrl}/api/public-holidays/{id}", cancellationToken);
        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await ReadErrorAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? Error)> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PatchAsync($"{BaseUrl}/api/public-holidays/{id}/restore", null, cancellationToken);
        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await ReadErrorAsync(response, cancellationToken));
    }

    private string BuildListUrl(HolidayListQueryViewModel query)
    {
        var parameters = new List<string> { $"pageNumber={query.PageNumber}" };
        if (query.Year.HasValue) parameters.Add($"year={query.Year.Value}");
        if (query.Month.HasValue) parameters.Add($"month={query.Month.Value}");
        if (!string.IsNullOrWhiteSpace(query.Search))
            parameters.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        if (query.IncludeInactive) parameters.Add("includeInactive=true");
        if (query.IsActive.HasValue) parameters.Add($"isActive={query.IsActive.Value.ToString().ToLowerInvariant()}");
        return $"{BaseUrl}/api/public-holidays?{string.Join('&', parameters)}";
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail)) return problem.Detail;
            if (!string.IsNullOrWhiteSpace(problem?.Title)) return problem.Title;
        }
        catch (JsonException) { }
        return $"Request failed with status code {(int)response.StatusCode}.";
    }

    private sealed class ApiProblem
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}


