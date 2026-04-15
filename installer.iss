; Inno Setup Script for Pure Battle Hub
; Download Inno Setup: https://jrsoftware.org/isdl.php

#define MyAppName "Pure Battle Hub"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Pure Battle Hub Team"
#define MyAppURL "https://github.com/3361409208a-source/PureBattleGame"
#define MyAppExeName "PureBattleGame.exe"

[Setup]
AppId={{PUREBATTLEHUB-1234-5678-ABCD-EF1234567890}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=PureBattleHub-{#MyAppVersion}-Setup
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; 主程序文件
Source: "publish_temp\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_temp\{#MyAppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_temp\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_temp\*.json"; DestDir: "{app}"; Flags: ignoreversion

; WebView2 运行时
Source: "publish_temp\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_temp\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_temp\Microsoft.Web.WebView2.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_temp\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs

; 游戏资源文件
Source: "publish_temp\Games\StarCoreDefense\*.mp3"; DestDir: "{app}\Games\StarCoreDefense"; Flags: ignoreversion

; 自建目录
[Dirs]
Name: "{app}\Games\StarCoreDefense\Assets\Sfx"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Messages]
chinesesimplified.WelcomeLabel2=这将安装 [name] 到您的电脑。%n%n[name] 是一款模块化桌面娱乐工具合集，包含星核防线塔防游戏和像素电子宠桌面宠物。%n%n点击“下一步”继续安装。

[CustomMessages]
chinesesimplified.LaunchProgram=启动 Pure Battle Hub
