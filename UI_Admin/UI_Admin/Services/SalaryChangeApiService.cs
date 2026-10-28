using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models.SalaryChanges;

namespace UI_Admin.Services;

public sealed class SalaryChangeApiException(HttpStatusCode status, string message) : Exception(message)
{ public HttpStatusCode Status { get; } = status; }

public sealed class SalaryChangeApiService(HttpClient client, IConfiguration configuration)
{
    private string Endpoint => (configuration["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("API address is missing.")).TrimEnd('/') + "/api/salary-change-requests";
    private static async Task<T> Read<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw new SalaryChangeApiException(response.StatusCode, (int)response.StatusCode >= 500
                ? "Unable to process the request. Reload its status before trying again."
                : await ApiErrorReader.ReadErrorMessageAsync(response, ct));
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new InvalidOperationException("The API returned an empty response.");
    }
    public async Task<SalaryChangeListViewModel> ListAsync(SalaryChangeQuery q, CancellationToken ct)
    {
        var query = new Dictionary<string, string?> {
            ["keyword"] = q.Keyword?.Trim(), ["userId"] = q.UserId?.ToString(), ["branchId"] = q.BranchId?.ToString(), ["status"] = q.Status,
            ["createdFrom"] = q.CreatedFrom?.ToString("yyyy-MM-dd"), ["createdTo"] = q.CreatedTo?.ToString("yyyy-MM-dd"),
            ["effectiveFrom"] = q.EffectiveFrom?.ToString("yyyy-MM-dd"), ["effectiveTo"] = q.EffectiveTo?.ToString("yyyy-MM-dd"),
            ["page"] = q.Page.ToString(), ["pageSize"] = "20"
        };
        using var response = await client.GetAsync(QueryHelpers.AddQueryString(Endpoint, query), ct);
        var result = await Read<SalaryChangeListViewModel>(response, ct);
        result.Query = q;
        return result;
    }
    public async Task<SalaryChangeItem> DetailAsync(int id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{Endpoint}/{id}", ct);
        return await Read<SalaryChangeItem>(response, ct);
    }
    public Task<SalaryChangeItem> CreateAsync(CreateSalaryChangeViewModel model, CancellationToken ct) => Post(Endpoint, new {
        model.UserId, model.EmployeeRequestId, model.PreviousRequestId, model.ProposedHourlyRate, model.ProposedEffectiveFrom, reason = model.Reason.Trim()
    }, ct);
    public Task<SalaryChangeItem> ApproveAsync(int id, SalaryChangeDecision model, CancellationToken ct) => Post($"{Endpoint}/{id}/approve", new {
        model.ApprovedEffectiveFrom, model.EffectiveDateChangeReason, model.ReviewNote
    }, ct);
    public Task<SalaryChangeItem> RejectAsync(int id, SalaryChangeDecision model, CancellationToken ct) => Post($"{Endpoint}/{id}/reject", new { model.ReviewNote }, ct);
    public Task<SalaryChangeItem> CancelAsync(int id, SalaryChangeDecision model, CancellationToken ct) => Post($"{Endpoint}/{id}/cancel", new { model.Reason }, ct);
    private async Task<SalaryChangeItem> Post(string url, object payload, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(url, payload, ct);
        return await Read<SalaryChangeItem>(response, ct);
    }
}
