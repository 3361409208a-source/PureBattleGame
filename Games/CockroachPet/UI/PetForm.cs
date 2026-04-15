using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PureBattleGame.Core;

namespace PureBattleGame.Games.CockroachPet;

public partial class PetForm : Form
{
    public static PetForm? Instance { get; private set; }

    // Windows API for global hotkeys
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID_MENU = 1;
    private const int HOTKEY_ID_TOGGLE = 2;
    private const int HOTKEY_ID_PAUSE = 3;
    private const int HOTKEY_ID_BOSS = 4; // 摸鱼模式热键ID
    private const int HOTKEY_ID_BOSS_CYCLE = 5; // 切换摸鱼主题
    private const int HOTKEY_ID_SPAWN_MONSTER = 6; // 投放怪物热键ID
    private const int HOTKEY_ID_OPACITY_UP = 7; // 增加透明度
    private const int HOTKEY_ID_OPACITY_DOWN = 8; // 减少透明度
    private const uint MOD_CTRL_SHIFT = 0x0002 | 0x0004; // MOD_CONTROL | MOD_SHIFT

    // 机器人列表
    private List<Robot> _robots = new List<Robot>();

    // 怪物列表
    private List<Monster> _monsters = new List<Monster>();

    // 定时器
    private System.Windows.Forms.Timer? _moveTimer;

    // 全局速度
    private int _globalSpeed = 100;


    // 机器人ID计数器
    private int _robotIdCounter = 1;

    // 通知图标
    private NotifyIcon? _notifyIcon;

    // 点击穿透
    private bool _clickThrough = true;

    // 摸鱼模式
    private bool _bossMode = false;
    private bool _bossModeHideRobots = true; // 摸鱼模式是否真正隐藏机器人（而非伪装界面）
    private BossModeTheme _bossModeTheme = BossModeTheme.Excel;
    private readonly string[] _themeNames = { "无（仅隐藏）", "Excel表格", "VS Code编辑器", "CMD终端", "Word文档" };

    // 默认设置
    public int DefaultRobotSize { get; set; } = 64;
    public string DefaultRobotName { get; set; } = "小八";
    public int DefaultRobotCount { get; set; } = 1;
    public bool ShowNamingDialog { get; set; } = false; // 默认不显示命名对话框
    public bool EnableAiThinking { get; set; } = false;
    public int AiThoughtFrequency { get; set; } = 60;
    public int FightFrequency { get; set; } = 15;
    public bool IsWeaponMaster { get; set; } = false;
    public RobotPersonalityType DefaultPersonality { get; set; } = RobotPersonalityType.Friendly;

    private List<Projectile> _projectiles = new List<Projectile>();


    // 设置窗口单例
    private SettingsForm? _settingsForm = null;

    public bool IsGameEnding => _isGameEnding;
    private bool _isGameEnding = false; // 标记是否正在执行胜者吞噬逻辑
    private Robot? _winner = null;
    private int _resetTimer = 0;

    // 控制面板单例
    private ControlPanelForm? _controlPanel = null;

    public PetForm()
    {
        Instance = this;
        InitializeComponent();
        InitializeWindow();
        LoadSettingsAndStart();
    }

    private void LoadSettingsAndStart()
    {
        // 初始化音效系统
        AudioManager.Initialize();

        // 尝试从文件加载设置
        LoadSettingsFromFile();

        // 初始化托盘图标
        InitNotifyIcon();

        // 加载持久化机器人
        var savedRobots = PersistenceManager.LoadRobots();
        if (savedRobots.Count > 0)
        {
            int restoredCount = 0;
            foreach (var data in savedRobots)
            {
                var robot = RestoreRobot(data);
                if (restoredCount >= 5)
                {
                    robot.IsVisible = false;
                }
                
                if (data.Id >= _robotIdCounter) _robotIdCounter = data.Id + 1;
                restoredCount++;
            }
            string msg = $"成功找回 {savedRobots.Count} 个伙伴！";
            if (savedRobots.Count > 5) msg += " (超出5个已自动隐藏)";
            ShowNotification(msg);
        }
        else
        {
            // 默认投放
            if (ShowNamingDialog)
            {
                SpawnRobotsWithNaming(DefaultRobotCount);
            }
            else
            {
                for (int i = 0; i < DefaultRobotCount; i++)
                {
                    string name = DefaultRobotCount == 1
                        ? DefaultRobotName
                        : $"{DefaultRobotName}-{i + 1}";
                    var robot = SpawnRobot(name, -1, -1);
                    if (i >= 5)
                    {
                        robot.IsVisible = false;
                    }
                }
            }
        }
        
        // 自动打开所有终端
        AutoOpenAllTerminals();
    }

    private void AutoOpenAllTerminals()
    {
        if (_robots.Count > 0)
        {
            var manager = TerminalManagerForm.Instance;
            manager.Show();
            foreach (var robot in _robots)
            {
                manager.OpenTerminal(robot);
            }
        }
    }


