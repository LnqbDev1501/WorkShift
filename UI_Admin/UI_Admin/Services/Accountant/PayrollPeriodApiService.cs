using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models;
using UI_Admin.Models.Accountant.PayrollPeriods;
using UI_Admin.Models.Accountant.PayrollSettlement;
using UI_Admin.Models.PayrollDocuments;

namespace UI_Admin.Services.Accountant;

public sealed class PayrollPeriodApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

public sealed class PayrollPeriodApiService(HttpClient client, IConfiguration config)
{
    private string BaseApi => (config["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("Missing API URL.")).TrimEnd('/') + "/api";
    private string Root => BaseApi + "/accountant/payroll-periods";

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var url = path.StartsWith("/api/") ? (config["WorkShiftApi:BaseUrl"] ?? "").TrimEnd('/') + path : Root + path;
        using var request = new HttpRequestMessage(method, url);
        if (body != null) request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return response;
        using (response)
        {
            var error = (int)response.StatusCode >= 500 ? "Unable to confirm result. Please reload the list before trying again." : await ApiErrorReader.ReadErrorMessageAsync(response, ct);
            throw new PayrollPeriodApiException(response.StatusCode, error ?? "Unable to process payroll period.");
        }
    }

    private async Task<T> Get<T>(string path, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Get, path, null, ct);
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new JsonException("Empty API response.");
    }

    public async Task<PeriodPage<PeriodRecord>> ListAsync(PeriodQuery q, CancellationToken ct)
    {
        var result = await Get<PeriodPage<PeriodRecord>>(QueryHelpers.AddQueryString("", new Dictionary<string, string?>
        {
            ["search"] = q.Search?.Trim(),
            ["status"] = q.Status,
            ["startDateFrom"] = q.StartDateFrom?.ToString("yyyy-MM-dd"),
            ["startDateTo"] = q.StartDateTo?.ToString("yyyy-MM-dd"),
            ["pageNumber"] = q.PageNumber.ToString(),
            ["pageSize"] = ListPaginationViewModel.DefaultPageSize.ToString()
        }), ct);
        if (result.Items == null || result.TotalItems < 0 || result.PageNumber < 1 || result.PageSize != ListPaginationViewModel.DefaultPageSize) throw new JsonException("Invalid period list response.");
        return result;
    }

    public async Task<PeriodRecord> DetailAsync(int id, CancellationToken ct)
    {
        var result = await Get<PeriodRecord>($"/{id}", ct);
        if (result.PayrollPeriodId != id || result.Payrolls == null) throw new JsonException("Invalid period detail response.");
        return result;
    }

    public async Task<List<ScheduleWeekOption>> WeeksAsync(CancellationToken ct)
    {
        var items = new List<ScheduleWeekOption>();
        for (var page = 1; ; page++)
        {
            var result = await Get<PeriodPage<ScheduleWeekOption>>($"/available-schedule-weeks?pageNumber={page}&pageSize=100", ct);
            if (result.Items == null || result.PageNumber != page || result.PageSize <= 0) throw new JsonException("Invalid available week response.");
            items.AddRange(result.Items);
            if (page >= result.TotalPages) return items.DistinctBy(w => w.ScheduleWeekId).ToList();
        }
    }

    public async Task<int> SaveAsync(PeriodForm form, CancellationToken ct)
    {
        using var response = await Send(form.Id == 0 ? HttpMethod.Post : HttpMethod.Patch, form.Id == 0 ? "" : $"/{form.Id}", new { periodName = form.PeriodName.Trim(), scheduleWeekId = form.ScheduleWeekId }, ct);
        return (await response.Content.ReadFromJsonAsync<PeriodRecord>(ct) ?? throw new JsonException("Empty period response.")).PayrollPeriodId;
    }

    public async Task OpenAsync(int id, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Post, $"/{id}/open", null, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Delete, $"/{id}", null, ct);
    }

    // Step 7: Send Previews to Staff
    public async Task SendPreviewsAsync(int id, DateTimeOffset? initialFeedbackDeadlineAt, CancellationToken ct)
    {
        object? body = initialFeedbackDeadlineAt.HasValue
            ? new { initialFeedbackDeadlineAt = initialFeedbackDeadlineAt.Value }
            : null;
        using var response = await Send(HttpMethod.Post, $"/api/payroll-periods/{id}/send-previews", body, ct);
    }

    // Step 10: Submit Period to Admin
    public async Task SubmitPeriodAsync(int id, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Post, $"/{id}/submit", null, ct);
    }

    // Step 12: Settlement Summary & Confirmation
    public async Task<PayrollSettlementSummaryResponse> SettlementSummaryAsync(int id, CancellationToken ct)
    {
        return await Get<PayrollSettlementSummaryResponse>($"/api/payroll-periods/{id}/settlement-summary", ct);
    }

    public async Task<PayrollSettlementConfirmationResponse> ConfirmSettlementAsync(int id, ConfirmPayrollSettlementRequest request, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Post, $"/api/payroll-periods/{id}/confirm-settlement", request, ct);
        return await response.Content.ReadFromJsonAsync<PayrollSettlementConfirmationResponse>(ct)
            ?? throw new JsonException("Empty settlement confirmation response.");
    }

    // Source Impact
    public async Task<PayrollSourceImpactResponse> SourceImpactAsync(int id, CancellationToken ct)
    {
        return await Get<PayrollSourceImpactResponse>($"/api/payroll-periods/{id}/source-impact", ct);
    }

    // Correction Requests in Period
    public async Task<List<PayrollIssueResponse>> CorrectionRequestsAsync(int id, CancellationToken ct)
    {
        return await Get<List<PayrollIssueResponse>>($"/api/payroll-periods/{id}/correction-requests", ct);
    }
}
