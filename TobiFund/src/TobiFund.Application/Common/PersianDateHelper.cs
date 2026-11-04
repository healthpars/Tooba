using System.Globalization;
using System.Text;

namespace TobiFund.Application.Common;

/// <summary>
/// مبدل تاریخ شمسی/میلادی و اعداد فارسی.
/// تاریخ‌ها در دیتابیس به‌صورت Gregorian ذخیره می‌شوند و تبدیل شمسی فقط در این لایه انجام می‌شود.
/// </summary>
public static class PersianDateHelper
{
    private static readonly PersianCalendar Pc = new();

    private static readonly string[] MonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    /// <summary>تبدیل تاریخ میلادی به رشته شمسی (مثلاً ۱۴۰۵/۰۱/۱۰)</summary>
    public static string ToPersianDate(DateTime date) =>
        ToPersianDigits($"{Pc.GetYear(date):0000}/{Pc.GetMonth(date):00}/{Pc.GetDayOfMonth(date):00}");

    /// <summary>تبدیل تاریخ و ساعت میلادی به رشته شمسی</summary>
    public static string ToPersianDateTime(DateTime date) =>
        ToPersianDigits($"{Pc.GetYear(date):0000}/{Pc.GetMonth(date):00}/{Pc.GetDayOfMonth(date):00} {date:HH:mm}");

    public static (int Year, int Month, int Day) GetPersianDateParts(DateTime date) =>
        (Pc.GetYear(date), Pc.GetMonth(date), Pc.GetDayOfMonth(date));

    public static (int Year, int Month) GetPersianYearMonth(DateTime date) =>
        (Pc.GetYear(date), Pc.GetMonth(date));

    public static int CurrentPersianYear => Pc.GetYear(DateTime.Today);

    public static string PersianMonthName(int month) =>
        month is >= 1 and <= 12 ? MonthNames[month - 1] : string.Empty;

    /// <summary>تبدیل تاریخ شمسی به میلادی (روز اضافی به‌صورت Clamp محدود می‌شود)</summary>
    public static DateTime PersianToGregorian(int year, int month, int day)
    {
        var daysInMonth = Pc.GetDaysInMonth(year, month);
        if (day > daysInMonth) day = daysInMonth;
        if (day < 1) day = 1;
        return new DateTime(year, month, day, Pc);
    }

    /// <summary>افزودن ماه شمسی به یک تاریخ</summary>
    public static DateTime AddPersianMonths(DateTime date, int months) => Pc.AddMonths(date, months);

    /// <summary>اولین روز سال شمسی</summary>
    public static DateTime PersianYearStart(int year) => new(year, 1, 1, Pc);

    /// <summary>تجزیه رشته تاریخ شمسی (yyyy/MM/dd) با اعداد فارسی یا انگلیسی</summary>
    public static bool TryParsePersianDate(string? text, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = ToEnglishDigits(text.Trim()).Replace('-', '/').Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out var y) ||
            !int.TryParse(parts[1], out var m) ||
            !int.TryParse(parts[2], out var d))
            return false;
        if (y < 1300 || y > 1500 || m < 1 || m > 12 || d < 1 || d > 31) return false;
        try
        {
            date = PersianToGregorian(y, m, d);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>تجزیه مبلغ وارد شده (اعداد فارسی/انگلیسی، جداکننده هزارگان)</summary>
    public static bool TryParseAmount(string? text, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var cleaned = ToEnglishDigits(text)
            .Replace(",", "")
            .Replace("٬", "")
            .Replace(" ", "")
            .Replace("‌", "");
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    /// <summary>تبدیل ارقام انگلیسی/عربی به ارقام فارسی</summary>
    public static string ToPersianDigits(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (c is >= '0' and <= '9')
                sb.Append((char)(c - '0' + 0x06F0)); // ۰۱۲۳۴۵۶۷۸۹
            else if (c is >= '٠' and <= '٩') // ارقام عربی
                sb.Append((char)(c - '٠' + 0x06F0));
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>تبدیل ارقام فارسی/عربی به ارقام انگلیسی</summary>
    public static string ToEnglishDigits(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (c is >= '۰' and <= '۹')
                sb.Append((char)(c - 0x06F0 + '0'));
            else if (c is >= '٠' and <= '٩')
                sb.Append((char)(c - 0x0660 + '0'));
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
