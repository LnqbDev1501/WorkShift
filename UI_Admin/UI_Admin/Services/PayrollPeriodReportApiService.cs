namespace UI_Admin.Services;
public sealed class PayrollPeriodReportApiService(HttpClient client, IConfiguration config)
{
    public async Task<byte[]> DownloadAsync(int id, CancellationToken ct)
    {
        var root = (config["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("Missing API URL.")).TrimEnd('/');
        using var response = await client.GetAsync($"{root}/api/payroll-periods/{id}/report", ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ApiErrorReader.ReadErrorMessageAsync(response, ct), null, response.StatusCode);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
