@echo off
chcp 65001 >nul
echo ========================================
echo   Pure Battle Game - 发布打包脚本
echo   打包为独立 EXE (无需目标电脑安装 .NET)
echo ========================================
echo.

set "DOTNET_CMD=dotnet"
where dotnet >nul 2>&1
if errorlevel 1 (
    if exist "E:\dotnet\dotnet.exe" (
        set "DOTNET_CMD=E:\dotnet\dotnet.exe"
    ) else (
        echo 错误: 未找到 .NET SDK
        echo 请先安装 .NET 8.0 SDK
        pause
        exit /b 1
    )
)

echo 使用 .NET SDK: %DOTNET_CMD%
%DOTNET_CMD% --version
echo.

echo 开始打包 (独立单文件，目标电脑无需安装 .NET)...
echo 这可能需要半分钟，请稍候...
echo.

%DOTNET_CMD% publish PureBattleGame.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    --output ./publish

if errorlevel 1 (
    echo.
    echo 打包失败！
    pause
    exit /b 1
)

echo.
echo ========================================
echo   打包成功！
echo ========================================
echo.
echo 独立 EXE 位置:
echo   E:\PureBattleGame\publish\PureBattleGame.exe
echo.
echo 此文件可直接复制到任意 64位 Windows 电脑直接双击运行！
echo.
explorer .\publish
pause
