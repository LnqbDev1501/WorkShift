using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Request;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdatePublicHolidayRequest
{
    [Required(ErrorMessage = "Holiday name is required.")]
    [MaxLength(150)]
    public string? HolidayName { get; set; }

    [Required(ErrorMessage = "Holiday date is required.")]
    public DateOnly? HolidayDate { get; set; }

    [Required(ErrorMessage = "Pay multiplier is required.")]
    [Range(typeof(decimal), "0.01", "999.99")]
    public decimal? PayMultiplier { get; set; }

    // Intentionally no IsActive. PUT never changes status.
}
