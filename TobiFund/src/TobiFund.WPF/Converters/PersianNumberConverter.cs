using System.Globalization;
using System.Windows.Data;
using TobiFund.Application.Common;

namespace TobiFund.WPF.Converters;

/// <summary>مبدل اعداد به رشته با جداکننده هزارگان و ارقام فارسی</summary>
public class PersianNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value switch
        {
            null => string.Empty,
            decimal d => d.ToString("N0", CultureInfo.InvariantCulture),
            double d => d.ToString("N0", CultureInfo.InvariantCulture),
            float f => f.ToString("N0", CultureInfo.InvariantCulture),
            int i => i.ToString("N0", CultureInfo.InvariantCulture),
            long l => l.ToString("N0", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
        return PersianDateHelper.ToPersianDigits(text);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
