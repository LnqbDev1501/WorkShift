namespace UI_Admin.Services;

public sealed class PayrollPeriodReportApiService(HttpClient client, IConfiguration config)
{
    public async Task<(byte[] FileBytes, string FileName, string ContentType)> DownloadAsync(int id, CancellationToken ct)
    {
        var root = (config["WorkShiftApi:BaseUrl"] ?? throw new InvalidOperationException("Missing API URL.")).TrimEnd('/');
        using var response = await client.GetAsync($"{root}/api/payroll-periods/{id}/report", ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ApiErrorReader.ReadErrorMessageAsync(response, ct), null, response.StatusCode);

        var fileBytes = await response.Content.ReadAsByteArrayAsync(ct);
        string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"WorkShift_Payroll_{id}.xlsx";
        string contentType = response.Content.Headers.ContentType?.MediaType
            ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return (fileBytes, fileName, contentType);
    }
}
