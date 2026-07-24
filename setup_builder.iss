[Setup]
AppId={{33614092-08A0-4C1F-9A61-PUREBATTLEGAM}}
AppName=Pure Battle Hub · 纯战枢纽 (Cyber-Quantum Hub)
AppVersion=v2.3 Cyber Edition
AppPublisher=Pure Battle Matrix Core
DefaultDirName={userpf}\PureBattleGame
DefaultGroupName=PureBattleGame (星核矩阵)
OutputDir=E:\PureBattleGame
OutputBaseFilename=PureBattleGame_Setup_v2.3
Compression=lzma2/fast
SolidCompression=no
WizardStyle=modern
WizardImageFile=E:\PureBattleGame\wizard_large.bmp
WizardSmallImageFile=E:\PureBattleGame\wizard_small.bmp
UninstallDisplayIcon={app}\PureBattleGame.exe
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

[Languages]
Name: "default"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=欢迎使用 Pure Battle Hub 星核矩阵部署终端
WelcomeLabel2=本向导将指导您完成 [name] 桌面像素AI宠物与星核防线战术系统的安装。%n%n正在建立量子粒子通道，初始化 60 FPS 像素图形引擎与 SiliconFlow AI 节点...%n%n请点击 [下一步] 开启科幻桌面新纪元。
WizardSelectDir=选择星核战术系统部署区块
SelectDirDesc=请指定 [name] 的硬盘数据挂载目录：
SelectDirLabel3=向导会将星核引擎与 WebUI 智能控制中枢部署在以下路径：
WizardInstalling=正在传输星核量子节点...
InstallingLabel=请稍候，系统正在解压 60 FPS DIB 像素引擎、安装 WebView2 交互大屏并初始化 AI 技能池...
FinishedHeadingLabel=🌌 星核矩阵部署成功！
FinishedLabel=恭喜！[name] 已完美安装在您的系统区块中。%n%n您可以通过快捷键 Ctrl+Shift+L 随时唤起 60 FPS 实时战斗日志与 MVP 战况大屏。
ClickFinish=点击 [完成] 开启 Pure Battle Hub 科幻摸鱼之旅。

[Tasks]
Name: "desktopicon"; Description: "🚀 创建桌面量子跃迁快捷入口"; GroupDescription: "系统关联设置:"; Flags: unchecked
Name: "autostarticon"; Description: "⚡ 开机自动唤醒星核守护网络 (可选)"; GroupDescription: "自动化部署:"; Flags: unchecked

[Files]
Source: "E:\PureBattleGame\publish\PureBattleGame.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "E:\PureBattleGame\publish\WebUI\dist\*"; DestDir: "{app}\WebUI\dist"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Pure Battle Hub (星核矩阵终端)"; Filename: "{app}\PureBattleGame.exe"
Name: "{autodesktop}\Pure Battle Hub"; Filename: "{app}\PureBattleGame.exe"; Tasks: desktopicon
Name: "{userstartup}\PureBattleGame"; Filename: "{app}\PureBattleGame.exe"; Tasks: autostarticon
Name: "{group}\卸载 Pure Battle Hub"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\PureBattleGame.exe"; Description: "⚡ 立即启动 Pure Battle Hub (开启桌面像素与AI世界)"; Flags: postinstall nowait skipifsilent
