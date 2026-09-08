using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Request;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreatePublicHolidaysRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(366)]
    public List<CreatePublicHolidayItemRequest>? Holidays { get; set; }
}
