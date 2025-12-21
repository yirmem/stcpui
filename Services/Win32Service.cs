using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace stcpui.Services;

public class Win32Service
{
    public static void UpdateRegeditValue(string regeditPath,string valueName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        string subKeyPath = @"SOFTWARE\"+regeditPath;
        int targetValue = 0; // 目标值

        // 重要：由于修改的是 HKEY_LOCAL_MACHINE (HKLM)，需要管理员权限
        RegistryKey? baseKey = null;
        RegistryKey? targetSubKey = null;

        try
        {
            // 1. 打开 HKLM 基键
            baseKey = Registry.LocalMachine;
            
            // 2. 以可写方式打开目标子项 (参数 true 表示可写)
            targetSubKey = baseKey.OpenSubKey(subKeyPath, true);
            
            if (targetSubKey == null)
            {
                // 如果子项不存在，可以在此创建，但鉴于目标路径通常是系统已有的，先考虑路径错误或权限不足
                throw new Exception($"注册表路径 '{subKeyPath}' 未找到。请检查路径是否正确，并确保程序以管理员权限运行。");
            }

            // 3. 设置 DWORD 值
            targetSubKey.SetValue(valueName, targetValue, RegistryValueKind.DWord);
            
            Console.WriteLine($"成功将 {valueName} 的值修改为 {targetValue}。");
        }
        catch (UnauthorizedAccessException ex)
        {
            // 最常见的异常：权限不足
            throw new UnauthorizedAccessException("修改注册表失败：权限不足。请确保以管理员身份运行此程序。", ex);
        }
        catch (Exception ex)
        {
            // 其他异常（如路径不存在、数据类型错误等）
            throw new Exception($"修改注册表时发生错误: {ex.Message}", ex);
        }
        finally
        {
            // 4. 关闭注册表键，释放资源
            targetSubKey?.Close();
            baseKey?.Close();
        }
    }
}