    private Icon CreateRobotIcon()
    {
        // 创建像素八爪鱼图标
        var iconBitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(iconBitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            // 绘制简单的像素八爪鱼
            using var bodyBrush = new SolidBrush(Color.FromArgb(77, 171, 255)); // 蓝色
            using var tentacleBrush = new SolidBrush(Color.FromArgb(51, 153, 255));
            using var eyeBrush = new SolidBrush(Color.White);
            using var pupilBrush = new SolidBrush(Color.Black);

            // 身体 (圆形)
            g.FillEllipse(bodyBrush, 8, 6, 16, 14);

            // 触手 (8条)
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (float)(Math.PI / 4);
                int tx = 16 + (int)(Math.Cos(angle) * 10);
                int ty = 13 + (int)(Math.Sin(angle) * 8);
                g.FillRectangle(tentacleBrush, tx - 1, ty - 1, 3, 3);
            }

            // 眼睛
            g.FillEllipse(eyeBrush, 11, 8, 5, 5);
            g.FillEllipse(eyeBrush, 18, 8, 5, 5);
            g.FillRectangle(pupilBrush, 13, 9, 2, 2);
            g.FillRectangle(pupilBrush, 20, 9, 2, 2);
        }
        return Icon.FromHandle(iconBitmap.GetHicon());
    }

    private void LoadSettingsFromFile()
    {
        try
        {
            string settingsPath = Path.Combine(Path.GetTempPath(), "RobotPetSettings.txt");
            if (File.Exists(settingsPath))
            {
                var lines = File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        switch (parts[0])
                        {
                            case "Count": DefaultRobotCount = int.Parse(parts[1]); break;
                            case "ShowNaming": ShowNamingDialog = bool.Parse(parts[1]); break;
                            case "DefaultName": DefaultRobotName = parts[1]; break;
                            case "DefaultSize": DefaultRobotSize = int.Parse(parts[1]); break;
                            case "DefaultSpeed": _globalSpeed = int.Parse(parts[1]); break;
                            case "EnableAi": EnableAiThinking = bool.Parse(parts[1]); break;
                            case "AiFreq": AiThoughtFrequency = int.Parse(parts[1]); break;
                            case "FightFreq": FightFrequency = int.Parse(parts[1]); break;
                            case "WeaponMaster": IsWeaponMaster = bool.Parse(parts[1]); break;
                            case "Personality":
                                if (int.TryParse(parts[1], out int personalityIndex))
                                {
                                    DefaultPersonality = (RobotPersonalityType)Math.Clamp(personalityIndex, 0, 7);
                                }
                                break;

                        }
                    }
                }
            }
            // 文件不存在就用默认值
        }
        catch
        {
            // 出错用默认值
        }
    }

    private void InitializeWindow()
    {
        int screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        // 全屏无边框
        this.Text = "Pixel Robot Pet";
        this.StartPosition = FormStartPosition.Manual;
        this.Location = Point.Empty;
        this.Size = new Size(screenWidth, screenHeight);
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.ShowInTaskbar = false; // 主窗口不显示在任务栏

        // 透明设置
        this.BackColor = Color.Black;
        this.TransparencyKey = Color.Black;
        this.AllowTransparency = true;
        this.DoubleBuffered = true;

        // 启用点击穿透
        SetClickThrough(true);

        // 设置窗口图标
        this.Icon = CreateRobotIcon();

        // 定时器
        _moveTimer = new System.Windows.Forms.Timer();
        _moveTimer.Interval = 30;
        _moveTimer.Tick += MoveTimer_Tick;
        _moveTimer.Start();

        // 事件绑定
        this.Paint += PetForm_Paint;
        this.MouseClick += PetForm_MouseClick;
        this.KeyDown += PetForm_KeyDown;

        // 注册全局热键
        RegisterGlobalHotkeys();
    }



    private void RegisterGlobalHotkeys()
    {
        // 使用组合键避免干扰正常操作
        // Ctrl+Shift+P = 暂停/继续所有
        RegisterHotKey(this.Handle, HOTKEY_ID_PAUSE, MOD_CTRL_SHIFT, 0x50); // P
        // Ctrl+Shift+T = 切换点击穿透
        RegisterHotKey(this.Handle, HOTKEY_ID_TOGGLE, MOD_CTRL_SHIFT, 0x54); // T
        // Ctrl+Shift+M = 打开菜单
        RegisterHotKey(this.Handle, HOTKEY_ID_MENU, MOD_CTRL_SHIFT, 0x4D); // M
        // Ctrl+Shift+H = 摸鱼模式 (Hide)
        RegisterHotKey(this.Handle, HOTKEY_ID_BOSS, MOD_CTRL_SHIFT, 0x48); // H
        // Ctrl+Shift+B = 切换摸鱼主题 (Boss theme)
        RegisterHotKey(this.Handle, HOTKEY_ID_BOSS_CYCLE, MOD_CTRL_SHIFT, 0x42); // B
        // Ctrl+Shift+X = 投放怪物 (X for eXterminate)
        RegisterHotKey(this.Handle, HOTKEY_ID_SPAWN_MONSTER, MOD_CTRL_SHIFT, 0x58); // X
        // Ctrl+Shift+Up = 增加透明度
        RegisterHotKey(this.Handle, HOTKEY_ID_OPACITY_UP, MOD_CTRL_SHIFT, 0x26); // Up arrow
        // Ctrl+Shift+Down = 减少透明度
        RegisterHotKey(this.Handle, HOTKEY_ID_OPACITY_DOWN, MOD_CTRL_SHIFT, 0x28); // Down arrow
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_HOTKEY = 0x0312;
        if (m.Msg == WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            switch (id)
            {
                case HOTKEY_ID_MENU:
                    ShowTrayMenu();
                    break;
                case HOTKEY_ID_TOGGLE:
                    SetClickThrough(!_clickThrough);
                    ShowNotification(_clickThrough ? "点击穿透: 开启" : "点击穿透: 关闭");
                    break;
                case HOTKEY_ID_PAUSE:
                    TogglePauseAll();
                    break;
                case HOTKEY_ID_BOSS:
                    ToggleBossMode();
                    break;
                case HOTKEY_ID_BOSS_CYCLE:
                    CycleBossModeTheme();
                    break;
                case HOTKEY_ID_SPAWN_MONSTER:
                    SpawnMonster();
                    break;
                case HOTKEY_ID_OPACITY_UP:
                    ChangeOpacity(20); // 增加透明度（更不透明）
                    break;
                case HOTKEY_ID_OPACITY_DOWN:
                    ChangeOpacity(-20); // 减少透明度（更透明）
                    break;
            }
        }
        base.WndProc(ref m);
    }

    private void ShowTrayMenu()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.ContextMenuStrip = CreateContextMenu();
            var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            mi?.Invoke(_notifyIcon, null);
        }
    }

    private void TogglePauseAll()
    {
        bool anyMoving = _robots.Any(r => r.IsMoving);
        foreach (var r in _robots) r.IsMoving = !anyMoving;
        ShowNotification(anyMoving ? "全部暂停" : "全部继续");
    }

    private void ToggleBossMode()
    {
        _bossMode = !_bossMode;

        if (_bossModeHideRobots)
        {
            // 真正隐藏机器人
            foreach (var r in _robots)
            {
                r.IsVisible = !_bossMode;
            }
            // 隐藏怪物
            foreach (var m in _monsters)
            {
                m.IsActive = !_bossMode;
            }
            ShowNotification(_bossMode ? "摸鱼模式已开启: 所有宠物已隐藏 👻" : "欢迎回来 👋");
        }
        else
        {
            // 传统摸鱼模式（伪装界面）
            ShowNotification(_bossMode ? $"摸鱼模式已开启: {_themeNames[(int)_bossModeTheme]} 🐟" : "欢迎回来 👋");
        }

        Invalidate();
    }

    private void CycleBossModeTheme()
    {
        if (!_bossMode)
        {
            ToggleBossMode();
            return;
        }

        _bossModeTheme = (BossModeTheme)(((int)_bossModeTheme + 1) % 5);
        ShowNotification($"切换伪装: {_themeNames[(int)_bossModeTheme]} 🎭");
        Invalidate();
    }

    private void SpawnRobotsWithNaming(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var robot = SpawnRobotWithName();
            if (robot != null && _robots.IndexOf(robot) >= 5)
            {
                robot.IsVisible = false;
            }
        }
    }

    /// <summary>
    /// 投放怪物 - 所有机器人会集火攻击
    /// </summary>
    private void SpawnMonster()
    {
        // 清理已死亡的怪物
        _monsters.RemoveAll(m => m.IsDead);

        // 在屏幕中心附近随机位置生成怪物
        var screen = Screen.PrimaryScreen;
        if (screen == null) return;

        float x = screen.Bounds.Width / 2f + new Random().Next(-200, 200);
        float y = screen.Bounds.Height / 2f + new Random().Next(-150, 150);

        var monster = new Monster(x, y);
        _monsters.Add(monster);

        ShowNotification($"🐲 怪物已投放！HP: {monster.MaxHP}");

        // 让所有机器人进入战斗状态，集火攻击怪物
        foreach (var robot in _robots)
        {
            if (robot.IsActive && !robot.IsDead)
            {
                robot.SetMonsterTarget(monster);
            }
        }

        Invalidate();
    }

    /// <summary>
    /// 调整所有机器人的透明度
    /// </summary>
    private void ChangeOpacity(int delta)
    {
        if (_robots.Count == 0) return;

        int newOpacity = _robots[0].Opacity + delta;
        newOpacity = Math.Clamp(newOpacity, 0, 255);

        foreach (var robot in _robots)
        {
            robot.Opacity = newOpacity;
        }

        string opacityPercent = (newOpacity * 100 / 255).ToString();
        ShowNotification($"透明度: {opacityPercent}% {(newOpacity == 0 ? "(完全透明)" : newOpacity == 255 ? "(完全不透明)" : "")}");
        Invalidate();
    }

    private void ShowNotification(string message)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.BalloonTipTitle = "Pixel Robot";
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(2000);
        }
    }

    private void InitNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Pixel Robot Pet",
            Icon = CreateRobotIcon(),
            Visible = true
        };

        // 直接绑定右键菜单
        _notifyIcon.ContextMenuStrip = CreateContextMenu();

        // 左键点击也显示菜单
        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                // 刷新菜单内容
                _notifyIcon.ContextMenuStrip = CreateContextMenu();
                // 显示菜单
                var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mi?.Invoke(_notifyIcon, null);
            }
        };

        // 启动提示
        _notifyIcon.BalloonTipTitle = "Pixel Robot Pet";
        _notifyIcon.BalloonTipText = "系统已启动！\nCtrl+Shift+M 打开菜单\nCtrl+Shift+P 暂停/继续\nCtrl+Shift+H 摸鱼模式\nCtrl+Shift+B 切换伪装\nCtrl+Shift+X 投放怪物";
        _notifyIcon.ShowBalloonTip(3000);
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        // 机器人列表
        if (_robots.Count > 0)
        {
            menu.Items.Add("机器人列表:").Enabled = false;
            foreach (var robot in _robots)
            {
                var robotMenu = new ToolStripMenuItem($"{robot.Name} (#{robot.Id})");
                
                // 终端菜单
                robotMenu.DropDownItems.Add("📺 打开终端", null, (s, e) => robot.OpenTerminal());
                robotMenu.DropDownItems.Add("🗕 关闭终端", null, (s, e) => robot.CloseTerminal());
                
                robotMenu.DropDownItems.Add(new ToolStripSeparator());
                
                // 机器人控制
                var status = robot.IsMoving ? "⏸ 暂停移动" : "▶ 恢复移动";
                robotMenu.DropDownItems.Add(status, null, (s, e) => robot.IsMoving = !robot.IsMoving);
                
                menu.Items.Add(robotMenu);
            }
            menu.Items.Add(new ToolStripSeparator());
        }

        // 新增机器人
        menu.Items.Add("➕ 投放新机器人", null, (s, e) => SpawnRobotWithName());
        menu.Items.Add("⚡ 快速投放", null, (s, e) =>
        {
            string[] names = { "小八", "阿呆", "像素仔", "蓝灵", "红豆", "大眼", "触手大王", "碳基生物" };
            SpawnRobot(names[new Random().Next(names.Length)], -1, -1);
        });

        menu.Items.Add(new ToolStripSeparator());

        // 控制面板 & 社交中心
        menu.Items.Add("🎛️ 打开控制面板", null, (s, e) => ShowControlPanel());
        menu.Items.Add("💬 世界聊天频道", null, (s, e) => TerminalManagerForm.Instance.Show());

        menu.Items.Add(new ToolStripSeparator());

        // 摸鱼模式菜单
        var bossMenu = new ToolStripMenuItem("🐟 摸鱼模式");
        var bossToggleItem = new ToolStripMenuItem(_bossMode ? "✅ 已开启" : "⭕ 已关闭");
        bossToggleItem.Click += (s, e) => ToggleBossMode();
        bossMenu.DropDownItems.Add(bossToggleItem);
        bossMenu.DropDownItems.Add(new ToolStripSeparator());

        var themes = new[] { ("🚫 无（仅隐藏）", BossModeTheme.None), ("📊 Excel表格", BossModeTheme.Excel), ("💻 VS Code", BossModeTheme.CodeEditor), ("⌨️ CMD终端", BossModeTheme.Terminal), ("📝 Word文档", BossModeTheme.Word) };
        foreach (var (name, theme) in themes)
        {
            var themeItem = new ToolStripMenuItem(_bossModeTheme == theme ? $"✓ {name}" : name);
            themeItem.Click += (s, e) =>
            {
                _bossModeTheme = theme;
                if (!_bossMode) ToggleBossMode();
                else Invalidate();
                ShowNotification($"切换伪装: {_themeNames[(int)theme]}");
            };
            bossMenu.DropDownItems.Add(themeItem);
        }
        menu.Items.Add(bossMenu);

        menu.Items.Add(new ToolStripSeparator());

        // 全局控制
        var controlMenu = new ToolStripMenuItem("全局控制");
        controlMenu.DropDownItems.Add("全部暂停", null, (s, e) =>
        {
            foreach (var r in _robots) r.IsMoving = false;
        });
        controlMenu.DropDownItems.Add("全部启动", null, (s, e) =>
        {
            foreach (var r in _robots) r.IsMoving = true;
        });
        controlMenu.DropDownItems.Add(new ToolStripSeparator());
        controlMenu.DropDownItems.Add("全部清除", null, (s, e) =>
        {
            _robots.Clear();
        });
        menu.Items.Add(controlMenu);

        // 骂人模式开关
        var anyCurseMode = _robots.Count > 0 && _robots.Any(r => r.CurseMode);
        var allCurseMode = _robots.Count > 0 && _robots.All(r => r.CurseMode);
        var curseMenu = new ToolStripMenuItem(anyCurseMode ? (allCurseMode ? "🤬 骂人模式 (全员开启)" : "🤬 骂人模式 (部分开启)") : "🤐 骂人模式 (已关闭)");
        curseMenu.DropDownItems.Add("全员开启", null, (s, e) =>
        {
            foreach (var r in _robots) r.CurseMode = true;
            ShowNotification("骂人模式已全员开启！🤬");
        });
        curseMenu.DropDownItems.Add("全员关闭", null, (s, e) =>
        {
            foreach (var r in _robots) r.CurseMode = false;
            ShowNotification("骂人模式已关闭");
        });
        curseMenu.DropDownItems.Add(new ToolStripSeparator());
        curseMenu.DropDownItems.Add("随机切换", null, (s, e) =>
        {
            foreach (var r in _robots) r.CurseMode = new Random().Next(100) < 50;
            ShowNotification("骂人模式随机切换完成！");
        });
        menu.Items.Add(curseMenu);

        // 速度控制
        var speedMenu = new ToolStripMenuItem("全局速度");
        var slowItem = new ToolStripMenuItem("慢速 (50%)");
        slowItem.Click += (s, e) => SetGlobalSpeed(50);
        var normalItem = new ToolStripMenuItem("正常 (100%)");
        normalItem.Click += (s, e) => SetGlobalSpeed(100);
        var fastItem = new ToolStripMenuItem("快速 (200%)");
        fastItem.Click += (s, e) => SetGlobalSpeed(200);
        speedMenu.DropDownItems.AddRange(new[] { slowItem, normalItem, fastItem });
        menu.Items.Add(speedMenu);

        menu.Items.Add(new ToolStripSeparator());

        // 设置 - 单例模式
        menu.Items.Add("⚙️ 设置...", null, (s, e) => ShowSettings());

        // 快捷键提示
        menu.Items.Add("ℹ️ 快捷键", null, (s, e) => ShowShortcuts());



        // 关于
        menu.Items.Add("❓ 关于", null, (s, e) =>
        {
            MessageBox.Show(
                "Pixel Robot Pet\n\n" +
                "桌面八爪鱼机器人宠物\n" +
                "点击机器人打开CMD终端\n\n" +
                "Version 2.0",
                "关于",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });

        // 退出
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("❌ 退出程序", null, (s, e) => ExitApplication());

        return menu;
    }

    private void ShowShortcuts()
    {
        using var dialog = new Form
        {
            Text = "快捷键",
            Size = new Size(480, 450),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White
        };

        var text = @"像素八爪鱼机器人 - 快捷键

【全局快捷键（任何窗口都可用）】
  Ctrl+Shift+P       - 暂停/继续所有机器人
  Ctrl+Shift+T       - 切换点击穿透模式
  Ctrl+Shift+M       - 打开菜单
  Ctrl+Shift+H       - 开启/关闭摸鱼模式（真正隐藏机器人）
  Ctrl+Shift+B       - 切换摸鱼伪装主题
  Ctrl+Shift+X       - 投放怪物（所有机器人集火攻击）

【程序内快捷键（关闭点击穿透后可用）】
  ESC                - 打开菜单
  F11                - 切换点击穿透模式
  空格               - 暂停/继续所有机器人

鼠标:
  左键点击机器人      - 打开该机器人的CMD终端
  右键托盘图标        - 打开菜单

终端操作:
  ESC                - 隐藏终端到托盘
  点击 X 按钮        - 隐藏到后台（机器人继续移动）
  CMD中输入 exit     - 真正关闭终端

机器人命令:
  robot-name         - 显示名字和ID
  robot-status       - 显示状态
  robot-resume       - 恢复移动
  robot-stop         - 停止移动
  robot-help         - 显示帮助

摸鱼模式伪装主题:
  Excel表格 | VS Code | CMD终端 | Word文档

托盘图标操作:
  左键/右键点击      - 显示菜单
  双击终端托盘图标   - 恢复终端窗口";

        var textBox = new TextBox
        {
            Text = text,
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.Black,
            ForeColor = Color.Lime,
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Vertical,
            Padding = new Padding(10)
        };

        dialog.Controls.Add(textBox);
        dialog.ShowDialog();
    }



    private void SetGlobalSpeed(int speed)
    {
        _globalSpeed = speed;
        foreach (var r in _robots)
        {
            r.SpeedMultiplier = speed / 100f;
        }
    }

    private void OpenRobotTerminal(Robot robot)
    {
        robot.OpenTerminal();
    }

    private void SetClickThrough(bool enable)
    {
        _clickThrough = enable;
        this.Enabled = !enable;
    }

    private void MoveTimer_Tick(object? sender, EventArgs e)
    {
        int screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        // 1. 更新所有机器人位置与基本逻辑
        foreach (var robot in _robots)
        {
            if (!robot.IsVisible) continue;
            robot.Update(screenWidth, screenHeight);
            
            // 武器大师模式下的状态提示
            if (robot.IsWeaponMaster && (robot.StatusMessage == "IDLE" || robot.StatusMessage == "BATTLE")) 
                robot.StatusMessage = "BATTLE";
        }

        // 2. 独立更新子弹位置与碰撞检测 (每帧只处理一次)
        for (int pIdx = _projectiles.Count - 1; pIdx >= 0; pIdx--)
        {
            var p = _projectiles[pIdx];
            p.Update();
            if (!p.IsActive) { _projectiles.RemoveAt(pIdx); continue; }

            // 碰撞检测：遍历所有可能被击中的机器人
            foreach (var target in _robots)
            {
                if (target == p.Owner || !target.IsVisible || !target.IsActive) continue;
                
                float pdx = p.X - (target.X + target.Size / 2);
                float pdy = p.Y - (target.Y + target.Size / 2);
                float distSq = pdx * pdx + pdy * pdy;
                float radius = target.Size / 2.5f; // 稍微收缩碰撞体积，更精准

                if (distSq < radius * radius)
                {
                    target.HandleProjectileHit(p);
                    AudioManager.PlayProjectileHitSound(p.Type);
                    p.IsActive = false;
                    break;
                }
            }
        }

        // 3. 处理机器人间的近距离社交/物理互动
        for (int i = 0; i < _robots.Count; i++)
        {
            var r1 = _robots[i];
            if (!r1.IsVisible || !r1.IsActive || r1.IsDead) continue;

            for (int j = i + 1; j < _robots.Count; j++)
            {
                var r2 = _robots[j];
                if (r2.IsVisible && r2.IsActive && !r2.IsDead)
                    r1.InteractWith(r2);
            }
        }

        // 4. 胜者吞噬与回合重置逻辑
        HandleGameRules(screenWidth, screenHeight);

        // 5. 更新怪物
        UpdateMonsters(screenWidth, screenHeight);

        this.Invalidate();
    }

    private void HandleGameRules(int sw, int sh)
    {
        if (_isGameEnding)
        {
            if (_winner == null || !_winner.IsActive) { ResetRound(); return; }

            var deadRobots = _robots.Where(r => r.IsDead && r.IsVisible).ToList();
            if (deadRobots.Count > 0)
            {
                // 胜者飞向最近的尸体
                var target = deadRobots.OrderBy(r => Math.Pow(r.X - _winner.X, 2) + Math.Pow(r.Y - _winner.Y, 2)).First();
                float dx = target.X - _winner.X;
                float dy = target.Y - _winner.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (dist < 40)
                {
                    // 吞噬！
                    target.IsVisible = false;
                    _winner.Size = Math.Min(250, (int)(_winner.Size * 1.3f)); // 限制最大体积，防止撑爆屏幕
                    _winner.HP = Math.Min(_winner.MaxHP, _winner.HP + 100);
                    _winner.SetBark("吞噬成功！力量在增长！💪", 40);
                }
                else
                {
                    // 强制移动，大幅提速
                    _winner.X += (dx / dist) * 20;
                    _winner.Y += (dy / dist) * 20;
                    _winner.Dx = 0; _winner.Dy = 0;
                }
            }
            else
            {
                // 所有尸体吃完，胜者巡场走动
                _resetTimer++;
                
                // 每 40 帧随机变换一次移动方向，模拟巡场
                if (_resetTimer % 40 == 0)
                {
                    Random rnd = new Random();
                    double angle = rnd.NextDouble() * Math.PI * 2;
                    _winner.Dx = (float)Math.Cos(angle) * 8;
                    _winner.Dy = (float)Math.Sin(angle) * 8;
                }

                _winner.X += _winner.Dx;
                _winner.Y += _winner.Dy;
                _winner.Dx *= 0.97f;
                _winner.Dy *= 0.97f;

                _winner.SpecialState = "SPINNING";
                if (_resetTimer == 30) _winner.SetBark("我是最强的霸主！无可匹敌！👑", 100);
                if (_resetTimer == 120) _winner.SetBark("重启像素协议...准备开启下个纪元。🌀", 120);
                
                if (_resetTimer > 250)
                {
                    ResetRound();
                }
            }
            return;
        }

        // 检测胜者
        var survivors = _robots.Where(r => !r.IsDead && r.IsVisible).ToList();
        if (_robots.Count > 1)
        {
            if (survivors.Count == 1)
            {
                _isGameEnding = true;
                _winner = survivors[0];
                _winner.SetBark("最后的赢家诞生了！我要享用我的奖赏！😋", 150);
                _resetTimer = 0;
                _projectiles.Clear();
            }
            else if (survivors.Count == 0)
            {
                ResetRound();
            }
        }
    }

    /// <summary>
    /// 更新所有怪物状态和碰撞检测
    /// </summary>
    private void UpdateMonsters(int screenWidth, int screenHeight)
    {
        // 更新怪物
        foreach (var monster in _monsters)
        {
            if (monster.IsActive && !monster.IsDead)
            {
                monster.Update(screenWidth, screenHeight);
            }
        }

        // 清理已死亡的怪物
        _monsters.RemoveAll(m => m.IsDead);

        if (_monsters.Count == 0) return;

        // 检测子弹与怪物的碰撞
        for (int pIdx = _projectiles.Count - 1; pIdx >= 0; pIdx--)
        {
            var p = _projectiles[pIdx];
            if (!p.IsActive) continue;

            foreach (var monster in _monsters)
            {
                if (!monster.IsActive || monster.IsDead) continue;

                var (centerX, centerY) = monster.GetCenter();
                float pdx = p.X - centerX;
                float pdy = p.Y - centerY;
                float distSq = pdx * pdx + pdy * pdy;
                float radius = monster.Size / 2.5f;

                if (distSq < radius * radius)
                {
                    // 计算伤害
                    int damage = p.Type switch
                    {
                        "CANNON" => 150,
                        "LIGHTNING" => 80,
                        "ROCKET" => 200,
                        "PLASMA" => 120,
                        "SPIT" => 50,
                        "INK" => 60,
                        _ => 100
                    };

                    monster.TakeDamage(damage);
                    p.IsActive = false;

                    // 如果怪物死亡，给予攻击者奖励
                    if (monster.IsDead && p.Owner != null)
                    {
                        p.Owner.SetBark("🎉 击败了怪物！获得奖励！", 100);
                        p.Owner.HP = Math.Min(p.Owner.MaxHP, p.Owner.HP + 200);
                        p.Owner.Size = Math.Min(150, p.Owner.Size + 10);
                    }

                    break;
                }
            }
        }

        // 让机器人自动攻击怪物
        foreach (var robot in _robots)
        {
            if (!robot.IsActive || robot.IsDead) continue;

            // 如果机器人没有追逐目标，自动锁定怪物
            if (robot.ChasingTarget == null || robot.ChasingTarget.IsDead)
            {
                var targetMonster = _monsters.FirstOrDefault(m => m.IsActive && !m.IsDead);
                if (targetMonster != null)
                {
                    robot.SetMonsterTarget(targetMonster);
                }
            }
        }
    }

    private void ResetRound()
    {
        _isGameEnding = false;
        _winner = null;
        _resetTimer = 0;
        _projectiles.Clear(); // 清空所有旧子弹

        // 清理怪物
        _monsters.Clear();

        Random rnd = new Random();
        foreach (var r in _robots)
        {
            r.IsDead = false;
            r.IsVisible = true;
            r.IsActive = true;
            r.IsMoving = true;
            r.HP = r.MaxHP;
            r.Size = r.OriginalSize;
            r.X = rnd.Next(Screen.PrimaryScreen.Bounds.Width - 100);
            r.Y = rnd.Next(Screen.PrimaryScreen.Bounds.Height - 100);
            
            // 立即启动动力系统：更快的起始速度
            double angle = rnd.NextDouble() * Math.PI * 2;
            float speed = 3.5f + (float)rnd.NextDouble() * 3.0f; // 大幅提高复活后的起始初速度
            r.Dx = (float)Math.Cos(angle) * speed;
            r.Dy = (float)Math.Sin(angle) * speed;
            
            r.RotationAngle = 0;
            r.PauseTimer = 0;
            r.StunTimer = 0;
            r.SlowTimer = 0;
            r.ChaseTimer = 0;
            r.DuelTimer = 0;
            r.AggressionTimer = 0;
            r.SpecialState = "NORMAL";
            r.IsFiringLaser = false;
            r.IsAiSpeaking = false;
            r.ChatTimer = 0;
            r.EmojiBubbleTimer = 0;
            r.StunTimer = 0;
            r.BlindTimer = 0;
            r.SocialCooldown = 0;

            r.SetBark("新纪元重组完成！战斗继续！⚡", 100);
        }
    }

    private void PetForm_Paint(object? sender, PaintEventArgs e)
    {
        // 摸鱼模式下绘制伪装界面
        if (_bossMode)
        {
            PixelRobotRenderer.DrawBossModeIndicator(e.Graphics,
                Screen.PrimaryScreen!.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height,
                _bossModeTheme);
            return; // 不绘制机器人
        }

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        foreach (var robot in _robots)
        {
            if (robot.IsVisible)
                PixelRobotRenderer.DrawRobot(e.Graphics, robot);
        }

        // 绘制子弹 (特效增强版)
        var projectilesCopy = _projectiles.ToArray(); // 创建快照防止并发修改崩溃
        foreach (var p in projectilesCopy)
        {
            if (p == null || !p.IsActive) continue;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            switch (p.Type)
            {
                case "CANNON":
                    // 渐变球体感
                    using (var cannonBrush = new System.Drawing.Drawing2D.LinearGradientBrush(new RectangleF(p.X - 10, p.Y - 10, 20, 20), Color.FromArgb(60, 60, 60), Color.Black, 45f))
                    {
                        e.Graphics.FillEllipse(cannonBrush, p.X - 9, p.Y - 9, 18, 18);
                    }
                    e.Graphics.DrawEllipse(Pens.DimGray, p.X - 9, p.Y - 9, 18, 18);
                    break;

                case "LIGHTNING":
                    // 绘制三层发光叠波闪电
                    Color[] lightningColors = { Color.White, Color.Yellow, Color.FromArgb(150, Color.Gold) };
                    float[] lightningWidths = { 1f, 3f, 6f };
                    for (int layer = 2; layer >= 0; layer--)
                    {
                        using var pen = new Pen(lightningColors[layer], lightningWidths[layer]);
                        float lx = p.X - p.Dx, ly = p.Y - p.Dy;
                        Random rng = new Random((int)p.X + (int)p.Y + layer); // 固定种子保持帧间稳定感但随位置变
                        for (int k = 0; k < 4; k++)
                        {
                            float nxtX = lx + p.Dx * 0.4f + rng.Next(-12, 12);
                            float nxtY = ly + p.Dy * 0.4f + rng.Next(-12, 12);
                            e.Graphics.DrawLine(pen, lx, ly, nxtX, nxtY);
                            lx = nxtX; ly = nxtY;
                        }
                    }
                    break;

                case "SPIT":
                    // 粘稠液体感
                    using (var spitBrush = new SolidBrush(Color.FromArgb(160, Color.Chartreuse)))
                    {
                        e.Graphics.FillEllipse(spitBrush, p.X - 6, p.Y - 6, 12, 12);
                        // 拖尾液滴
                        e.Graphics.FillEllipse(spitBrush, p.X - p.Dx * 1.5f + 4, p.Y - p.Dy * 1.5f, 5, 5);
                        e.Graphics.FillEllipse(spitBrush, p.X - p.Dx * 0.8f - 3, p.Y - p.Dy * 0.8f + 2, 4, 4);
                    }
                    break;

                case "INK":
                    // 墨水分离感
                    using (var inkBrush = new SolidBrush(Color.FromArgb(200, Color.Indigo)))
                    {
                        float currentAngle = (float)Math.Atan2(p.Dy, p.Dx);
                        e.Graphics.FillEllipse(inkBrush, p.X - 8, p.Y - 8, 16, 16);
                        e.Graphics.FillEllipse(inkBrush, p.X + (float)Math.Cos(currentAngle + 0.5) * 10, p.Y + (float)Math.Sin(currentAngle + 0.5) * 10, 6, 6);
                        e.Graphics.FillEllipse(inkBrush, p.X + (float)Math.Cos(currentAngle - 0.5) * 12, p.Y + (float)Math.Sin(currentAngle - 0.5) * 12, 5, 5);
                    }
                    break;

                case "ROCKET":
                    // 带有火光的火箭
                    using (var rocketPath = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        float rSize = 10;
                        rocketPath.AddRectangle(new RectangleF(p.X - rSize/2, p.Y - rSize/2, rSize, rSize));
                        e.Graphics.FillPath(Brushes.DarkRed, rocketPath);
                        // 尾部火焰
                        using var fireBrush = new System.Drawing.Drawing2D.LinearGradientBrush(new PointF(p.X, p.Y), new PointF(p.X - p.Dx * 2, p.Y - p.Dy * 2), Color.Orange, Color.Transparent);
                        e.Graphics.FillEllipse(fireBrush, p.X - p.Dx * 1.5f - 5, p.Y - p.Dy * 1.5f - 5, 10, 10);
                    }
                    break;

                case "PLASMA":
                    // 核心高能脉冲
                    e.Graphics.FillEllipse(Brushes.White, p.X - 3, p.Y - 3, 6, 6);
                    using (var pPen = new Pen(Color.FromArgb(150, p.ProjectileColor), 4))
                    {
                        e.Graphics.DrawEllipse(pPen, p.X - 7, p.Y - 7, 14, 14);
                    }
                    using (var outerPen = new Pen(Color.FromArgb(60, p.ProjectileColor), 8))
                    {
                        e.Graphics.DrawEllipse(outerPen, p.X - 10, p.Y - 10, 20, 20);
                    }
                    break;

                default:
                    using (var defBrush = new SolidBrush(p.ProjectileColor))
                    {
                        e.Graphics.FillEllipse(defBrush, p.X - 3, p.Y - 3, 6, 6);
                    }
                    break;
            }
        }

        // 绘制怪物
        foreach (var monster in _monsters)
        {
            if (monster.IsActive && !monster.IsDead)
                MonsterRenderer.DrawMonster(e.Graphics, monster);
        }
    }

    private void PetForm_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        SetClickThrough(false);

        // 从后向前检测
        for (int i = _robots.Count - 1; i >= 0; i--)
        {
            var robot = _robots[i];
            if (robot.HitTest(e.X, e.Y))
            {
                robot.OpenTerminal();
                return;
            }
        }

        SetClickThrough(true);
    }

    private void PetForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // 这些快捷键只在程序有焦点时响应（点击穿透关闭时）
        if (e.KeyCode == Keys.F11)
        {
            SetClickThrough(!_clickThrough);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            // 显示菜单
            if (_notifyIcon != null)
            {
                _notifyIcon.ContextMenuStrip = CreateContextMenu();
                _notifyIcon.ContextMenuStrip.Show(Cursor.Position);
            }
        }
        else if (e.KeyCode == Keys.Space)
        {
            TogglePauseAll();
        }
    }

    private void ExitApplication()
    {
        try
        {
            PersistenceManager.SaveRobots(_robots);
            
            // 彻底清理终端
            if (TerminalManagerForm.Instance != null)
            {
                TerminalManagerForm.Instance.Shutdown();
            }

            foreach (var robot in _robots)
            {
                robot.CloseTerminal();
            }

            _notifyIcon?.Dispose();
            _controlPanel?.Close();
            _settingsForm?.Close();
        }
        catch { }
        
        this.Hide();
        // 如果是从主控界面进入的，恢复主控界面
        if (MoyuLauncher.Instance != null && !MoyuLauncher.Instance.IsDisposed)
        {
            MoyuLauncher.Instance.Show();
            MoyuLauncher.Instance.Focus();
        }
        else
        {
            // 如果主窗口不在了，才真正退出
            Application.Exit();
        }
    }

    // 公共方法供控制面板使用
    public List<Robot> GetRobots() => _robots;

    public Robot? SpawnRobotWithName()
    {
        using var nameDialog = new Form
        {
            Text = "命名机器人",
            Size = new Size(350, 180),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        var names = new[] { "Claude", "Alpha", "Beta", "Gamma", "Delta", "Octo", "Pixel", "Byte", "Bit", "Neo" };
        var defaultName = _robotIdCounter == 1 ? DefaultRobotName : names[new Random().Next(names.Length)];

        var label = new Label
        {
            Text = $"为机器人 #{_robotIdCounter} 命名:",
            Location = new Point(20, 20),
            Size = new Size(300, 30),
            Font = new Font("Microsoft YaHei", 11),
            ForeColor = Color.White
        };

        var textBox = new TextBox
        {
            Location = new Point(20, 55),
            Size = new Size(290, 30),
            Text = defaultName,
            Font = new Font("Microsoft YaHei", 11),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnOk = new Button
        {
            Text = "投放",
            Location = new Point(100, 100),
            Size = new Size(120, 35),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Lime,
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
        };

        nameDialog.Controls.Add(label);
        nameDialog.Controls.Add(textBox);
        nameDialog.Controls.Add(btnOk);
        nameDialog.AcceptButton = btnOk;

        if (nameDialog.ShowDialog() == DialogResult.OK)
        {
            string name = textBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) name = $"Robot-{_robotIdCounter:D3}";
            return SpawnRobot(name, -1, -1);
        }
        return null;
    }

    public Robot SpawnRobot(string name, float startX, float startY)
    {
        int screenWidth = Screen.PrimaryScreen!.Bounds.Width;

        if (startX < 0)
        {
            startX = new Random().Next(screenWidth - 100);
            startY = -80;
        }

        Robot robot = new Robot(_robotIdCounter, name, startX, startY);
        robot.Size = DefaultRobotSize + new Random().Next(-10, 10);
        robot.SpeedMultiplier = _globalSpeed / 100f;
        robot.EnableAiThinking = EnableAiThinking;
        robot.AiThoughtFrequency = AiThoughtFrequency;
        robot.FightFrequency = FightFrequency;
        robot.IsWeaponMaster = IsWeaponMaster;
        robot.PersonalityType = DefaultPersonality;
        robot.InitializePersonalityTraits();


        _robots.Add(robot);
        _robotIdCounter++;

        robot.OnGrowthUpdated += (r) => PersistenceManager.SaveRobots(_robots);
        SkillManager.SaveRobotSkills(robot);

        PersistenceManager.SaveRobots(_robots);
        ShowNotification($"Robot '{name}' deployed!");
        return robot;
    }

    private Robot RestoreRobot(RobotData data)
    {
        int screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen!.Bounds.Height;
        float x = new Random().Next(screenWidth - 100);
        float y = new Random().Next(screenHeight - 100);

        Robot robot = new Robot(data.Id, data.Name, x, y);
        robot.Personality = data.Personality;
        robot.PersonalityType = (RobotPersonalityType)data.PersonalityType;
        robot.InitializePersonalityTraits();
        robot.CurrentEmotion = (EmotionState)data.CurrentEmotion;
        robot.ConsciousnessLevel = data.ConsciousnessLevel;
        robot.Experience = data.Experience;
        robot.InternalGuidelines = data.InternalGuidelines;
        robot.Size = data.Size;
        robot.SpeedMultiplier = data.SpeedMultiplier;
        robot.EnableAiThinking = EnableAiThinking;
        robot.AiThoughtFrequency = AiThoughtFrequency;
        robot.FightFrequency = FightFrequency;
        robot.IsWeaponMaster = IsWeaponMaster;

        robot.PrimaryColor = Color.FromArgb(data.PrimaryColorR, data.PrimaryColorG, data.PrimaryColorB);

        // 优先加载专门的技能文件
        var savedSkills = SkillManager.LoadRobotSkills(data.Id, data.Name);
        if (savedSkills != null && savedSkills.Count > 0)
        {
            robot.Skills = savedSkills;
        }
        else if (data.Skills != null && data.Skills.Count > 0)
        {
            robot.Skills = data.Skills;
        }

        foreach (var insight in data.LearnedInsights) robot.LearnedInsights.Add(insight);
        if (data.CustomPhrases != null) robot.CustomPhrases = data.CustomPhrases;


        robot.OnGrowthUpdated += (r) => PersistenceManager.SaveRobots(_robots);
        _robots.Add(robot);
        return robot;
    }

    public void ClearAllRobots()
    {
        _robots.Clear();
        PersistenceManager.SaveRobots(_robots);
    }

    public void RemoveRobot(Robot robot)
    {
        if (_robots.Contains(robot))
        {
            robot.CloseTerminal();
            _robots.Remove(robot);
            PersistenceManager.SaveRobots(_robots);
        }
    }

    public void ShowSettings()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm();
            _settingsForm.RobotSize = DefaultRobotSize;
            _settingsForm.RobotName = DefaultRobotName;
            _settingsForm.RobotSpeed = _globalSpeed;
            _settingsForm.ShowNamingDialog = ShowNamingDialog;
            _settingsForm.EnableAiThinking = EnableAiThinking;
            _settingsForm.AiThoughtFrequency = AiThoughtFrequency;
            _settingsForm.FightFrequency = FightFrequency;
            _settingsForm.IsWeaponMaster = IsWeaponMaster;
            _settingsForm.DefaultPersonality = DefaultPersonality;
            _settingsForm.FormClosed += (sender, args) =>
            {
                if (_settingsForm.DialogResult == DialogResult.OK)
                {
                    DefaultRobotSize = _settingsForm.RobotSize;
                    DefaultRobotName = _settingsForm.RobotName;
                    _globalSpeed = _settingsForm.RobotSpeed;
                    ShowNamingDialog = _settingsForm.ShowNamingDialog;
                    EnableAiThinking = _settingsForm.EnableAiThinking;
                    AiThoughtFrequency = _settingsForm.AiThoughtFrequency;
                    FightFrequency = _settingsForm.FightFrequency;
                    IsWeaponMaster = _settingsForm.IsWeaponMaster;
                    DefaultPersonality = _settingsForm.DefaultPersonality;


                    foreach (var r in _robots) r.SpeedMultiplier = _globalSpeed / 100f;
                    foreach (var r in _robots) r.EnableAiThinking = EnableAiThinking;
                    foreach (var r in _robots) r.AiThoughtFrequency = AiThoughtFrequency;
                    foreach (var r in _robots) r.FightFrequency = FightFrequency;
                    foreach (var r in _robots) r.IsWeaponMaster = IsWeaponMaster;
                }
                _settingsForm = null;
            };
            _settingsForm.Show();
        }
        else
        {
            _settingsForm.Activate();
        }
    }

    public void AddProjectile(Projectile p)
    {
        if (p != null)
        {
            _projectiles.Add(p);
        }
    }

    private void ShowControlPanel()
    {
        if (_controlPanel == null || _controlPanel.IsDisposed)
        {
            _controlPanel = new ControlPanelForm(this);
            _controlPanel.FormClosed += (s, e) => _controlPanel = null;
            _controlPanel.Show();
        }
        else
        {
            _controlPanel.Activate();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 如果不是由于 Application.Exit 引起的，则尝试调用统一退出逻辑
        if (e.CloseReason == CloseReason.UserClosing)
        {
             ExitApplication();
             return; // ExitApplication 会直接杀死进程
        }

        // 兜底清理逻辑
        UnregisterHotKey(this.Handle, HOTKEY_ID_PAUSE);
        UnregisterHotKey(this.Handle, HOTKEY_ID_BOSS);
        UnregisterHotKey(this.Handle, HOTKEY_ID_BOSS_CYCLE);
        _moveTimer?.Stop();
        _moveTimer?.Dispose();
        _notifyIcon?.Dispose();
        
        base.OnFormClosing(e);
    }
}
