### Windows 打包
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

### MacOs 打包
    dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
    ./package_mac_app.sh

### Modbus 客户端
    1. 每次连接临时存储连接方案

### Tcp客户端
    1. 每次连接临时存储连接方案

### 可视化 Pm2
    1. 可自定义pm2工作目录
    2. 日志查看、项目启动关闭重启删除
    3. 每项CPU、内存资源占用查看

#### TODO计划
    1. 一键安装 node 及 pm2及 pm2-windows-startup

### 运维工具
#### Windows 系统工具
    1. 开关防火墙
    2. 一键修改系统密码为 123456且开启开机自动登录系统，启用无密码登录
    3. 可修改注册表
    4. 常用下载



