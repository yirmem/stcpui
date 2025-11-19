using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using stcpui.ViewModels;

namespace stcpui.Views;

public partial class ModbusClient : UserControl
{
    public ModbusClient()
    {
        InitializeComponent();
        var services = App.Services; // 确保你的App类提供了对Services的访问
        this.DataContext = services.GetRequiredService<ModbusClientViewModel>();
    }
}