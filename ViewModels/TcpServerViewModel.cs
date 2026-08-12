using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using stcpui.Services;
using stcpui.Views;

namespace stcpui.ViewModels;

public partial class TcpServerViewModel : ViewModelBase
{
    public const string BroadcastLabel = "广播 (所有客户端)";

    // 输入属性
    [ObservableProperty]
    private int _port = 8880;

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private string _serverStatus = "未启动";

    [ObservableProperty]
    private string _startBtnText = "启动服务";

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
    private int _clientCount = 0;

    [ObservableProperty]
    private string _selectedClient = BroadcastLabel;

    // 左侧客户端列表（不包含广播项）
    public ObservableCollection<string> ClientList { get; } = new();

    // 发送目标下拉框（首项为广播）
    public ObservableCollection<string> SendTargets { get; } = new();

    // 数据结果（聊天式展示）
    public ObservableCollection<TcpDataResultItem> DataResults { get; } = new();

    private readonly TcpServerService _serverService;

    public TcpServerViewModel()
    {
        _serverService = new TcpServerService();
        // 订阅服务事件
        _serverService.ServerStatusChanged += OnServerStatusChanged;
        _serverService.ClientConnected += OnClientConnected;
        _serverService.ClientDisconnected += OnClientDisconnected;
        _serverService.DataReceived += OnServerDataReceived;

        SendTargets.Add(BroadcastLabel);
        SelectedClient = BroadcastLabel;
    }

    // 启动 / 停止服务
    [RelayCommand]
    private async Task ToggleServerAsync()
    {
        if (IsRunning)
        {
            _serverService.Stop();
            return;
        }

        if (Port <= 0 || Port > 65535)
        {
            StatusMessage = "端口号无效，请输入 1 - 65535 之间的数值";
            return;
        }

        ServerStatus = "正在启动...";
        StatusMessage = "正在启动服务...";
        var success = await _serverService.StartAsync(Port);
        if (!success)
        {
            ServerStatus = "启动失败";
            StatusMessage = "启动失败，端口可能已被占用";
        }
    }

    // 发送数据
    [RelayCommand]
    private async Task SendDataAsync()
    {
        if (string.IsNullOrWhiteSpace(SendMessage))
        {
            StatusMessage = "请输入要发送的数据";
            return;
        }

        if (!IsRunning)
        {
            StatusMessage = "服务未启动，无法发送数据";
            return;
        }

        byte[] data;
        try
        {
            data = SendType ? TcpService.HexStringToByteArray(SendMessage) : Encoding.UTF8.GetBytes(SendMessage);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hex格式错误: {ex.Message}";
            return;
        }

        string displayContent = SendType ? BitConverter.ToString(data).Replace("-", " ") : Encoding.UTF8.GetString(data);
        bool isBroadcast = SelectedClient == BroadcastLabel;

        string result;
        if (isBroadcast)
        {
            result = await _serverService.BroadcastAsync(data);
        }
        else
        {
            result = await _serverService.SendToClientAsync(SelectedClient, data);
        }

        DataResults.Add(new TcpDataResultItem
        {
            Content = isBroadcast ? $"[广播] {displayContent}" : $"[{SelectedClient}] {displayContent}",
            Author = "Me",
            Time = DateTime.Now.ToString("HH:mm:ss")
        });

        StatusMessage = result;
        SomeMethodThatRequiresScrolling();
    }

    private void OnServerStatusChanged(object sender, bool isRunning)
    {
        IsRunning = isRunning;
        ServerStatus = isRunning ? "已启动" : "已停止";
        StartBtnText = isRunning ? "停止服务" : "启动服务";
        if (!isRunning)
        {
            StatusMessage = "服务已停止";
        }
    }

    private void OnClientConnected(object sender, ServerClientEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ClientList.Add(e.EndPoint);
            if (!SendTargets.Contains(e.EndPoint))
            {
                SendTargets.Add(e.EndPoint);
            }
            ClientCount = ClientList.Count;
            AddLogItem(e.EndPoint, "[客户端已连接]");
            StatusMessage = $"客户端已连接: {e.EndPoint}";
        });
    }

    private void OnClientDisconnected(object sender, ServerClientEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ClientList.Remove(e.EndPoint);
            SendTargets.Remove(e.EndPoint);
            if (SelectedClient == e.EndPoint)
            {
                SelectedClient = BroadcastLabel;
            }
            ClientCount = ClientList.Count;
            AddLogItem(e.EndPoint, "[客户端已断开]");
            StatusMessage = $"客户端已断开: {e.EndPoint}";
        });
    }

    private void OnServerDataReceived(object sender, ServerReceiveEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            string content = RecvType
                ? BitConverter.ToString(e.Data).Replace("-", " ")
                : Encoding.UTF8.GetString(e.Data);

            DataResults.Add(new TcpDataResultItem
            {
                Content = content,
                Author = e.EndPoint,
                Time = DateTime.Now.ToString("HH:mm:ss")
            });

            StatusMessage = $"收到来自 {e.EndPoint} 的数据";
            SomeMethodThatRequiresScrolling();
        });
    }

    private void AddLogItem(string endpoint, string content)
    {
        DataResults.Add(new TcpDataResultItem
        {
            Content = content,
            Author = endpoint,
            Time = DateTime.Now.ToString("HH:mm:ss")
        });
        SomeMethodThatRequiresScrolling();
    }

    private void SomeMethodThatRequiresScrolling()
    {
        MessageBus.Current.SendMessage(new ScrollToEndMessage());
    }

    public void Dispose()
    {
        _serverService?.Dispose();
    }
}