using System;
using System.Globalization;
using System.Windows.Data;

namespace MDXAMLUI.Converters;

public class BooleanStateSwitchingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !((bool)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !((bool)value);
    }
}
