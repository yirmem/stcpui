#!/bin/bash

# Avalonia macOS 应用打包脚本
# 使用方法：在项目根目录运行 ./package_mac_app.sh

# 配置变量
APP_NAME="Stcp"                    # 应用名称（.app文件名）
EXECUTABLE_NAME="stcpui"             # 可执行文件名称（与项目输出一致）
BUNDLE_ID="com.bin.stcpui"  # 应用标识符
VERSION="1.0.0"                      # 应用版本
PUBLISH_DIR="bin/Release/net9.0/osx-arm64/publish"  # 发布目录
APP_DIR="${APP_NAME}.app"            # 输出的 .app 目录
ICON_SOURCE="Assets/AppIcon.icns"    # 图标源文件

echo "开始打包 macOS 应用程序..."

# 检查发布目录是否存在
if [ ! -d "$PUBLISH_DIR" ]; then
    echo "错误: 发布目录不存在: $PUBLISH_DIR"
    echo "请先运行: dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true"
    exit 1
fi

# 检查可执行文件是否存在
if [ ! -f "$PUBLISH_DIR/$EXECUTABLE_NAME" ]; then
    echo "错误: 可执行文件不存在: $PUBLISH_DIR/$EXECUTABLE_NAME"
    echo "请检查 EXECUTABLE_NAME 配置是否正确"
    exit 1
fi

# 删除已存在的应用包
if [ -d "$APP_DIR" ]; then
    echo "删除已存在的应用包..."
    rm -rf "$APP_DIR"
fi

# 创建应用程序包目录结构 [6,8](@ref)
echo "创建应用程序包结构..."
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# 复制所有发布文件到 MacOS 目录
echo "复制文件到应用程序包..."
cp -R "$PUBLISH_DIR"/* "$APP_DIR/Contents/MacOS/"

# 设置可执行文件权限
chmod +x "$APP_DIR/Contents/MacOS/$EXECUTABLE_NAME"

# 复制应用程序图标 [6](@ref)
if [ -f "$ICON_SOURCE" ]; then
    echo "复制应用程序图标..."
    cp "$ICON_SOURCE" "$APP_DIR/Contents/Resources/AppIcon.icns"
else
    echo "警告: 图标文件不存在: $ICON_SOURCE，将使用默认图标"
    # 创建简单的默认图标占位符
    touch "$APP_DIR/Contents/Resources/AppIcon.icns"
fi

# 创建 Info.plist 文件 [6,8](@ref)
echo "创建应用程序配置文件..."
cat > "$APP_DIR/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>$EXECUTABLE_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_ID</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.13</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2025 Your Company. All rights reserved.</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon.icns</string>
</dict>
</plist>
EOF

echo "应用程序包创建完成: $APP_DIR"

# 验证应用程序结构
echo "验证应用程序结构..."
echo "应用包内容:"
find "$APP_DIR" -type f | sed 's|^|    |'

# 具体验证项
if [ -f "$APP_DIR/Contents/MacOS/$EXECUTABLE_NAME" ]; then
    echo "✅ 可执行文件位置正确"
    # 检查文件类型
    file "$APP_DIR/Contents/MacOS/$EXECUTABLE_NAME" | head -1
else
    echo "❌ 可执行文件未找到: $APP_DIR/Contents/MacOS/$EXECUTABLE_NAME"
fi

if [ -f "$APP_DIR/Contents/Info.plist" ]; then
    echo "✅ Info.plist 文件已创建"
else
    echo "❌ Info.plist 文件创建失败"
fi

if [ -f "$APP_DIR/Contents/Resources/AppIcon.icns" ]; then
    echo "✅ 图标文件已配置"
else
    echo "⚠️ 图标文件未配置"
fi

# 检查依赖库
DYLIBS_COUNT=$(find "$APP_DIR/Contents/MacOS" -name "*.dylib" | wc -l)
echo "发现 $DYLIBS_COUNT 个动态库文件"

# 验证应用程序包完整性
#echo "应用程序包完整性检查:"
#if [ -f "$APP_DIR/Contents/MacOS/$EXECUTABLE_NAME" ] && [ -f "$APP_DIR/Contents/Info.plist" ]; then
#    echo "✅ 应用程序包结构完整"
#    
#    # 检查是否为 ARM64 架构 [1,3](@ref)
#    if command -v lipo >/dev/null 2>&1; then
#        ARCHS=$(lipo -archs "$APP_DIR/Contents/MacOS/$EXECUTABLE_NAME" 2>/dev/null || echo "未知")
#        echo "✅ 可执行文件架构: $ARCHS"
#    else
#        ARCHS=$(file "$APP_DIR/Contents/MacOS/$EXECUTABLE_NAME" 2>/dev/null | grep -o "arm64\|x86_64" || echo "未知")
#        echo "📋 可执行文件信息: $ARCHS"
#    fi
#else
#    echo "❌ 应用程序包结构不完整"
#    exit 1
#fi

echo ""
echo "打包完成！应用程序包: $APP_DIR"
#echo "您可以:"
#echo "1. 直接双击 $APP_NAME.app 运行测试"
#echo "2. 将 $APP_NAME.app 拖拽到 Applications 文件夹中进行安装"
#echo "3. 修改 APP_NAME 变量可改变应用包名称（当前: $APP_NAME），而不影响可执行文件名称（当前: $EXECUTABLE_NAME）"