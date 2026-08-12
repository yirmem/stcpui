using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace stcpui.Services;

public class ServerReceiveEventArgs : EventArgs
{
    public TcpClient Client { get; set; } = null!;
    public string EndPoint { get; set; } = "";
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

public class ServerClientEventArgs : EventArgs
{
    public TcpClient Client { get; set; } = null!;
    public string EndPoint { get; set; } = "";
}

public class TcpServerService : IDisposable
{
    private TcpListener? _listener;
    private readonly Dictionary<TcpClient, string> _clients = new();
    private readonly object _lock = new();
    private volatile bool _accepting;

    // 定义事件，用于通知服务状态变化、客户端连接/断开以及数据接收
    public event EventHandler<bool> ServerStatusChanged;
    public event EventHandler<ServerClientEventArgs> ClientConnected;
    public event EventHandler<ServerClientEventArgs> ClientDisconnected;
    public event EventHandler<ServerReceiveEventArgs> DataReceived;

    public bool IsRunning => _accepting;

    public int ClientCount
    {
        get
        {
            lock (_lock)
            {
                return _clients.Count;
            }
        }
    }

    public async Task<bool> StartAsync(int port)
    {
        try
        {
            Stop();

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _accepting = true;

            OnServerStatusChanged(true);
            _ = Task.Run(AcceptClientsAsync);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动TCP服务失败: {ex.Message}");
            _accepting = false;
            OnServerStatusChanged(false);
            return false;
        }
    }

    private async Task AcceptClientsAsync()
    {
        try
        {
            while (_accepting)
            {
                var client = await _listener!.AcceptTcpClientAsync();
                string endpoint = client.Client.RemoteEndPoint?.ToString() ?? "未知客户端";

                lock (_lock)
                {
                    _clients[client] = endpoint;
                }

                OnClientConnected(new ServerClientEventArgs { Client = client, EndPoint = endpoint });
                _ = Task.Run(() => ReceiveFromClientAsync(client, endpoint));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"接受客户端异常: {ex.Message}");
        }
    }

    private async Task ReceiveFromClientAsync(TcpClient client, string endpoint)
    {
        try
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[4096];
            while (client.Connected && _accepting)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    // 连接已由远程主机关闭
                    break;
                }

                byte[] receivedData = new byte[bytesRead];
                Array.Copy(buffer, receivedData, bytesRead);
                OnDataReceived(new ServerReceiveEventArgs
                {
                    Client = client,
                    EndPoint = endpoint,
                    Data = receivedData
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"接收客户端 {endpoint} 数据异常: {ex.Message}");
        }
        finally
        {
            RemoveClient(client, endpoint);
        }
    }

    private void RemoveClient(TcpClient client, string endpoint)
    {
        lock (_lock)
        {
            _clients.Remove(client);
        }

        try
        {
            client?.Close();
        }
        catch
        {
        }

        OnClientDisconnected(new ServerClientEventArgs { Client = client, EndPoint = endpoint });
    }

    public void Stop()
    {
        _accepting = false;

        try
        {
            _listener?.Stop();
        }
        catch
        {
        }
        _listener = null;

        List<TcpClient> snapshot;
        lock (_lock)
        {
            snapshot = new List<TcpClient>(_clients.Keys);
        }

        foreach (var client in snapshot)
        {
            try
            {
                client?.Close();
            }
            catch
            {
            }
        }

        lock (_lock)
        {
            _clients.Clear();
        }

        OnServerStatusChanged(false);
    }

    public async Task<string> BroadcastAsync(byte[] data)
    {
        List<TcpClient> snapshot;
        lock (_lock)
        {
            snapshot = new List<TcpClient>(_clients.Keys);
        }

        if (snapshot.Count == 0)
        {
            return "当前没有已连接的客户端";
        }

        foreach (var client in snapshot)
        {
            var result = await SendDataInternalAsync(client, data);
            if (result != "发送成功")
            {
                return result;
            }
        }

        return $"已广播给 {snapshot.Count} 个客户端";
    }

    public async Task<string> SendToClientAsync(string endpoint, byte[] data)
    {
        TcpClient? client = null;
        lock (_lock)
        {
            foreach (var kv in _clients)
            {
                if (kv.Value == endpoint)
                {
                    client = kv.Key;
                    break;
                }
            }
        }

        if (client == null)
        {
            return "目标客户端不存在或已断开连接";
        }

        return await SendDataInternalAsync(client, data);
    }

    private async Task<string> SendDataInternalAsync(TcpClient client, byte[] data)
    {
        try
        {
            if (client == null || !client.Connected || client.GetStream() == null)
            {
                return "客户端连接已断开";
            }

            var stream = client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);
            return "发送成功";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送数据时发生错误: {ex.ToString()}");
            return $"发送失败: {ex.Message}";
        }
    }

    protected virtual void OnServerStatusChanged(bool isRunning)
    {
        ServerStatusChanged?.Invoke(this, isRunning);
    }

    protected virtual void OnClientConnected(ServerClientEventArgs e)
    {
        ClientConnected?.Invoke(this, e);
    }

    protected virtual void OnClientDisconnected(ServerClientEventArgs e)
    {
        ClientDisconnected?.Invoke(this, e);
    }

    protected virtual void OnDataReceived(ServerReceiveEventArgs e)
    {
        DataReceived?.Invoke(this, e);
    }

    public void Dispose()
    {
        Stop();
    }
}