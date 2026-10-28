using UI_Admin.Models.Staff.Shifts;

namespace UI_Admin.Services.Staff;

public class StaffShiftApiService(HttpClient client, IConfiguration configuration)
{
    private readonly string baseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');

    public async Task<IReadOnlyList<StaffShiftListItemViewModel>> GetAvailableShiftsAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (fromDate.HasValue)
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue)
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var url = $"{baseUrl}/api/staff/shifts/available?{string.Join("&", queryParams)}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        var paged = await response.Content.ReadFromJsonAsync<PagedResult<StaffShiftListItemViewModel>>(cancellationToken: ct);
        return paged?.Items ?? [];
    }

    public async Task<IReadOnlyList<StaffShiftListItemViewModel>> GetMyScheduleAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (fromDate.HasValue)
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue)
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var url = $"{baseUrl}/api/staff/shifts/my-schedule?{string.Join("&", queryParams)}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        var paged = await response.Content.ReadFromJsonAsync<PagedResult<StaffShiftListItemViewModel>>(cancellationToken: ct);
        return paged?.Items ?? [];
    }

    public async Task<StaffShiftRegistrationViewModel> RegisterShiftAsync(int shiftId, CancellationToken ct = default)
    {
        var url = $"{baseUrl}/api/staff/shifts/{shiftId}/register";
        using var response = await client.PostAsync(url, null, ct);
        await EnsureSuccessAsync(response, ct);

        var data = await response.Content.ReadFromJsonAsync<StaffShiftRegistrationViewModel>(cancellationToken: ct);
        return data ?? throw new InvalidOperationException("Không thể nhận kết quả đăng ký ca làm.");
    }

    public async Task<StaffShiftRegistrationViewModel> CancelRegistrationAsync(
        int shiftId,
        string reason,
        CancellationToken ct = default)
    {
        var url = $"{baseUrl}/api/staff/shifts/{shiftId}/cancel-registration";
        var payload = new { reason = reason.Trim() };
        using var response = await client.PostAsJsonAsync(url, payload, ct);
        await EnsureSuccessAsync(response, ct);

        var data = await response.Content.ReadFromJsonAsync<StaffShiftRegistrationViewModel>(cancellationToken: ct);
        return data ?? throw new InvalidOperationException("Không thể nhận kết quả hủy đăng ký ca làm.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await ApiErrorReader.ReadErrorMessageAsync(response, ct);
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }

    private sealed class PagedResult<T>
    {
        public List<T> Items { get; set; } = [];
    }
}
