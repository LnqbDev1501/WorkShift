using System.Text.Json.Serialization;

namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];

    public int PageNumber { get; set; }

    [JsonIgnore]
    public int Page
    {
        get => PageNumber;
        set => PageNumber = value;
    }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    [JsonIgnore]
    public int TotalCount
    {
        get => TotalItems;
        set => TotalItems = value;
    }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PaidCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CompletedCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TotalOrderValue { get; set; }
}
