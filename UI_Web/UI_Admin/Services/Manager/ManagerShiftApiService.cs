using UI_Admin.Services;
using System.Net;
using System.Text.Json;
using UI_Admin.Models.Manager;
namespace UI_Admin.Services.Manager;
public sealed class ManagerShiftApiService(HttpClient client, IConfiguration configuration)
{
    private string BaseUrl => configuration.GetWorkShiftApiBaseUrl();
    public async Task<ShiftDetailResponse> GetAsync(int id, CancellationToken ct)
    {
        var shift = await ReadAsync<ShiftDetailResponse>($"/api/manager/shifts/{id}", ct);
        if (shift.Staff is null || shift.EmployeeRequirements is null) throw new JsonException("Invalid shift response.");
        return shift;
    }
    public async Task<ShiftOptionsResponse> OptionsAsync(CancellationToken ct)
    {
        var options = await ReadAsync<ShiftOptionsResponse>("/api/manager/shifts/options", ct);
        if (options.Templates is null || options.EmployeeTypes is null || options.Staff is null) throw new JsonException("Invalid options response.");
        return options;
    }
    private async Task<T> ReadAsync<T>(string path, CancellationToken ct)
    {
        using var response = await client.GetAsync(BaseUrl + path, ct);
        await CheckAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new JsonException("Empty API response.");
    }
    public async Task<ShiftDetailResponse> SaveAsync(ManagerShiftEditViewModel model, CancellationToken ct)
    {
        var path = model.Id.HasValue ? $"/api/manager/shifts/{model.Id}" : $"/api/manager/schedule-weeks/{model.WeekId}/shifts";
        using var request = new HttpRequestMessage(model.Id.HasValue ? HttpMethod.Put : HttpMethod.Post, BaseUrl + path)
        { Content = JsonContent.Create(new { model.ShiftTemplateId, WorkDate = model.WorkDate!.Value, model.Note, model.EmployeeRequirements }) };
        using var response = await client.SendAsync(request, ct);
        await CheckAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ShiftDetailResponse>(ct) ?? throw new JsonException("Empty API response.");
    }
    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await client.DeleteAsync(BaseUrl + $"/api/manager/shifts/{id}", ct);
        await CheckAsync(response, ct);
    }
    public async Task AssignAsync(int id, Guid userId, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(BaseUrl + $"/api/manager/shifts/{id}/assignments", new { UserId = userId }, ct);
        await CheckAsync(response, ct);
    }
    public async Task CancelAsync(int assignmentId, string reason, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(BaseUrl + $"/api/manager/assignments/{assignmentId}/cancel", new { Reason = reason }, ct);
        await CheckAsync(response, ct);
    }
    private static async Task CheckAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await ApiErrorReader.ReadErrorMessageAsync(response, ct), null, response.StatusCode);
    }
    public static bool IsFailure(Exception ex) => ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException;
    public static string Error(Exception ex) => ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.Conflict } ? ex.Message
        : ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound } ? "The requested shift or schedule could not be found."
        : ex is HttpRequestException { StatusCode: HttpStatusCode.Forbidden } ? "You do not have access to this shift or schedule."
        : "The operation could not be confirmed. Refresh the details before trying again.";
}
