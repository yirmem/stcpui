### Windows 打包
    dotnet publish -c Release -r win-x64 --self-contained

### MacOs 打包
    dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
    ./package_mac_app.sh

### Modbus 调试
#### TODO计划
    1. 每次连接临时存储连接方案

--------

##  TODO计划
### Tcp调试工具
TODO

### 运维工具
#### Windows 系统工具
    1. 开关防火墙
    2. 一键修改系统密码为 123456且开启开机自动登录系统，启用无密码登录
    3. 可修改注册表

#### 可视化 Pm2
    1. 进程列表及应用开关按钮
    2. 添加程序使用 Pm2 管控，以及开机启动
    3. 一键安装 node 及 pm2及 pm2-windows-startup


