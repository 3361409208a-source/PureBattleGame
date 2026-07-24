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
echo   打包成功！自动同步至根目录 PureBattleGame.exe
echo ========================================
echo.
taskkill /F /IM PureBattleGame.exe 2>nul
timeout /t 1 /nobreak >nul
copy /Y "publish\PureBattleGame.exe" "PureBattleGame.exe"
echo.
echo.
echo 正在使用 Inno Setup 编译最新安装包...
if exist "C:\Users\Administrator\AppData\Local\Programs\Inno Setup 6\ISCC.exe" (
    "C:\Users\Administrator\AppData\Local\Programs\Inno Setup 6\ISCC.exe" /O"E:\PureBattleGame" setup_builder.iss
    echo 安装包已重新生成: E:\PureBattleGame\PureBattleGame_Setup_v2.0.exe
)
echo.
echo 产物位置:
echo 1. 独立程序: publish\PureBattleGame.exe
echo 2. 根目录程序: PureBattleGame.exe
echo 3. 安装包: PureBattleGame_Setup_v2.0.exe
echo.
pause
