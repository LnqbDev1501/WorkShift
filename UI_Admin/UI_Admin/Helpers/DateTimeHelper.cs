using System;

namespace UI_Admin.Helpers;

public static class DateTimeHelper
{
    /// <summary>
    /// Converts a UTC DateTime to Vietnam time (UTC+7).
    /// </summary>
    public static DateTime ToVietnamTime(this DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return utc.AddHours(7);
    }

    /// <summary>
    /// Formats a UTC DateTime to Vietnam date time string (dd/MM/yyyy HH:mm).
    /// </summary>
    public static string ToVnDateTimeString(this DateTime utcDateTime, string format = "dd/MM/yyyy HH:mm")
    {
        return utcDateTime.ToVietnamTime().ToString(format);
    }

    /// <summary>
    /// Formats a nullable UTC DateTime to Vietnam date time string (dd/MM/yyyy HH:mm).
    /// </summary>
    public static string ToVnDateTimeString(this DateTime? utcDateTime, string format = "dd/MM/yyyy HH:mm", string fallback = "—")
    {
        return utcDateTime.HasValue ? utcDateTime.Value.ToVnDateTimeString(format) : fallback;
    }

    /// <summary>
    /// Formats a UTC DateTime to Vietnam date string (dd/MM/yyyy).
    /// </summary>
    public static string ToVnDateString(this DateTime utcDateTime, string format = "dd/MM/yyyy")
    {
        return utcDateTime.ToVietnamTime().ToString(format);
    }

    /// <summary>
    /// Formats a UTC DateTime to Vietnam time string (HH:mm).
    /// </summary>
    public static string ToVnTimeString(this DateTime utcDateTime, string format = "HH:mm")
    {
        return utcDateTime.ToVietnamTime().ToString(format);
    }

    /// <summary>
    /// Formats day of week for submission deadline rule.
    /// </summary>
    public static string FormatDayOfWeek(short? dayOfWeek)
    {
        if (!dayOfWeek.HasValue) return "Not applicable";
        return dayOfWeek.Value switch
        {
            0 => "Sunday",
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            7 => "Sunday",
            _ => $"Day {dayOfWeek.Value}"
        };
    }
}
