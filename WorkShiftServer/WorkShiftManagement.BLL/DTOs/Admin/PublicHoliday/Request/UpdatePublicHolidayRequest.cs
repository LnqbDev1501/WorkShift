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


}


