using System.Text.Json;
using UI_Admin.Models;

namespace UI_Admin.Services;

public sealed class ShiftTemplateApiService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<(bool Success, string? Error, bool DuplicateName)> UpdateAsync(
        EditShiftTemplateViewModel model, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
        using var response = await httpClient.PutAsJsonAsync(
            $"{baseUrl.TrimEnd('/')}/api/admin/shift-templates/{model.Id}", new
            {
                shiftName = model.ShiftName?.Trim(),
                model.StartTime,
                model.EndTime,
                description = model.Description?.Trim()
            }, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.OK) return (true, null, false);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, "A shift template with this name already exists.", true);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (false, "This template no longer exists. Your entered values have been kept.", false);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return (false, "Your session has expired. Please sign in again.", false);
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return (false, "You do not have permission to edit this template.", false);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken);
            return (false, error?.Message ?? "Check the entered values and try again.", false);
        }
        response.EnsureSuccessStatusCode();
        throw new HttpRequestException("Unexpected response while updating a shift template.");
    }

    public async Task<ShiftTemplateDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
        using var response = await httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/api/admin/shift-templates/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShiftTemplateDetailViewModel>(cancellationToken: cancellationToken)
            ?? throw new JsonException("The shift template API returned an empty response.");
    }

    public async Task<(bool Success, string Message)> SetStatusAsync(int id, bool restore, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
        var url = $"{baseUrl.TrimEnd('/')}/api/admin/shift-templates/{id}";
        using var response = restore
            ? await httpClient.PostAsync($"{url}/restore", null, cancellationToken)
            : await httpClient.DeleteAsync(url, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (false, "This shift template could not be found. It may have been removed.");
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return (false, "Your session has expired. Please sign in again.");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return (false, "You do not have permission to change template status.");
        if (response.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken);
            return (false, error?.Message ?? "The template status could not be changed. Refresh and try again.");
        }
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken);
        return (true, result?.Message ?? (restore ? "Template restored successfully." : "Template deactivated successfully."));
    }

    public async Task<(bool Success, string? Error, bool DuplicateName)> CreateAsync(
        CreateShiftTemplateViewModel model, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
        using var response = await httpClient.PostAsJsonAsync(
            $"{baseUrl.TrimEnd('/')}/api/admin/shift-templates", new
            {
                shiftName = model.ShiftName?.Trim(),
                model.StartTime,
                model.EndTime,
                description = model.Description?.Trim()
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
        if (!string.IsNullOrEmpty(query.Status))
            url += $"&status={Uri.EscapeDataString(query.Status)}";
        if (query.StartTime.HasValue)
            url += $"&startTime={query.StartTime.Value.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)}";
        if (query.EndTime.HasValue)
            url += $"&endTime={query.EndTime.Value.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShiftTemplatePage>(cancellationToken: cancellationToken)
            ?? throw new JsonException("The shift template API returned an empty response.");
    }
}
