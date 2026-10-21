using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public sealed class CreateShiftTemplateRequest : IValidatableObject
{
    public string? ShiftName { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }

    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ShiftName is null)
            yield return new ValidationResult("shiftName is required. Provide a name such as 'Morning Shift'.", [nameof(ShiftName)]);
        else if (string.IsNullOrWhiteSpace(ShiftName))
            yield return new ValidationResult("shiftName must not be empty or contain only spaces.", [nameof(ShiftName)]);
        else if (ShiftName.Trim().Length > 150)
            yield return new ValidationResult("shiftName must not exceed 150 characters after trimming spaces.", [nameof(ShiftName)]);

        var validStart = TimeOnly.TryParseExact(StartTime, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start);
        var validEnd = TimeOnly.TryParseExact(EndTime, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end);
        if (StartTime is null)
            yield return new ValidationResult("startTime is required. Provide a time such as '08:00:00'.", [nameof(StartTime)]);
        else if (string.IsNullOrWhiteSpace(StartTime))
            yield return new ValidationResult("startTime must not be blank. Use HH:mm:ss format, for example '08:00:00'.", [nameof(StartTime)]);
        else if (!validStart)
            yield return new ValidationResult("startTime must use the 24-hour HH:mm:ss format, for example '08:00:00'. Hours must be 00-23; minutes and seconds must be 00-59.", [nameof(StartTime)]);
        if (EndTime is null)
            yield return new ValidationResult("endTime is required. Provide a time such as '16:00:00'.", [nameof(EndTime)]);
        else if (string.IsNullOrWhiteSpace(EndTime))
            yield return new ValidationResult("endTime must not be blank. Use HH:mm:ss format, for example '16:00:00'.", [nameof(EndTime)]);
        else if (!validEnd)
            yield return new ValidationResult("endTime must use the 24-hour HH:mm:ss format, for example '16:00:00'. Hours must be 00-23; minutes and seconds must be 00-59.", [nameof(EndTime)]);
        if (validStart && validEnd && end == start)
            yield return new ValidationResult("endTime must not equal startTime. Choose a later end time.", [nameof(EndTime)]);
        else if (validStart && validEnd && end < start)
            yield return new ValidationResult("endTime must be later than startTime on the same day. Overnight shifts are not supported.", [nameof(EndTime)]);

        if (Description?.Length > 500)
            yield return new ValidationResult("description must not exceed 500 characters. Shorten it or omit this optional field.", [nameof(Description)]);
    }

    public static string GetBodyError(string field) => field.ToLowerInvariant() switch
    {
        "starttime" => "startTime must be a JSON string in HH:mm:ss format, for example '08:00:00'.",
        "endtime" => "endTime must be a JSON string in HH:mm:ss format, for example '16:00:00'.",
        "shiftname" => "shiftName must be a JSON string, for example 'Morning Shift'.",
        "description" => "description must be a JSON string or null.",
        _ => "The request body must be a valid JSON object. Use double quotes for property names and string values, and check commas and braces."
    };
}
