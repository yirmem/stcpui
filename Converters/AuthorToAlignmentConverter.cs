using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;

namespace stcpui.Converters;

// 根据Author判断是否居右对齐
    public class AuthorToAlignmentConverter : IValueConverter
    {
        public static AuthorToAlignmentConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == "Me" ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 根据Author设置不同的背景色
    public class AuthorToBackgroundConverter : IValueConverter
    {
        public static AuthorToBackgroundConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "Me")
            {
                // 自己消息的背景色 - 浅蓝色
                return new SolidColorBrush(Color.Parse("#E3F2FD"));
            }
            else
            {
                // 他人消息的背景色 - 浅灰色
                return new SolidColorBrush(Color.Parse("#F5F5F5"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 根据Author设置文字颜色
    public class AuthorToForegroundConverter : IValueConverter
    {
        public static AuthorToForegroundConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "Me")
            {
                // 自己消息的文字颜色
                return new SolidColorBrush(Color.Parse("#1565C0"));
            }
            else
            {
                // 他人消息的文字颜色
                return new SolidColorBrush(Color.Parse("#424242"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }