using System.Text.Json;
using UI_Admin.Models.PayrollCalculation;
namespace UI_Admin.Services.Accountant;

public sealed class PayrollCalculationActionApiService(HttpClient client, IConfiguration config)
{
    private string Root => (config["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("Missing API URL.")).TrimEnd('/') + "/api/payroll-periods";
    public async Task<CalculationReadiness> ReadinessAsync(int id, CancellationToken ct)
    {
        using var r = await client.GetAsync($"{Root}/{id}/calculation-readiness", ct);
        var result = await PayrollCalculationApiService.Read<CalculationReadiness>(r, ct);
        if (result.PayrollPeriodId != id || result.Employees == null || result.Employees.Any(e => e.BlockingReasons == null || e.Warnings == null)) throw new JsonException("Invalid readiness response.");
        return result;
    }
    public async Task<CalculatedPayroll> EmployeeAsync(int id, Guid userId, CancellationToken ct)
    {
        using var r = await client.PostAsync($"{Root}/{id}/employees/{userId}/calculate", null, ct);
        var result = await PayrollCalculationApiService.Read<CalculatedPayroll>(r, ct);
        if (result.PayrollPeriodId != id || result.UserId != userId) throw new JsonException("Invalid employee calculation response.");
        return result;
    }
    public async Task<CalculationBatch> PeriodAsync(int id, CancellationToken ct)
    {
        using var r = await client.PostAsync($"{Root}/{id}/calculate", null, ct);
        var result = await PayrollCalculationApiService.Read<CalculationBatch>(r, ct);
        if (result.PayrollPeriodId != id || result.Payrolls == null || result.FailedEmployees == null) throw new JsonException("Invalid batch response.");
        return result;
    }
}
