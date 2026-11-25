using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WorkShiftManagement.BLL.DTOs.Admin.HolidayPayRule.Request;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HolidayPayRuleRequest
{
    [Required, StringLength(150)]
    public string RuleName { get; set; } = "";

    [Required, Range(typeof(decimal), "0.01", "999.99")]
    public decimal? PayMultiplier { get; set; }

    [Required]
    public DateOnly? EffectiveFrom { get; set; }
}

