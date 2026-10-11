using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.Holidays;

public sealed class HolidayRuleViewModel
{
    public int HolidayPayRuleId { get; set; }
    [Required, StringLength(150)] public string RuleName { get; set; } = "";
    [Required, Range(typeof(decimal), "0.01", "999.99")] public decimal? PayMultiplier { get; set; }
    [Required, DataType(DataType.Date)] public DateOnly? EffectiveFrom { get; set; }
    public int? ReplacedRuleId { get; set; }
    public string Status { get; set; } = "";
    public bool CanEdit { get; set; }
    public bool CanPublish { get; set; }
    public bool CanCancel { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StatusLabel => Status switch { "DRAFT" => "Draft", "PUBLISH" => "Published", "ACTIVE" => "Active", "INACTIVE" => "Inactive", "CANCEL" => "Cancelled", _ => Status };
}
