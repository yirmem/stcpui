using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using stcpui.Models;
using stcpui.Repository;
using stcpui.Services;

namespace stcpui.ViewModels;

public partial class Pm2ViewModel:ViewModelBase
{
    [ObservableProperty]
    private string _consoleOutput = "等待操作...";
    [ObservableProperty]
    private string _pm2WorkDir = "";
    [ObservableProperty] private string _selectedFilePath = string.Empty;
    [ObservableProperty] private bool _isPm2Installed = false;
    
    // 表格数据源
    public ObservableCollection<Pm2App> Apps { get; } = new();
    private readonly IPm2WorkRepository _pm2Repository;

    public Pm2ViewModel(IPm2WorkRepository pm2WorkRepository)
    {
        _pm2Repository = pm2WorkRepository;
        // 初始化时检查环境
        CheckEnvironmentCommand.Execute(null);
    }

    [RelayCommand]
    public async Task CheckEnvironmentAsync()
    {
        try
        {
            ConsoleOutput = "正在检查 Node 和 PM2 环境...";
            // 模拟检查过程，实际请调用 cmd/bash 执行 "pm2 -v"
            // var version = await RunProcessAsync("pm2", "-v");
            
            //await Task.Delay(500); // 模拟耗时
            bool isInstalled = true; // 假设已安装

            if (isInstalled)
            {
                IsPm2Installed = true;
                ConsoleOutput = "环境检查通过。";
                await RefreshListAsync();
            }
            else
            {
                IsPm2Installed = false;
                ConsoleOutput = "未检测到 PM2，请先安装。";
            }
        }
        catch (Exception ex)
        {
            ConsoleOutput = $"检查失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SavePm2WorkDir()
    {
        try
        {
            Pm2WorkModel wm = new Pm2WorkModel();
            wm.Id = 1;
            wm.WorkDir = Pm2WorkDir;
            await _pm2Repository.UpSertAsync(wm);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshListAsync()
    {
        if (!IsPm2Installed) return;
        Apps.Clear();
        var pm2WorkList = await _pm2Repository.GetAllAsync();
        foreach (var wm in pm2WorkList)
        {
            Pm2WorkDir = wm.WorkDir;
        }
        try
        {
            // 执行 pm2 jlist 命令
            string jsonOutput = await Pm2Service.ExecutePm2CommandAsync("jlist",Pm2WorkDir);
            if (string.IsNullOrEmpty(jsonOutput))
            {
                ConsoleOutput = "未能获取PM2进程列表";
                return;
            }
            // 反序列化JSON并添加到Apps集合
            var pm2Processes = Pm2Service.ParsePm2JListOutput(jsonOutput);
        
            foreach (var process in pm2Processes)
            {
                Apps.Add(process);
            }
            ConsoleOutput = $"列表已刷新，共{Apps.Count}个进程";
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            ConsoleOutput = $"刷新列表时出错: {ex.Message}";
        }
    }
    // 传入 Pm2App 对象
    [RelayCommand]
    public void ViewLog(Pm2App app)
    {
        ConsoleOutput = $"正在查看 ID:{app.Id} ({app.Name}) 的日志...";
    
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows 方案保持不变
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c pm2 logs {app.Id}",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
    
                Process.Start(startInfo);
                ConsoleOutput += "\nCMD 已启动，正在显示日志...";
            }
            else
            {
                ConsoleOutput = "错误：当前操作系统不支持，仅支持 Windows。";
            }
        }
        catch (Exception ex)
        {
            ConsoleOutput = $"错误：无法启动终端进程 - {ex.Message}";
        }
    }
    
    

    [RelayCommand]
    public async Task StopAppAsync(Pm2App app)
    {
        ConsoleOutput = $"正在停止 {app.Name} ...";
        await Pm2Service.ExecutePm2CommandAsync($"stop {app.Id}",Pm2WorkDir);
        await RefreshListAsync();
        await Pm2Service.ExecutePm2CommandAsync($"save",Pm2WorkDir);
    }

    [RelayCommand]
    public async Task RestartAppAsync(Pm2App app)
    {
        ConsoleOutput = $"正在重启 {app.Name} ...";
        await Pm2Service.ExecutePm2CommandAsync($"restart {app.Id}",Pm2WorkDir);
        await RefreshListAsync();
        await Pm2Service.ExecutePm2CommandAsync($"save",Pm2WorkDir);
    }
    
    [RelayCommand]
    public async Task DeleteAppAsync(Pm2App app)
    {
        ConsoleOutput = $"正在删除 {app.Name} ...";
        await Pm2Service.ExecutePm2CommandAsync($"delete {app.Id}",Pm2WorkDir);
        await RefreshListAsync();
        await Pm2Service.ExecutePm2CommandAsync($"save",Pm2WorkDir);
    }

    // 文件选择命令，接收 TopLevel 参数
    [RelayCommand]
    public async Task SelectFileAsync(TopLevel? topLevel)
    {
        if (topLevel == null)
        {
            // 可选：记录日志或处理 topLevel 为 null 的情况
            ConsoleOutput = "错误：TopLevel 不可用。";
            return;
        }

        try
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择配置文件",
                AllowMultiple = false
            });

            // 用户可能取消了选择，files 集合为空
            if (files != null && files.Count > 0 && files[0] is IStorageFile selectedFile)
            {
                // 优先使用 Path.LocalPath，这是 Avalonia 11 推荐的方式[6,7](@ref)
                SelectedFilePath = selectedFile.Path.LocalPath;

                // 备选方案：通过 Name 属性获取路径[6](@ref)
                // 在某些特定情况下，如果 LocalPath 不可用，可以尝试以下方法：
                // SelectedFilePath = selectedFile.Name; // 这可能只返回文件名，而非完整路径
                var parentFolder = await selectedFile.GetParentAsync();
                string directoryPath = parentFolder.Path.LocalPath;

                Console.WriteLine($"已选择文件: {SelectedFilePath}"); // 输出到调试窗口
                // 如果 ConsoleOutput 用于在应用界面显示，可以保留
                // ConsoleOutput = $"已选择文件: {SelectedFilePath}"; 
                await Pm2Service.ExecutePm2CommandAsync($"start \"{SelectedFilePath}\" --cwd \"{parentFolder}\"",Pm2WorkDir);
                await RefreshListAsync();
                await Pm2Service.ExecutePm2CommandAsync($"save",Pm2WorkDir);
            }
            else
            {
                // 用户取消了文件选择
                Console.WriteLine("文件选择已被取消。");
                // 可选：清除之前选择的路径
                SelectedFilePath = null;
                ConsoleOutput = "未选择文件。";
            }
        }
        catch (Exception ex)
        {
            // 处理可能出现的异常，例如没有文件操作权限等[3,6](@ref)
            ConsoleOutput = $"选择文件时出错：{ex.Message}";
            // 实际项目中建议使用日志框架记录 ex
            Console.WriteLine($"错误：{ex.Message}");
        }
    }
}