using System.Globalization;
using System.Windows.Data;
using TobiFund.Application.Common;

namespace TobiFund.WPF.Converters;

/// <summary>مبدل تاریخ میلادی به شمسی (پارامتر "datetime" تاریخ و ساعت را هم نمایش می‌دهد)</summary>
public class PersianDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var withTime = parameter?.ToString() == "datetime";
        // note: boxed DateTime? with a value is boxed as DateTime, so one check covers both
        if (value is DateTime dt)
            return withTime
                ? PersianDateHelper.ToPersianDateTime(dt)
                : PersianDateHelper.ToPersianDate(dt);
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
