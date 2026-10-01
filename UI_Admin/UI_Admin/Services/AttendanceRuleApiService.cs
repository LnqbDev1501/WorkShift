using System.Net.Http.Json;
using System.Text.Json;
using UI_Admin.Models.AttendanceRule;

namespace UI_Admin.Services;

public sealed class AttendanceRuleApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public AttendanceRuleApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<AttendanceRuleListViewModel> GetListAsync(
        AttendanceRuleListQueryViewModel query,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string> { $"pageNumber={Math.Max(1, query.PageNumber)}" };
        if (!string.IsNullOrWhiteSpace(query.Search))
            parameters.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        if (!string.IsNullOrWhiteSpace(query.Status))
            parameters.Add($"status={Uri.EscapeDataString(query.Status.Trim())}");

        var url = BuildUrl($"api/admin/attendance-rules?{string.Join('&', parameters)}");
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedAttendanceRuleApiResponse>(cancellationToken: cancellationToken)
            ?? new PagedAttendanceRuleApiResponse();

        // Calculate counts across all status categories
        var allListUrl = BuildUrl("api/admin/attendance-rules?pageNumber=1&pageSize=1000");
        var allListResponse = await _httpClient.GetFromJsonAsync<PagedAttendanceRuleApiResponse>(allListUrl, cancellationToken)
            ?? new PagedAttendanceRuleApiResponse();

        var allItems = allListResponse.Items ?? [];

        return new AttendanceRuleListViewModel
        {
            Query = query,
            Items = pagedResponse.Items.Select(MapToViewModel).ToList(),
            PageNumber = pagedResponse.PageNumber,
            PageSize = pagedResponse.PageSize,
            TotalItems = pagedResponse.TotalItems,
            TotalPages = pagedResponse.TotalPages,
            TotalRules = allListResponse.TotalItems,
            ActiveRules = allItems.Count(r => r.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)),
            ScheduledRules = allItems.Count(r => r.Status.Equals("SCHEDULED", StringComparison.OrdinalIgnoreCase)),
            DraftRules = allItems.Count(r => r.Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
        };
    }

    public async Task<AttendanceRuleListItemViewModel> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(BuildUrl($"api/admin/attendance-rules/{id}"), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var dto = await response.Content.ReadFromJsonAsync<AttendanceRuleApiItemResponse>(cancellationToken: cancellationToken)
            ?? throw new JsonException("Attendance rule API returned an empty response.");

        return MapToViewModel(dto);
    }

    public async Task PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl($"api/admin/attendance-rules/{id}/publish"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CancelAsync(int id, string? cancellationReason, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl($"api/admin/attendance-rules/{id}/cancel"),
            new { cancellationReason },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, BuildUrl($"api/admin/attendance-rules/{id}"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static AttendanceRuleListItemViewModel MapToViewModel(AttendanceRuleApiItemResponse dto) => new()
    {
        AttendanceRuleId = dto.AttendanceRuleId,
        RuleName = dto.RuleName,
        Description = dto.Description,
        CheckInBeforeMinutes = dto.CheckInBeforeMinutes,
        CheckInGraceMinutes = dto.CheckInGraceMinutes,
        CheckOutGraceMinutes = dto.CheckOutGraceMinutes,
        CheckOutAfterMinutes = dto.CheckOutAfterMinutes,
        MaxAttendanceDistanceMeters = dto.MaxAttendanceDistanceMeters,
        EffectiveFrom = dto.EffectiveFrom,
        EffectiveTo = dto.EffectiveTo,
        Status = dto.Status,
        CreatedByName = dto.CreatedByName,
        CreatedAt = dto.CreatedAt
    };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var message = "Backend API request failed. Please try again later.";
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken: cancellationToken);
            message = error?.Errors?.Count > 0
                ? string.Join(" ", error.Errors.Values.SelectMany(errors => errors))
                : error?.Message ?? error?.Detail ?? message;
        }
        catch (JsonException)
        {
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private string BuildUrl(string relativeUrl) =>
        $"{_apiBaseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";

    private sealed class PagedAttendanceRuleApiResponse
    {
        public List<AttendanceRuleApiItemResponse> Items { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class AttendanceRuleApiItemResponse
    {
        public int AttendanceRuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CheckInBeforeMinutes { get; set; }
        public int CheckInGraceMinutes { get; set; }
        public int CheckOutGraceMinutes { get; set; }
        public int CheckOutAfterMinutes { get; set; }
        public int MaxAttendanceDistanceMeters { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
