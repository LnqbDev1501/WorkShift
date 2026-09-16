namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class NotificationAudienceOptionsResponse
{
    public List<NotificationAudienceRegionOptionResponse> Regions { get; set; } = [];
    public List<NotificationAudienceBranchOptionResponse> Branches { get; set; } = [];
}

public class NotificationAudienceRegionOptionResponse
{
    public int RegionId { get; set; }
    public string RegionName { get; set; } = null!;
}

public class NotificationAudienceBranchOptionResponse
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public int RegionId { get; set; }
}
