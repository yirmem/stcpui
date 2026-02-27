using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using stcpui.Models;
using stcpui.Repository;
using stcpui.Services;
using stcpui.Views;

namespace stcpui.ViewModels;

public partial class TcpClientViewModel:ViewModelBase
{
    // 输入属性
    [ObservableProperty]
    private string _ipAddress = "127.0.0.1";
    
    [ObservableProperty]
    private int _port = 8880;
    
    [ObservableProperty]
    private bool _isConnected = false;
    
    [ObservableProperty]
    private string _connectStatus = "未连接";
    
    [ObservableProperty]
    private ObservableCollection<string> _ipItems = new ObservableCollection<string>();

    [ObservableProperty]
    private string _connectBtnText = "连接";
    
    [ObservableProperty]
    private string _statusMessage = "等待消息";
    
    [ObservableProperty]
    private string _sendMessage = "";
    
    [ObservableProperty]
    private bool _sendType = true;
    
    [ObservableProperty]
    private bool _recvType = true;
    
    [ObservableProperty]
    private bool _hasData = true;
    
    [ObservableProperty]
    private string _receivedData = "";
    
    // 数据结果
    public ObservableCollection<TcpDataResultItem> DataResults { get; } = new();
   
    private readonly TcpService _tcpService;
    private readonly ITcpClientRepository _tcpClientRepository;
    
    public TcpClientViewModel(ITcpClientRepository tcpClientRepository)
    {
        _tcpClientRepository = tcpClientRepository;
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
        IpItems = new ObservableCollection<string>();
        var tcpList = await _tcpClientRepository.GetAllAsync();
        foreach (var tcpItem in tcpList)
        {
            IpAddress =  tcpItem.Ip;
            Port =  tcpItem.Port;
            IpItems.Add(IpAddress);
            _Id = tcpItem.Id;
            SendType = Convert.ToBoolean(tcpItem.SendType);
            RecvType = Convert.ToBoolean(tcpItem.RecvType);
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
            TcpClientModel mc = new TcpClientModel();
            mc.Ip = IpAddress;
            mc.Port = Port;
            
            var tcpModel = await _tcpClientRepository.GetByIdAsync(_Id);
            if (tcpModel.Ip == mc.Ip)
            {
                mc.Id = _Id;
            }
            
            await _tcpClientRepository.UpSertAsync(mc);
            ConnectStatus = "正在连接...";
            var success = await _tcpService.ConnectAsync(IpAddress, Port);
            ConnectBtnText = "断开";
            // 连接结果会通过ConnectionStatusChanged事件更新IsConnected和StatusMessage
            if (!success)
            {
                ConnectStatus = "连接失败";
                ConnectBtnText = "连接";
            }

            LoadDataAsync();

        }
        
    }
    // 读取数据命令
    [RelayCommand]
    public async Task ReadDataAsync()
    {
        var tcpDataResultItem = new TcpDataResultItem
        {
            Content = SendMessage,
            Author = "Me",
            Time = DateTime.Now.ToString("HH:mm:ss")
        };
        DataResults.Add(tcpDataResultItem);
        SomeMethodThatRequiresScrolling();
        var success = "";
        try
        {
            success = await _tcpService.SendDataAsync(SendMessage,SendType);
        }
        catch (Exception e)
        {
            success = e.Message;
        }
        StatusMessage = success;
    }
    
    private void OnTcpServiceConnectionStatusChanged(object sender, bool isConnected)
    {
        IsConnected = isConnected;
        ConnectStatus = isConnected ? "已连接" : "已断开";
        ConnectBtnText = isConnected ? "断开" : "连接";
    }

    private void OnTcpServiceDataReceived(object sender, byte[] data)
    {
        string hexString = BitConverter.ToString(data);
        string cleanHexString = hexString.Replace("-", " ");
        
        var tcpDataResultItem = new TcpDataResultItem
        {
            Content = cleanHexString,
            Author = IpAddress+":"+Port,
            Time = DateTime.Now.ToString("HH:mm:ss")
        };
        DataResults.Add(tcpDataResultItem);
        
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = $"收到数据: {cleanHexString}";
            SomeMethodThatRequiresScrolling();
        });
    }
    
    private void SomeMethodThatRequiresScrolling()
    {
        // ... 业务逻辑，例如向集合添加新项 ...

        // 发送滚动消息
        MessageBus.Current.SendMessage(new ScrollToEndMessage());
    }

    // 记得在ViewModel销毁时释放资源
    public void Dispose()
    {
        _tcpService?.Dispose();
    }
    
}


// 数据结果项
public class TcpDataResultItem
{
    public string Time { get; set; } = "";
    public string Content { get; set; } = "";
    public string Author { get; set; } = "";
}
