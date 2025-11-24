using System;

namespace stcpui.Models;

public class ModbusModel
{
    public long Id { get; set; }
    
    public string Ip { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 503;

    public int? DeviceAddress { get; set; } = 1;
    public string StartAddress { get; set; } = "0000";
    public ushort ReadLength { get; set; } = 10;
    
    public DateTime CreateTime { get; set; }
}