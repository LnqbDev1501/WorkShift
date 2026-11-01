using System.Text.Json;
using UI_Admin.Models.PayrollCalculation;
namespace UI_Admin.Services.Accountant;

public sealed class PayrollCalculationActionApiService(HttpClient client, IConfiguration config)
{
    private string Root => (config["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("Missing API URL.")).TrimEnd('/') + "/api/payroll-periods";
    public async Task<CalculationInputResponse> InputsAsync(int id, Guid userId, CancellationToken ct)
    {
        using var r = await client.GetAsync($"{Root}/{id}/employees/{userId}/calculation-inputs", ct);
        var result = await PayrollCalculationApiService.Read<CalculationInputResponse>(r, ct);
        if (result.PayrollPeriodId != id || result.Readiness == null || result.Readiness.UserId != userId || result.Sources == null) throw new JsonException("Invalid calculation inputs response.");
        return result;
    }
    public async Task<CalculatedPayroll> PreviewEmployeeAsync(int id, Guid userId, CancellationToken ct)
    {
        using var r = await client.GetAsync($"{Root}/{id}/employees/{userId}/calculation-preview", ct);
        var result = await PayrollCalculationApiService.Read<CalculatedPayroll>(r, ct);
        if (result.PayrollPeriodId != id || result.UserId != userId || result.PayrollId != 0 || result.Status != "PREVIEW" || result.Details == null) throw new JsonException("Invalid preview response.");
        return result;
    }
    public async Task<CalculationBatch> PreviewPeriodAsync(int id, CancellationToken ct)
    {
        using var r = await client.GetAsync($"{Root}/{id}/calculation-preview", ct);
        var result = await PayrollCalculationApiService.Read<CalculationBatch>(r, ct);
        if (result.PayrollPeriodId != id || result.Payrolls == null || result.FailedEmployees == null || result.Payrolls.Any(p => p.PayrollId != 0 || p.Status != "PREVIEW" || p.PayrollPeriodId != id || p.Details == null)) throw new JsonException("Invalid preview response.");
        return result;
    }
    public async Task<CalculationReadiness> ReadinessAsync(int id, CancellationToken ct)
    {
        using var r = await client.GetAsync($"{Root}/{id}/calculation-readiness", ct);
        var result = await PayrollCalculationApiService.Read<CalculationReadiness>(r, ct);
        if (result.PayrollPeriodId != id || result.Employees == null || result.Employees.Any(e => e.BlockingReasons == null || e.Warnings == null)) throw new JsonException("Invalid readiness response.");
        return result;
    }
    public Task<CalculatedPayroll> EmployeeAsync(int id, Guid userId, CancellationToken ct)
        => throw new InvalidOperationException("The initial calculation must be run for the entire period (Calculate Period). Payrolls requested for modification must use the Correction Request and Recalculate workflow.");
    public async Task<CalculationBatch> PeriodAsync(int id, CancellationToken ct)
    {
        using var r = await client.PostAsync($"{Root}/{id}/calculate", null, ct);
        var result = await PayrollCalculationApiService.Read<CalculationBatch>(r, ct);
        if (result.PayrollPeriodId != id || result.Payrolls == null || result.FailedEmployees == null) throw new JsonException("Invalid batch response.");
        return result;
    }
}
