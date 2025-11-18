using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using stcpui.Helper;
using stcpui.ViewModels;
using stcpui.Views;

namespace stcpui;

public partial class App : Application
{
    public new static App Current => (App)Application.Current!;
    public IServiceProvider Services { get; private set; }
    
    public override void Initialize()
    {
        //AvaloniaXamlLoader.Load(this);
        // 初始化DI容器
        var serviceCollection = new ServiceCollection();
        // 注册ViewModel（如果需要由容器创建）
        serviceCollection.AddTransient<ModbusClientViewModel>();
        
        // 构建ServiceProvider
        Services = serviceCollection.BuildServiceProvider();
    }
    

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var viewModel = Services.GetRequiredService<ModbusClientViewModel>();
            desktop.MainWindow = new MainWindow
            {
                //DataContext = new MainWindowViewModel(),
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}