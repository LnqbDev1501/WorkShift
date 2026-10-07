using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using UI_Admin.Models.Manager;

namespace UI_Admin.Services.Manager;

public sealed class ManagerScheduleApiService(HttpClient client, IConfiguration configuration)
{
    public async Task RegistrationAsync(int id, bool open, CancellationToken ct)
    {
        using var response = await client.PostAsync(BaseUrl() + $"/api/manager/schedule-weeks/{id}/{(open ? "open-registration" : "close-registration")}", null, ct);
        await CheckMutationAsync(response, ct);
    }
    public Task<ScheduleWeekDetailResponse> SaveAsync(ManagerScheduleEditViewModel model, CancellationToken ct) =>
        SaveCoreAsync(model, ct);

    private async Task<ScheduleWeekDetailResponse> SaveCoreAsync(ManagerScheduleEditViewModel model, CancellationToken ct)
    {
        var deadline = new DateTimeOffset(DateTime.SpecifyKind(model.RegistrationDeadline!.Value, DateTimeKind.Unspecified), TimeSpan.FromHours(7));
        using var request = new HttpRequestMessage(model.Id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            BaseUrl() + "/api/manager/schedule-weeks" + (model.Id.HasValue ? $"/{model.Id}" : ""))
        {
            Content = JsonContent.Create(new { WeekStartDate = model.WeekStartDate!.Value, RegistrationDeadline = deadline })
        };
        using var response = await client.SendAsync(request, ct);
        await CheckMutationAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ScheduleWeekDetailResponse>(ct) ?? throw new JsonException("Empty schedule response.");
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await client.DeleteAsync(BaseUrl() + $"/api/manager/schedule-weeks/{id}", ct);
        await CheckMutationAsync(response, ct);
    }

    private static async Task CheckMutationAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ApiErrorReader.ReadErrorMessageAsync(response, ct), null, response.StatusCode);
    }

    private string BaseUrl()
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
            throw new InvalidOperationException("Invalid API URL.");
        return baseUrl!.TrimEnd('/');
    }
    public Task<List<ScheduleWeekSummaryResponse>> GetListAsync(ManagerScheduleFilter filter, CancellationToken ct) =>
        GetAsync<List<ScheduleWeekSummaryResponse>>(QueryHelpers.AddQueryString("/api/manager/schedule-weeks", new Dictionary<string, string?>
        {
            ["fromDate"] = filter.FromDate?.ToString("yyyy-MM-dd"),
            ["toDate"] = filter.ToDate?.ToString("yyyy-MM-dd"), ["status"] = filter.Status
        }), ct);

    public async Task<ScheduleWeekDetailResponse> GetDetailAsync(int id, CancellationToken ct)
    {
        var week = await GetAsync<ScheduleWeekDetailResponse>($"/api/manager/schedule-weeks/{id}", ct);
        if (week.Shifts is null || week.Shifts.Any(shift => shift.EmployeeRequirements is null))
            throw new JsonException("Invalid schedule response.");
        return week;
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await client.GetAsync(BaseUrl() + path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new JsonException("Empty schedule response.");
    }
}
