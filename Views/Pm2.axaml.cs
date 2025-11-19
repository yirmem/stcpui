using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using stcpui.ViewModels;

namespace stcpui.Views;

public partial class Pm2 : UserControl
{
    public Pm2()
    {
        InitializeComponent();
        var services = App.Services; // 确保你的App类提供了对Services的访问
        this.DataContext = services.GetRequiredService<Pm2ViewModel>();
    }
    
    private async void OpenFileButton_Clicked(object sender, RoutedEventArgs e)
    {
        // 获取当前窗口的 TopLevel 引用以访问 StorageProvider
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel != null)
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择一个文件",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                // 获取文件路径
                // 注意：TryGetLocalPath() 可能在某些非桌面平台返回 null，但在桌面端通常有效
                var filePath = files[0].Path.LocalPath;

                // 将结果传递给 ViewModel
                if (DataContext is Pm2ViewModel vm)
                {
                }
            }
        }
    }
}