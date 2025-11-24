using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using stcpui.Models;
using stcpui.Repository;
using stcpui.Services;

namespace stcpui.ViewModels;

public partial class ModbusClientViewModel:ViewModelBase
{
    // 输入属性
    [ObservableProperty]
    private string _ipAddress = "127.0.0.1";
    
    [ObservableProperty]
    private int _port = 504;
    
    [ObservableProperty]
    private string? _deviceAddress = "1";
    public int? DeviceAddressValue
    {
        get
        {
            if (int.TryParse(DeviceAddress, out int result))
            {
                return result;
            }
            return 1; // 或者一个默认值
        }
    }
    
    [ObservableProperty]
    private FunctionCodeItem _functionCode;
    
    [ObservableProperty]
    private string _startAddress = "0000";
    
    [ObservableProperty]
    private string? _readLength = "10";
    
    public ushort ReadLengthValue
    {
        get
        {
            if (ushort.TryParse(ReadLength, out ushort result))
            {
                return result;
            }
            return 10; // 或者一个默认值
        }
    }
    
    [ObservableProperty]
    private bool _isConnected = false;
    
    [ObservableProperty]
    private string _statusMessage = "未连接";
    
    [ObservableProperty]
    private bool _hasData = true;
    
    [ObservableProperty]
    private bool _isSinged = true;
    
    // 功能码列表
    public ObservableCollection<FunctionCodeItem> FunctionCodes { get; } = new()
    {
        new FunctionCodeItem { Code = "03", Name = "03 - 读取保持寄存器" },
        new FunctionCodeItem { Code = "04", Name = "04 - 读取输入寄存器" }
    };
    
    // 数据结果
    public ObservableCollection<DataResultItem> DataResults { get; } = new();
   
    private readonly TcpService _tcpService;
    private readonly IModbusRepository _modbusRepository;
    
    public ModbusClientViewModel(IModbusRepository modbusRepository)
    {
        _modbusRepository = modbusRepository;
        // 设置默认功能码
        FunctionCode = FunctionCodes.First();
        _tcpService = new TcpService();
        // 订阅TCP服务的事件
        _tcpService.ConnectionStatusChanged += OnTcpServiceConnectionStatusChanged;
        _tcpService.DataReceived += OnTcpServiceDataReceived;
        LoadDataCommand.Execute(null);
    }

