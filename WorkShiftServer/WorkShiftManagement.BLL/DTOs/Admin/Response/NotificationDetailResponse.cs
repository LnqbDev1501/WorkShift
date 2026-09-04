namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class NotificationDetailResponse : NotificationListResponse
{
    public string Message { get; set; } = null!;

    public int? TargetRegionId { get; set; }

    public int? TargetBranchId { get; set; }

    public string CreatedByName { get; set; } = null!;

    public int InAppRecipientCount { get; set; }
}
