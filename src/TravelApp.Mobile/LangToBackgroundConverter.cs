using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace TravelApp;

public class LangToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var selected = value as string ?? string.Empty;
        var param = parameter as string ?? string.Empty;
        if (string.Equals(selected, param, StringComparison.OrdinalIgnoreCase))
        {
            return Color.FromArgb("#00CED1");
        }

        return Color.FromArgb("#212121");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
