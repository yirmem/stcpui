using CommunityToolkit.Mvvm.ComponentModel;

namespace stcpui.Models;

public partial class Pm2App : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _cpu = "0%";
    [ObservableProperty] private string _memory = "0MB";
    [ObservableProperty] private string _status = "Stopped";
    [ObservableProperty] private string _upTime = "";
}