using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models.EmployeeAdjustments;

namespace UI_Admin.Services;

public sealed class EmployeeAdjustmentApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

public sealed class EmployeeAdjustmentApiService(HttpClient client, IConfiguration configuration)
{
    private string Base => configuration.GetWorkShiftApiBaseUrl();

    private string Endpoint(string type) =>
        $"{Base}/api/employee-{(type == EmployeeAdjustmentTypes.Penalty ? "penalties" : "rewards")}";

    private string AdminEndpoint(string type) =>
        $"{Base}/api/admin/employee-{(type == EmployeeAdjustmentTypes.Penalty ? "penalties" : "rewards")}";

    private static async Task<T> Read<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw new EmployeeAdjustmentApiException(response.StatusCode, (int)response.StatusCode >= 500
                ? "Unable to process the request. Reload its status before trying again."
                : await ApiErrorReader.ReadErrorMessageAsync(response, ct));
        return await response.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    public async Task<EmployeeAdjustmentListViewModel> ListAsync(string type, EmployeeAdjustmentQuery q, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["keyword"] = q.Keyword?.Trim(),
            ["userId"] = q.UserId?.ToString(),
            ["payrollPeriodId"] = q.PayrollPeriodId?.ToString(),
            ["branchId"] = q.BranchId?.ToString(),
            ["approvalStatus"] = q.ApprovalStatus,
            ["createdFrom"] = q.CreatedFrom?.ToString("yyyy-MM-dd"),
            ["createdTo"] = q.CreatedTo?.ToString("yyyy-MM-dd"),
            ["page"] = q.Page.ToString(),
            ["pageSize"] = "20"
        };
        using var response = await client.GetAsync(QueryHelpers.AddQueryString(Endpoint(type), query), ct);
        var result = await Read<EmployeeAdjustmentListViewModel>(response, ct);
        result.AdjustmentType = type;
        result.Query = q;
        return result;
    }

    public async Task<EmployeeAdjustmentItem> DetailAsync(string type, int id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{Endpoint(type)}/{id}", ct);
        return await Read<EmployeeAdjustmentItem>(response, ct);
    }

    public async Task<List<PayrollPeriodOption>> GetPayrollPeriodsAsync(CancellationToken ct)
    {
        using var response = await client.GetAsync($"{Base}/api/manager/payroll-periods", ct);
        return await Read<List<PayrollPeriodOption>>(response, ct);
    }

    public Task<EmployeeAdjustmentItem> CreateAsync(string type, SaveEmployeeAdjustmentViewModel model, CancellationToken ct) =>
        Post(Endpoint(type), new
        {
            model.UserId,
            model.PayrollPeriodId,
            adjustmentDate = model.AdjustmentDate?.ToString("yyyy-MM-dd"),
            adjustmentName = model.AdjustmentName.Trim(),
            model.Amount,
            reason = model.Reason.Trim()
        }, ct);

    public async Task<EmployeeAdjustmentItem> UpdateAsync(string type, int id, SaveEmployeeAdjustmentViewModel model, CancellationToken ct)
    {
        using var response = await client.PatchAsJsonAsync($"{Endpoint(type)}/{id}", new
        {
            model.UserId,
            model.PayrollPeriodId,
            adjustmentDate = model.AdjustmentDate?.ToString("yyyy-MM-dd"),
            adjustmentName = model.AdjustmentName.Trim(),
            model.Amount,
            reason = model.Reason.Trim()
        }, ct);
        return await Read<EmployeeAdjustmentItem>(response, ct);
    }

    public async Task DeleteAsync(string type, int id, CancellationToken ct)
    {
        using var response = await client.DeleteAsync($"{Endpoint(type)}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            throw new EmployeeAdjustmentApiException(response.StatusCode, (int)response.StatusCode >= 500
                ? "Unable to process the request. Reload its status before trying again."
                : await ApiErrorReader.ReadErrorMessageAsync(response, ct));
    }

    public Task<EmployeeAdjustmentItem> ApproveAsync(string type, int id, ReviewEmployeeAdjustmentViewModel model, CancellationToken ct) =>
        Post($"{AdminEndpoint(type)}/{id}/approve", new { model.ReviewNote }, ct);

    public Task<EmployeeAdjustmentItem> RejectAsync(string type, int id, ReviewEmployeeAdjustmentViewModel model, CancellationToken ct) =>
        Post($"{AdminEndpoint(type)}/{id}/reject", new { model.ReviewNote }, ct);

    private async Task<EmployeeAdjustmentItem> Post(string url, object payload, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(url, payload, ct);
        return await Read<EmployeeAdjustmentItem>(response, ct);
    }
}
