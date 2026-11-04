using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TobiFund.WPF.Converters;

/// <summary>تبدیل اندیس (int) به Visibility — برای نمایش تب/گزارش انتخاب‌شده</summary>
public class IndexToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int index) return Visibility.Collapsed;
        var target = parameter switch
        {
            string s when int.TryParse(s, out var p) => p,
            int p => p,
            _ => -1
        };
        return index == target ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility && visibility == Visibility.Visible)
        {
            return parameter switch
            {
                string s when int.TryParse(s, out var p) => p,
                int p => p,
                _ => 0
            };
        }
        return 0;
    }
}
