using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using stcpui.Models;

namespace stcpui.Services;

public class Pm2Service
{
    public static async Task<string> ExecutePm2CommandAsync(string arguments,string workDir)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pm2",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            startInfo.EnvironmentVariables["PATH"] = "/usr/local/bin:" + Environment.GetEnvironmentVariable("PATH");
            startInfo.WorkingDirectory = "/usr/local/bin";
            startInfo.FileName = "/usr/local/bin/pm2";
            if (workDir != "")
            {
                startInfo.EnvironmentVariables["PATH"] = workDir+":" + Environment.GetEnvironmentVariable("PATH");
                startInfo.WorkingDirectory = workDir;
                startInfo.FileName = workDir+"/pm2";
            }
        }else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.WorkingDirectory = @"C:\Users\Sheng\AppData\Roaming\npm";
            startInfo.FileName = "pm2.cmd";
            if (workDir != "")
            {
                startInfo.WorkingDirectory = workDir;
            }
        }
        
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"PM2命令执行失败: {error}");
        }
        return output;
    }

    public static List<Pm2App> ParsePm2JListOutput(string jsonOutput)
    {
        var processes = new List<Pm2App>();
        try
        {
            // 使用System.Text.Json解析JSON输出
            using JsonDocument document = JsonDocument.Parse(jsonOutput);
            JsonElement root = document.RootElement;

            long tempUpTime = 0;

            foreach (JsonElement element in root.EnumerateArray())
            {
                var app = new Pm2App
                {
                    Id = element.TryGetProperty("pm_id", out var idElement) ? idElement.GetInt32() : 0,
                    Name = element.TryGetProperty("name", out var nameElement) ? nameElement.GetString()?? "未知":"未知",
                    Status = element.TryGetProperty("pm2_env", out var envElement) && 
                             envElement.TryGetProperty("status", out var statusElement) ? 
                             statusElement.GetString() ?? "unknown" : "unknown",
                    UpTime = element.TryGetProperty("pm2_env", out var pm2EnvElement) && 
                             pm2EnvElement.TryGetProperty("pm_uptime", out var upTimeElement) ? 
                        DateTimeOffset.FromUnixTimeMilliseconds(upTimeElement.GetInt64()).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss") : "N/A"
                };

                // 处理CPU和内存信息
                if (element.TryGetProperty("monit", out var monitElement))
                {
                    app.Cpu = monitElement.TryGetProperty("cpu", out var cpuElement) ? 
                             $"{cpuElement.GetSingle():F1}%" : "0%";
                    
                    app.Memory = monitElement.TryGetProperty("memory", out var memoryElement) ? 
                               $"{memoryElement.GetInt64() / 1024.0 / 1024.0:F1}MB" : "0MB";
                }
                else
                {
                    app.Cpu = "0%";
                    app.Memory = "0MB";
                }
                processes.Add(app);
            }
        }
        catch (JsonException ex)
        {
            throw new Exception($"解析PM2输出失败: {ex.Message}");
        }

        return processes;
    }
}