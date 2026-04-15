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
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; 主程序文件（单文件发布，包含所有依赖）
Source: "publish_temp\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; 自建目录
[Dirs]
Name: "{app}\Games\StarCoreDefense\Assets\Sfx"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
