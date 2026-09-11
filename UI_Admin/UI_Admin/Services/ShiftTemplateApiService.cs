using System.Text.Json;
using UI_Admin.Models;

namespace UI_Admin.Services;

public sealed class ShiftTemplateApiService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<(bool Success, string? Error, bool DuplicateName)> CreateAsync(
        CreateShiftTemplateViewModel model, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
        using var response = await httpClient.PostAsJsonAsync(
            $"{baseUrl.TrimEnd('/')}/api/admin/shift-templates", new
            {
                shiftName = model.ShiftName?.Trim(), model.StartTime, model.EndTime,
                model.BreakMinutes, description = model.Description?.Trim()
            }, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Created)
            return (true, null, false);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, "A shift template with this name already exists.", true);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken);
            return (false, error?.Message ?? "The template could not be saved. Please check the entered values.", false);
        }
        response.EnsureSuccessStatusCode();
        throw new HttpRequestException("Unexpected response while creating a shift template.");
    }

    private sealed class ApiError
    {
        public string? Message { get; set; }
    }

    public async Task<ShiftTemplatePage> GetAllAsync(
        ShiftTemplateListQueryViewModel query, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
        var url = $"{baseUrl.TrimEnd('/')}/api/admin/shift-templates?page={query.Page}";
        if (!string.IsNullOrWhiteSpace(query.Search))
            url += $"&search={Uri.EscapeDataString(query.Search.Trim())}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShiftTemplatePage>(cancellationToken: cancellationToken)
            ?? throw new JsonException("The shift template API returned an empty response.");
    }
}
