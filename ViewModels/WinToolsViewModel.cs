using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using stcpui.Services;

namespace stcpui.ViewModels;

public partial class WinToolsViewModel:ViewModelBase
{
    
    public WinToolsViewModel()
    {
        
    }
    
    private readonly CommandExecutorService _executor = new();
    [RelayCommand]
    public async Task ExecCmd(string arguments)
    {
        if (arguments == "netplwiz")
        {
            Win32Service.UpdateRegeditValue(@"Microsoft\Windows NT\CurrentVersion\PasswordLess\Device","DevicePasswordLessBuildVersion");
        }
        var result = await _executor.ExecuteAsync("cmd.exe","/c "+arguments);
    }
    
    [RelayCommand]
    public async Task CloseFireWall()
    {
        await _executor.ExecuteAsync("netsh", "advfirewall set allprofiles state off");
    }
    
    [RelayCommand]
    public void OpenStartupFolder()
    {
        try
        {
            // 获取当前用户的启动文件夹路径
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            if (!string.IsNullOrEmpty(startupPath) && Directory.Exists(startupPath))
            {
                // 使用 Process.Start 打开文件夹
                // UseShellExecute = true 会让操作系统使用默认的文件资源管理器
                Process.Start(new ProcessStartInfo(startupPath)
                {
                    UseShellExecute = true
                });
            }
            else
            {
                Debug.WriteLine("启动文件夹路径为空或不存在。");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开启动文件夹时出错: {ex.Message}");
        }
    }
    
    [RelayCommand]
    public void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }
    
}