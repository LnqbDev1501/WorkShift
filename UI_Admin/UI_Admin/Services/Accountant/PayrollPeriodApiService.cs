using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models;
using UI_Admin.Models.Accountant.PayrollPeriods;
namespace UI_Admin.Services.Accountant;

public sealed class PayrollPeriodApiException(HttpStatusCode status, string message) : Exception(message)
{ public HttpStatusCode Status { get; } = status; }
public sealed class PayrollPeriodApiService(HttpClient client, IConfiguration config)
{
    private string Root => (config["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("Missing API URL.")).TrimEnd('/') + "/api/accountant/payroll-periods";
    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, Root + path);
        if (body != null) request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return response;
        using (response)
        {
            var error = (int)response.StatusCode >= 500 ? "Không xác nhận được kết quả. Hãy tải lại danh sách trước khi thử lại." : await ApiErrorReader.ReadErrorMessageAsync(response, ct);
            throw new PayrollPeriodApiException(response.StatusCode, error ?? "Không thể xử lý kỳ lương.");
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
        { ["search"] = q.Search?.Trim(), ["status"] = q.Status, ["startDateFrom"] = q.StartDateFrom?.ToString("yyyy-MM-dd"), ["startDateTo"] = q.StartDateTo?.ToString("yyyy-MM-dd"), ["pageNumber"] = q.PageNumber.ToString(), ["pageSize"] = ListPaginationViewModel.DefaultPageSize.ToString() }), ct);
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
    public async Task OpenAsync(int id, CancellationToken ct) { using var response = await Send(HttpMethod.Post, $"/{id}/open", null, ct); }
    public async Task DeleteAsync(int id, CancellationToken ct) { using var response = await Send(HttpMethod.Delete, $"/{id}", null, ct); }
}
