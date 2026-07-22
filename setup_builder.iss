[Setup]
AppId={{33614092-08A0-4C1F-9A61-PUREBATTLEGAM}}
AppName=PureBattleGame (像素机器人桌宠 & 星核防线)
AppVersion=2.0
AppPublisher=PureBattleGame
DefaultDirName={userpf}\PureBattleGame
DefaultGroupName=PureBattleGame
OutputDir=E:\PureBattleGame\installer_output
OutputBaseFilename=PureBattleGame_Setup_v2.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\PureBattleGame.exe
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

[Languages]
Name: "default"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式设置:"; Flags: unchecked
Name: "autostarticon"; Description: "开机自动启动 (可选)"; GroupDescription: "开机设置:"; Flags: unchecked

[Files]
Source: "E:\PureBattleGame\publish\PureBattleGame.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\PureBattleGame (像素机器人)"; Filename: "{app}\PureBattleGame.exe"
Name: "{group}\卸载 PureBattleGame"; Filename: "{uninstallexe}"
Name: "{autodesktop}\PureBattleGame (像素机器人)"; Filename: "{app}\PureBattleGame.exe"; Tasks: desktopicon
Name: "{userstartup}\PureBattleGame"; Filename: "{app}\PureBattleGame.exe"; Tasks: autostarticon

[Run]
Filename: "{app}\PureBattleGame.exe"; Description: "运行 PureBattleGame 像素机器人"; Flags: nowait postinstall skipifsilent
