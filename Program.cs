using Avalonia;
using System;
using Serilog;
using Serilog.Events;

namespace stcpui;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 创建 Logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information() // 设置最低日志级别
            .WriteTo.File("logs/all-.txt", rollingInterval: RollingInterval.Month)
            .WriteTo.Logger(l => l
                .Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Error || evt.Level == LogEventLevel.Fatal)
                .WriteTo.File("logs/error-.txt", rollingInterval: RollingInterval.Month)
            )
            .CreateLogger();

        try
        {
            Log.Information("Application Starting Up");
            // ... 其余初始化代码，例如构建主窗口
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start correctly");
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
