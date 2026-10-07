using System.Net.Http.Json;
using System.Text.Json;
using UI_Admin.Models.Admin.AttendancePenaltyRule;

namespace UI_Admin.Services.Admin;

public sealed class AttendancePenaltyRuleApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public AttendancePenaltyRuleApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<AttendancePenaltyRuleListViewModel> GetListAsync(
        string? search,
        string? status,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string> { $"pageNumber={Math.Max(1, pageNumber)}" };
        if (!string.IsNullOrWhiteSpace(search))
            parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(status))
            parameters.Add($"status={Uri.EscapeDataString(status.Trim().ToUpperInvariant())}");

        var url = BuildUrl($"api/admin/attendance-penalty-rules?{string.Join('&', parameters)}");
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var pagedResponse = await response.Content.ReadFromJsonAsync<AttendancePenaltyRuleListApiResponse>(cancellationToken: cancellationToken)
            ?? new AttendancePenaltyRuleListApiResponse();

        // Calculate counts across all status categories
        var allListUrl = BuildUrl("api/admin/attendance-penalty-rules?pageNumber=1");
        var allListResponse = await _httpClient.GetFromJsonAsync<AttendancePenaltyRuleListApiResponse>(allListUrl, cancellationToken)
            ?? new AttendancePenaltyRuleListApiResponse();

        var allItems = allListResponse.Items ?? Array.Empty<AttendancePenaltyRuleApiItemResponse>();

        return new AttendancePenaltyRuleListViewModel
        {
            Items = pagedResponse.Items.Select(MapToItemViewModel).ToList(),
            PageNumber = pagedResponse.PageNumber,
            PageSize = pagedResponse.PageSize,
            TotalItems = pagedResponse.TotalItems,
            SearchTerm = search,
            Status = status,
            TotalRules = allListResponse.TotalItems,
            ActiveRules = allItems.Count(r => r.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)),
            ScheduledRules = allItems.Count(r => r.Status.Equals("SCHEDULED", StringComparison.OrdinalIgnoreCase)),
            DraftRules = allItems.Count(r => r.Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
        };
    }

    public async Task<AttendancePenaltyRuleDetailViewModel> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(BuildUrl($"api/admin/attendance-penalty-rules/{id}"), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var dto = await response.Content.ReadFromJsonAsync<AttendancePenaltyRuleDetailApiResponse>(cancellationToken: cancellationToken)
            ?? throw new JsonException("Attendance penalty rule API returned an empty response.");

        return MapToDetailViewModel(dto);
    }

    public async Task<int> CreateAsync(CreateAttendancePenaltyRuleViewModel model, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model.RuleName,
            model.Description,
            EffectiveFrom = model.EffectiveFrom?.ToString("yyyy-MM-dd"),
            PenaltyLevels = model.PenaltyLevels.Select(l => new
            {
                ViolationType = l.ViolationType?.Trim().ToUpperInvariant(),
                l.FromMinutes,
                l.ToMinutes,
                l.PenaltyAmount
            }).ToList()
        };

        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl("api/admin/attendance-penalty-rules"),
            payload,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var createdDto = await response.Content.ReadFromJsonAsync<AttendancePenaltyRuleDetailApiResponse>(cancellationToken: cancellationToken);
        return createdDto?.PenaltyRuleId ?? 0;
    }

    public async Task UpdateAsync(int id, CreateAttendancePenaltyRuleViewModel model, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model.RuleName,
            model.Description,
            EffectiveFrom = model.EffectiveFrom?.ToString("yyyy-MM-dd"),
            PenaltyLevels = model.PenaltyLevels.Select(l => new
            {
                ViolationType = l.ViolationType?.Trim().ToUpperInvariant(),
                l.FromMinutes,
                l.ToMinutes,
                l.PenaltyAmount
            }).ToList()
        };

        using var response = await _httpClient.PutAsJsonAsync(
            BuildUrl($"api/admin/attendance-penalty-rules/{id}"),
            payload,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            BuildUrl($"api/admin/attendance-penalty-rules/{id}/publish"),
            null,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CancelAsync(int id, string? cancellationReason, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            CancellationReason = cancellationReason
        };

        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl($"api/admin/attendance-penalty-rules/{id}/cancel"),
            payload,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static AttendancePenaltyRuleItemViewModel MapToItemViewModel(AttendancePenaltyRuleApiItemResponse dto)
    {
        return new AttendancePenaltyRuleItemViewModel
        {
            PenaltyRuleId = dto.PenaltyRuleId,
            RuleName = dto.RuleName,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            Status = FormatStatus(dto.Status),
            CreatedAt = dto.CreatedAt
        };
    }

    private static AttendancePenaltyRuleDetailViewModel MapToDetailViewModel(AttendancePenaltyRuleDetailApiResponse dto)
    {
        return new AttendancePenaltyRuleDetailViewModel
        {
            PenaltyRuleId = dto.PenaltyRuleId,
            RuleName = dto.RuleName,
            Description = dto.Description,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            Status = FormatStatus(dto.Status),
            CreatedBy = dto.CreatedBy,
            CreatedByName = dto.CreatedByName,
            PublishedBy = dto.PublishedBy,
            PublishedByName = dto.PublishedByName,
            PublishedAt = dto.PublishedAt,
            CancelledBy = dto.CancelledBy,
            CancelledByName = dto.CancelledByName,
            CancelledAt = dto.CancelledAt,
            CancellationReason = dto.CancellationReason,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            PenaltyLevels = dto.PenaltyLevels.Select(l => new AttendancePenaltyLevelViewModel
            {
                PenaltyLevelId = l.PenaltyLevelId,
                PenaltyRuleId = l.PenaltyRuleId,
                ViolationType = l.ViolationType,
                FromMinutes = l.FromMinutes,
                ToMinutes = l.ToMinutes,
                PenaltyAmount = l.PenaltyAmount,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            }).ToList()
        };
    }

    private static string FormatStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status)) return string.Empty;
        return char.ToUpperInvariant(status[0]) + status[1..].ToLowerInvariant();
    }

    private string BuildUrl(string relativeUrl)
    {
        return $"{_apiBaseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private sealed class AttendancePenaltyRuleListApiResponse
    {
        public IReadOnlyList<AttendancePenaltyRuleApiItemResponse> Items { get; set; } = Array.Empty<AttendancePenaltyRuleApiItemResponse>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class AttendancePenaltyRuleApiItemResponse
    {
        public int PenaltyRuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    private sealed class AttendancePenaltyRuleDetailApiResponse
    {
        public int PenaltyRuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public Guid? PublishedBy { get; set; }
        public string? PublishedByName { get; set; }
        public DateTime? PublishedAt { get; set; }
        public Guid? CancelledBy { get; set; }
        public string? CancelledByName { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<AttendancePenaltyLevelApiResponse> PenaltyLevels { get; set; } = new();
    }

    private sealed class AttendancePenaltyLevelApiResponse
    {
        public int PenaltyLevelId { get; set; }
        public int PenaltyRuleId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public int FromMinutes { get; set; }
        public int? ToMinutes { get; set; }
        public decimal PenaltyAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
