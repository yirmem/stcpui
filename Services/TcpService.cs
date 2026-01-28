using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace stcpui.Services;

public class TcpService
{
    private TcpClient _tcpClient;
    private NetworkStream _networkStream;
    private StreamReader _reader;
    private StreamWriter _writer;

    // 定义事件，用于通知连接状态变化和收到数据
    public event EventHandler<bool> ConnectionStatusChanged;
    public event EventHandler<byte[]> DataReceived;

    public bool IsConnected => _tcpClient?.Client != null && _tcpClient.Connected;

    public async Task<bool> ConnectAsync(string ipAddress, int port)
    {
        try
        {
            // 清理可能的旧连接
            Disconnect();

            _tcpClient = new TcpClient();
            // 设置连接超时，例如5秒
            var connectTask = _tcpClient.ConnectAsync(ipAddress, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var finishedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (finishedTask == timeoutTask)
            {
                Disconnect();
                OnConnectionStatusChanged(false);
                return false; // 连接超时
            }

            await connectTask; // 确保连接任务完成（或可能抛出的异常被捕获）

            if (_tcpClient.Connected)
            {
                _networkStream = _tcpClient.GetStream();
                _reader = new StreamReader(_networkStream);
                _writer = new StreamWriter(_networkStream) { AutoFlush = true };

                // 启动后台任务监听来自服务器的数据
                _ = Task.Run(ReceiveDataAsync);
                
                OnConnectionStatusChanged(true);
                return true;
            }
            else
            {
                OnConnectionStatusChanged(false);
                return false;
            }
        }
        catch (Exception ex)
        {
            // 可以记录日志 ex.Message
            Disconnect();
            OnConnectionStatusChanged(false);
            return false;
        }
    }

    public void Disconnect()
    {
        _reader?.Close();
        _writer?.Close();
        _networkStream?.Close();
        _tcpClient?.Close();

        _reader = null;
        _writer = null;
        _networkStream = null;
        _tcpClient = null;

        OnConnectionStatusChanged(false);
    }

    public async Task<string> SendDataAsync(string data,bool isHex = false)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("TCP连接未就绪，无法发送数据。");
        }

        try
        {
            byte[] sendData;
            if (isHex)
            {
                // 如果是16进制字符串，则进行转换
                sendData = HexStringToByteArray(data);
            }
            else
            {
                // 否则按普通文本处理（使用UTF8编码）
                sendData = Encoding.UTF8.GetBytes(data);
            }

            // 直接使用NetworkStream发送字节数组[1,7](@ref)
            await _networkStream.WriteAsync(sendData, 0, sendData.Length);
            return "发送成功";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送数据时发生错误: {ex.ToString()}");
            Disconnect();
            throw new InvalidOperationException($"发送数据时发生错误: {ex.Message}", ex);
        }
    }

    private async Task ReceiveDataAsync()
    {
        byte[] buffer = new byte[1024];
        try
        {
            while (IsConnected && _networkStream.CanRead)
            {
                // 这是一个简单的读取示例，Modbus协议可能需要更复杂的解析
                var bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    // 连接已由远程主机关闭
                    Disconnect();
                    break;
                }
                byte[] receivedData = new byte[bytesRead];
                Array.Copy(buffer, 0, receivedData, 0, bytesRead);
                OnDataReceived(receivedData);
            }
        }
        catch (IOException)
        {
            // 流读取错误，通常是连接断开
            Disconnect();
        }
        catch (ObjectDisposedException)
        {
            // 对象已被释放，正常断开
        }
        catch (Exception ex)
        {
            // 处理其他异常
            System.Diagnostics.Debug.WriteLine($"接收数据时出错: {ex.Message}");
        }
    }

    protected virtual void OnConnectionStatusChanged(bool isConnected)
    {
        ConnectionStatusChanged?.Invoke(this, isConnected);
    }

    protected virtual void OnDataReceived(byte[] data)
    {
        DataReceived?.Invoke(this, data);
    }

    public void Dispose()
    {
        Disconnect();
    }
    
    public static byte[] HexStringToByteArray(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return new byte[0];
        
        // 移除空格，并确保长度为偶数[4,5](@ref)
        hexString = hexString.Replace(" ", "");
        if (hexString.Length % 2 != 0)
            throw new ArgumentException("16进制字符串长度无效。");
    
        byte[] buffer = new byte[hexString.Length / 2];
        for (int i = 0; i < hexString.Length; i += 2)
        {
            buffer[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }
        return buffer;
    }
    
}