namespace UI_Admin.Models
{
    public class RegionViewModel
    {
        public int RegionId { get; set; }

        public string RegionCode { get; set; } = string.Empty;

        public string RegionName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int BranchCount { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
