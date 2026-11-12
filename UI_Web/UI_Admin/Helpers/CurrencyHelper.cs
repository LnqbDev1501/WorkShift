using System.Globalization;

namespace UI_Admin.Helpers;

public static class CurrencyHelper
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string FormatNumber(decimal amount) => amount.ToString("N0", Vietnamese);

    public static string FormatVnd(decimal amount) => $"{amount.ToString("N0", Vietnamese)} ₫";
}