    private long _Id = 0;
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var modbusList = await _modbusRepository.GetAllAsync();
        foreach (var modbusItem in modbusList)
        {
            IpAddress =  modbusItem.Ip;
            Port =  modbusItem.Port;
            StartAddress =  modbusItem.StartAddress;
            ReadLength =  modbusItem.ReadLength+"";
            DeviceAddress =  modbusItem.DeviceAddress+"";
            _Id = modbusItem.Id;
        }
    }
    
    [RelayCommand]
    public async Task ToggleConnectionAsync()
    {
        if (IsConnected)
        {
            _tcpService.Disconnect();
        }
        else
        {
            ModbusModel mc = new ModbusModel();
            mc.Ip = IpAddress;
            mc.Port = Port;
            mc.ReadLength = ReadLengthValue;
            mc.StartAddress = StartAddress;
            mc.DeviceAddress = DeviceAddressValue;
            if (_Id != 0)
            {
                mc.Id = _Id;
                await _modbusRepository.UpdateAsync(mc);
            }
            else
            {
                await _modbusRepository.InsertAsync(mc);
            }
            StatusMessage = "正在连接...";
            var success = await _tcpService.ConnectAsync(IpAddress, Port);
            // 连接结果会通过ConnectionStatusChanged事件更新IsConnected和StatusMessage
            if (!success)
            {
                StatusMessage = "连接失败，请检查地址、端口及服务端状态";
            }
            
        }
        
    }
    // 读取数据命令
    [RelayCommand]
    public async Task ReadDataAsync()
    {
        var success = await _tcpService.SendDataAsync(BuildModbusRequestFrame(),true);
        StatusMessage = success;
    }
    
    private void OnTcpServiceConnectionStatusChanged(object sender, bool isConnected)
    {
        IsConnected = isConnected;
        StatusMessage = isConnected ? "已连接" : "已断开";
    }

    private void OnTcpServiceDataReceived(object sender, byte[] data)
    {
        string hexString = BitConverter.ToString(data);
        string cleanHexString = hexString.Replace("-", "");
        // 处理接收到的数据，例如更新界面或记录日志
        // 注意：可能需要从UI线程调度，确保线程安全
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 这里处理接收到的数据，例如将其添加到日志或特定属性中
            // ReceivedData += $"{DateTime.Now}: {data}\n";
            StatusMessage = $"收到数据: {cleanHexString}";
            ParseModbusResponse(data, IsSinged?NumericFormat.Signed:NumericFormat.Unsigned);
        });
    }

    // 记得在ViewModel销毁时释放资源
    public void Dispose()
    {
        _tcpService?.Dispose();
    }
    public string BuildModbusRequestFrame()
    {
        // 1. 将各属性转换为字节
        byte deviceAddressByte = (byte)DeviceAddressValue;
        byte functionCodeByte = Convert.ToByte(_functionCode.Code, 16); // 假设FunctionCodeItem有一个Code属性表示十六进制字符串

        // 起始地址和读取长度需要转换为2个字节（高位在前）
        ushort startAddressValue = ushort.Parse(_startAddress, System.Globalization.NumberStyles.HexNumber);
        byte[] startAddressBytes = new byte[] { (byte)(startAddressValue >> 8), (byte)startAddressValue };

        ushort readLengthValue = ReadLengthValue;
        byte[] readLengthBytes = new byte[] { (byte)(readLengthValue >> 8), (byte)readLengthValue };

        // 2. 构建报文主体（不含CRC）
        List<byte> frameBytes = new List<byte>();
        frameBytes.Add(deviceAddressByte);
        frameBytes.Add(functionCodeByte);
        frameBytes.AddRange(startAddressBytes);
        frameBytes.AddRange(readLengthBytes);

        // 3. 计算CRC16校验码（Modbus格式，低字节在前）
        byte[] crc = CalculateCRC16(frameBytes.ToArray());
        frameBytes.AddRange(crc);

        // 4. 将整个报文转换为十六进制字符串
        StringBuilder hexString = new StringBuilder();
        foreach (byte b in frameBytes)
        {
            hexString.Append(b.ToString("X2")); // 格式化为两位大写十六进制
        }

        return hexString.ToString();
    }

    private byte[] CalculateCRC16(byte[] data)
    {
        ushort crc = 0xFFFF;
        for (int i = 0; i < data.Length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        // 返回CRC，低字节在前
        return new byte[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
    }
    
    // 添加枚举类型定义符号格式
    public enum NumericFormat
    {
        Unsigned,    // 无符号整数
        Signed       // 有符号整数（使用二进制补码表示）
    }

    // 修改解析方法，添加numericFormat参数
    public void ParseModbusResponse(byte[] data, NumericFormat numericFormat = NumericFormat.Signed)
    {
        // 清空现有数据
        DataResults.Clear();

        if (data == null || data.Length < 5)
        {
            // 处理错误：数据长度不足
            return;
        }

        byte slaveAddress = data[0];
        byte functionCode = data[1];

        // 检查是否为异常响应（功能码最高位为1）
        if ((functionCode & 0x80) != 0)
        {
            // 处理异常响应
            byte errorCode = data[2];
            // 可以在这里记录错误信息
            return;
        }

        // 解析03和04功能码的响应
        if (functionCode == 0x03 || functionCode == 0x04)
        {
            // 数据字节数（表示后面跟随的数据字节数量）
            byte byteCount = data[2];
        
            // 计算寄存器数量：每个寄存器2字节，所以寄存器数量 = byteCount / 2
            int registerCount = byteCount / 2;

            // 检查数据长度是否匹配
            if (data.Length < 3 + byteCount + 2) // 地址1+功能码1+字节数1+数据+CRC2
            {
                // 数据长度不匹配
                return;
            }

            // 解析每个寄存器值
            for (int i = 0; i < registerCount; i++)
            {
                // 每个寄存器占2字节，大端序（高位在前，低位在后）
                ushort rawValue = (ushort)((data[3 + 2 * i] << 8) | data[4 + 2 * i]);
                int address = i;  // 地址从0开始递增

                int finalValue;
                string valueType;
                
                // 根据选择的数值格式进行解析
                if (numericFormat == NumericFormat.Signed)
                {
                    // 有符号整数解析（二进制补码）
                    finalValue = (short)rawValue; // 直接转换为有符号short
                    valueType = "有符号";
                }
                else
                {
                    // 无符号整数解析（默认）
                    finalValue = rawValue;
                    valueType = "无符号";
                }

                DataResultItem item = new DataResultItem
                {
                    Address = address,
                    Value = finalValue,
                    DisplayText = $"{address:D4}: {finalValue} ",
                    BackgroundColor = numericFormat == NumericFormat.Signed ? "LightYellow" : "White"
                };

                DataResults.Add(item);
            }
        }
    }
    
}
    
// 功能码数据模型
public class FunctionCodeItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    public override string ToString() => Name;
}

// 数据结果项
public class DataResultItem
{
    public int Address { get; set; }
    public int Value { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "White";
}
