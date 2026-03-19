@echo off
echo ========================================
echo   Pure Battle Game - 构建脚本
echo ========================================
echo.

REM 检查 .NET SDK 是否安装
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo 错误: 未找到 .NET SDK
    echo 请先安装 .NET 8.0 SDK 或更高版本
    echo 下载地址: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo 清理旧构建文件...
dotnet clean --configuration Release
if errorlevel 1 (
    echo 清理失败
    pause
    exit /b 1
)

echo.
echo 开始构建...
dotnet build --configuration Release
if errorlevel 1 (
    echo.
    echo 构建失败!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   构建成功!
echo ========================================
echo.
echo 可执行文件位置:
echo   PureBattleGame\bin\Release\net8.0-windows\PureBattleGame.exe
echo.
echo 直接运行:
echo   dotnet run
echo.
pause