using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace stcpui.Models;

[Table("TcpClient")]
public class TcpClientModel
{
    public long Id { get; set; }
    
    public string Ip { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 8880;

    public int SendType { get; set; } = 1; //发送消息类型 0 false 字符串 ｜ 1 true hex
    public int RecvType { get; set; } = 1; //接收消息类型 0 false 字符串 ｜ 1 true hex
    
    public DateTime CreateTime { get; set; }
}