using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using stcpui.ViewModels;

namespace stcpui.Views;

public partial class TcpClient : UserControl
{
    public TcpClient()
    {
        InitializeComponent();
        var services = App.Services; // 确保你的App类提供了对Services的访问
        this.DataContext = services.GetRequiredService<TcpClientViewModel>();
        MessageBus.Current.Listen<ScrollToEndMessage>().Subscribe(OnScrollToEndMessage);
        
    }
    
    private void OnScrollToEndMessage(ScrollToEndMessage msg)
    {
        // 找到名为 "MessageScrollViewer" 的 ScrollViewer 组件
        var scrollViewer = this.FindControl<ScrollViewer>("MessageScrollViewer");
        // 调用滚动到底部的方法
        scrollViewer?.ScrollToEnd();
    }
    
}

public class ScrollToEndMessage
{
    // 可以添加一些属性，比如标识哪个ScrollViewer，但简单场景下留空即可
}