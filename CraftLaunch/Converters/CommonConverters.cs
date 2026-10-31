using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CraftLaunch.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToIndexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 0 : 1;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int index && index == 0;
}
