using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models;
using UI_Admin.Models.PayrollCalculation;
namespace UI_Admin.Services;

public sealed class CalculationApiException(HttpStatusCode status, string message, EmployeeReadiness? readiness = null) : Exception(message)
{ public HttpStatusCode Status { get; } = status; public EmployeeReadiness? Readiness { get; } = readiness; }
public sealed class PayrollCalculationApiService(HttpClient client, IConfiguration config)
{
    private string Root => (config["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("Missing API URL.")).TrimEnd('/') + "/api";
    public static async Task<T> Read<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            try
            {
                using var json = JsonDocument.Parse(text);
                var root = json.RootElement;
                var message = root.TryGetProperty("detail", out var detail) ? detail.GetString() : root.TryGetProperty("message", out var msg) ? msg.GetString() : null;
                var readiness = root.TryGetProperty("readiness", out var r) ? r.Deserialize<EmployeeReadiness>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) : null;
                var defaultMsg = (int)response.StatusCode >= 500
                    ? "Could not complete the payroll request. Please reload and try again."
                    : "Unable to process payroll operation.";
                throw new CalculationApiException(response.StatusCode, message ?? defaultMsg, readiness);
            }
            catch (JsonException)
            {
                var defaultMsg = (int)response.StatusCode >= 500
                    ? "Could not complete the payroll request. Please reload and try again."
                    : "Unable to process payroll operation.";
                throw new CalculationApiException(response.StatusCode, !string.IsNullOrWhiteSpace(text) ? text : defaultMsg);
            }
        }
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new JsonException("Empty calculation response.");
    }
    private async Task<T> Get<T>(string path, CancellationToken ct) { using var r = await client.GetAsync(Root + path, ct); return await Read<T>(r, ct); }
    public async Task<CalculationPage> ListAsync(int periodId, CalculationQuery q, CancellationToken ct)
    {
        var result = await Get<CalculationPage>(QueryHelpers.AddQueryString($"/payroll-periods/{periodId}/payrolls", new Dictionary<string, string?> { ["keyword"] = q.Keyword?.Trim(), ["userId"] = q.UserId?.ToString(), ["status"] = q.Status, ["page"] = q.Page.ToString(), ["pageSize"] = ListPaginationViewModel.DefaultPageSize.ToString() }), ct);
        if (result.Items == null || result.TotalItems < 0 || result.PageNumber < 1 || result.PageSize != 20) throw new JsonException("Invalid payroll list response.");
        return result;
    }
    public async Task<CalculatedPayroll> DetailAsync(int id, CancellationToken ct)
    { var result = await Get<CalculatedPayroll>($"/payrolls/{id}", ct); if (result.PayrollId != id || result.Details == null) throw new JsonException("Invalid payroll detail response."); return result; }
}
