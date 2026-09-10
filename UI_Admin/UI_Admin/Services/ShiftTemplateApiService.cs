using System.Text.Json;
using UI_Admin.Models;

namespace UI_Admin.Services;

public sealed class ShiftTemplateApiService(HttpClient httpClient, IConfiguration configuration)
{
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
