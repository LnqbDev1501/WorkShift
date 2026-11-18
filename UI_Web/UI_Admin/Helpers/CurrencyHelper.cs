using System.Globalization;

namespace UI_Admin.Helpers;

public static class CurrencyHelper
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string FormatNumber(decimal amount)
    {
        var rounded = Math.Abs(amount) < 0.5m ? 0m : amount;
        return rounded.ToString("#,##0", Vietnamese);
    }

    public static string FormatVnd(decimal amount)
    {
        var rounded = Math.Abs(amount) < 0.5m ? 0m : amount;
        return $"{rounded.ToString("#,##0", Vietnamese)} ₫";
    }
}
