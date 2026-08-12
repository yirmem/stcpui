using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using stcpui.ViewModels;

namespace stcpui.Views;

public partial class TcpServer : UserControl
{
    public TcpServer()
    {
        InitializeComponent();
        var services = App.Services; // 确保你的App类提供了对Services的访问
        this.DataContext = services.GetRequiredService<TcpServerViewModel>();
        MessageBus.Current.Listen<ScrollToEndMessage>().Subscribe(new ScrollObserver(OnScrollToEndMessage));
    }

    private void OnScrollToEndMessage(ScrollToEndMessage msg)
    {
        // 找到名为 "MessageScrollViewer" 的 ScrollViewer 组件
        var scrollViewer = this.FindControl<ScrollViewer>("MessageScrollViewer");
        // 调用滚动到底部的方法
        scrollViewer?.ScrollToEnd();
    }

    // 用于订阅 MessageBus 的简单 IObserver 适配器
    private sealed class ScrollObserver : IObserver<ScrollToEndMessage>
    {
        private readonly Action<ScrollToEndMessage> _action;
        public ScrollObserver(Action<ScrollToEndMessage> action) => _action = action;
        public void OnNext(ScrollToEndMessage value) => _action(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    public void Dispose()
    {
        (DataContext as TcpServerViewModel)?.Dispose();
    }
}