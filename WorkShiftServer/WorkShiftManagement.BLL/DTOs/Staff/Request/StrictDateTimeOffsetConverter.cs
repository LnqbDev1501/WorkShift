using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

/// <summary>
/// Converter strictly enforcing that incoming ISO 8601 strings contain explicit timezone offset ('Z' or '+/-HH:mm').
/// Rejects any ambiguous dates without an explicit offset.
/// </summary>
public class StrictDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private static readonly Regex ExplicitOffsetRegex = new(
        @"^.+[T\s]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(Z|[+-]\d{2}(?::?\d{2})?)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var rawString = reader.GetString();
        if (string.IsNullOrWhiteSpace(rawString))
        {
            throw new JsonException("RequestedTime is required.");
        }

        rawString = rawString.Trim();
        if (!ExplicitOffsetRegex.IsMatch(rawString))
        {
            throw new JsonException("RequestedTime must include an explicit timezone offset (e.g. 'Z' or '+07:00'). Values without timezone offsets are not allowed.");
        }

        if (DateTimeOffset.TryParse(rawString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse '{rawString}' as a valid DateTimeOffset.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("O"));
    }
}
