namespace UI_Admin.Models;

public sealed class ListPaginationViewModel
{
    public const int DefaultPageSize = 20;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = Math.Clamp(value, 1, int.MaxValue / 100);
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > 0 ? value : DefaultPageSize;
    }

    public int TotalItems { get; init; }
    public int? MaximumPages { get; init; }
    public string ItemLabel { get; init; } = "records";
    public string PageParameter { get; init; } = "page";
    public int TotalPages => Math.Max(1, Math.Min(MaximumPages ?? int.MaxValue,
        (int)Math.Ceiling(Math.Max(0, TotalItems) / (double)Math.Max(1, PageSize))));
    public int FirstItem => TotalItems == 0 || PageNumber > TotalPages ? 0 : (PageNumber - 1) * PageSize + 1;
    public int LastItem => FirstItem == 0 ? 0 : (int)Math.Min((long)PageNumber * PageSize, TotalItems);

    // For catalog endpoints that return the complete filtered collection.
    public static ListPaginationViewModel ForCollection(int totalItems, string? requestedPage, string itemLabel)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)DefaultPageSize));
        var pageNumber = int.TryParse(requestedPage, out var value) ? Math.Clamp(value, 1, totalPages) : 1;
        return new ListPaginationViewModel
        {
            TotalItems = totalItems,
            PageNumber = pageNumber,
            ItemLabel = itemLabel
        };
    }
}
