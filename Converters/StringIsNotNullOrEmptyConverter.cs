using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace stcpui.Converters;

public class StringIsNotNullOrEmptyConverter : IValueConverter
{
    public static StringIsNotNullOrEmptyConverter Instance { get; } = new();
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value?.ToString());
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}