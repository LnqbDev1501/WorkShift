namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class AdminUserListResponse
{
    public IReadOnlyList<AdminUserListItemResponse> Items { get; set; } =
        Array.Empty<AdminUserListItemResponse>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
