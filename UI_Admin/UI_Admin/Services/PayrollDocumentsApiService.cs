using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Models.PayrollDocuments;
namespace UI_Admin.Services;

public sealed class PayrollDocumentsApiService(HttpClient client, IConfiguration config)
{
    private string Root => (config["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("Missing API URL.")).TrimEnd('/') + "/api";
    public static string Query(object input)
    {
        var fields = input.GetType().GetProperties().ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..], p =>
        {
            var value = p.GetValue(input);
            return value switch { null => null, DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), bool b => b ? "true" : "false", IFormattable f => f.ToString(null, CultureInfo.InvariantCulture), _ => value.ToString() };
        });
        return QueryHelpers.AddQueryString("", fields);
    }
    public async Task<T> Get<T>(string path, CancellationToken ct)
    { using var r = await client.GetAsync(Root + path, ct); return await PayrollCalculationApiService.Read<T>(r, ct); }
    public async Task<T> Post<T>(string path, object? body, CancellationToken ct)
    { using var r = body == null ? await client.PostAsync(Root + path, null, ct) : await client.PostAsJsonAsync(Root + path, body, ct); return await PayrollCalculationApiService.Read<T>(r, ct); }
    public Task<PayslipDetailsResponse> Payslip(int id, CancellationToken ct) => Get<PayslipDetailsResponse>($"/payslips/{id}", ct);
    public Task<PayrollFeedbackDetailsResponse> Feedback(int id, CancellationToken ct) => Get<PayrollFeedbackDetailsResponse>($"/payroll-feedback-requests/{id}", ct);
    public Task<PayrollVersions> Versions(int id, CancellationToken ct) => Get<PayrollVersions>($"/payrolls/{id}/versions", ct);
    public Task<CalculatedPayroll> Payroll(int id, CancellationToken ct) => Get<CalculatedPayroll>($"/payrolls/{id}", ct);
    public async Task<string> PeriodStatus(int id, bool accountant, CancellationToken ct) => (await Get<PeriodState>($"/{(accountant ? "accountant" : "admin")}/payroll-periods/{id}", ct)).Status;
    private sealed class PeriodState { public string Status { get; set; } = ""; }
}
