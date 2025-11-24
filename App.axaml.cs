using System;
using System.Data;
using System.Data.SQLite;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using stcpui.Helper;
using stcpui.Repository;
using stcpui.ViewModels;
using stcpui.Views;

namespace stcpui;

public partial class App : Application
{
    public new static App Current => (App)Application.Current!;
    public static IServiceProvider Services { get; private set; }
    
    public override void Initialize()
    {
        //AvaloniaXamlLoader.Load(this);
        // 初始化DI容器
        var serviceCollection = new ServiceCollection();
        
        // 注册数据库连接
        serviceCollection.AddScoped<IDbConnection>(provider => 
        {
            var connection = new SQLiteConnection("Data Source=stcp.db;");
            connection.Open();
            // 创建用户表
            const string createUserTableSql = @"
            CREATE TABLE IF NOT EXISTS ModbusModel (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Ip TEXT DEFAULT '127.0.0.1',
                Port INTEGER DEFAULT 503,
                DeviceAddress INTEGER DEFAULT 1,
                StartAddress TEXT DEFAULT '0000',
                ReadLength INTEGER DEFAULT 10,
                CreateTime DATETIME DEFAULT (datetime('now', 'localtime'))
            )";
            connection.Execute(createUserTableSql);
            return connection;
        });
        
        // 注册泛型仓储
        serviceCollection.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        // 注册特定仓储
        serviceCollection.AddScoped<IModbusRepository, ModbusRepository>();
        
        // 注册ViewModel（如果需要由容器创建）
        serviceCollection.AddTransient<MainWindowViewModel>();
        serviceCollection.AddTransient<ModbusClientViewModel>();
        serviceCollection.AddTransient<Pm2ViewModel>();
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
            var viewModel = Services.GetRequiredService<MainWindowViewModel>();
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