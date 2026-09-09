using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Request;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreatePublicHolidayItemRequest
{
    [Required(ErrorMessage = "Holiday name is required.")]
    [MaxLength(150)]
    public string? HolidayName { get; set; }

    [Required(ErrorMessage = "Holiday date is required.")]
    public DateOnly? HolidayDate { get; set; }

    // Nullable + Required distinguishes an omitted/null value on UPDATE;
    // CREATE defaults to 1 when the field is omitted.
    [Range(typeof(decimal), "0.01", "999.99")]
    public decimal? PayMultiplier { get; set; } = 1m;
}
