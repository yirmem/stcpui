using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace stcpui.Converters;

// 根据布尔值切换颜色：true 绿色，false 灰色（用于服务运行状态指示灯）
public class BoolToStatusBrushConverter : IValueConverter
{
    public static BoolToStatusBrushConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.Parse("#28A745"))
            : new SolidColorBrush(Color.Parse("#B0BEC5"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}