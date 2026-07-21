@echo off
chcp 65001 >nul
echo ========================================
echo   Pure Battle Game - 构建脚本
echo ========================================
echo.

set "DOTNET_CMD=dotnet"
where dotnet >nul 2>&1
if errorlevel 1 (
    if exist "E:\dotnet\dotnet.exe" (
        set "DOTNET_CMD=E:\dotnet\dotnet.exe"
    ) else (
        echo 错误: 未找到 .NET SDK
        echo 请先安装 .NET 8.0 SDK 或运行环境配置
        pause
        exit /b 1
    )
)

echo 使用 .NET SDK: %DOTNET_CMD%
%DOTNET_CMD% --version
echo.

echo 清理旧构建文件...
%DOTNET_CMD% clean --configuration Release
if errorlevel 1 (
    echo 清理失败
    pause
    exit /b 1
)

echo.
echo 开始构建...
%DOTNET_CMD% build --configuration Release
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
pause