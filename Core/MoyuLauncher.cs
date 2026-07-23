using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using PureBattleGame.Games.StarCoreDefense;
using PureBattleGame.Games.CockroachPet;

namespace PureBattleGame.Core;

public partial class MoyuLauncher : WebUIHostForm
{
    public static MoyuLauncher? Instance { get; private set; }
    private BattleForm? _gameInstance;
    private PetForm? _petInstance;
    private NotifyIcon _trayIcon = null!;
    private bool _wasBrowserVisible = false;
    private bool _wasGameVisible = false;

    // 全局热键常量 (Alt + Space)
    private const int HOTKEY_ID = 9000;
    private const uint MOD_ALT = 0x0001;
    private const uint VK_SPACE = 0x20;

    public MoyuLauncher() : base("launcher", "PURE BATTLE HUB | 摸鱼游戏主控中心")
    {
        Instance = this;
        this.Size = new Size(460, 520);
        this.MinimumSize = new Size(420, 480);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(13, 14, 21);
        this.DoubleBuffered = true;
        this.Opacity = SettingsManager.Current.DefaultOpacity;

        InitializeTray();
    }

    private Icon _trayIconGraphic = null!;

    private void InitializeTray()
    {
        if (_trayIcon != null) return;

        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "PURE BATTLE HUB | 摸鱼游戏主控中心";

        // 使用高品质 Bitmap 绘制图标并克隆 Icon 实例，防止被 GC 释放后在系统托盘消失
        using (Bitmap bmp = new Bitmap(32, 32))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillEllipse(brush, 2, 2, 28, 28);
                }
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawEllipse(pen, 2, 2, 28, 28);
                    using (var font = new Font("Segoe UI", 13, FontStyle.Bold))
                    {
                        g.DrawString("P", font, Brushes.White, new PointF(7, 3));
                    }
                }
            }
            IntPtr hIcon = bmp.GetHicon();
            _trayIconGraphic = (Icon)Icon.FromHandle(hIcon).Clone();
            _trayIcon.Icon = _trayIconGraphic;
        }

        var menu = new ContextMenuStrip();
        menu.BackColor = Color.FromArgb(24, 24, 30);
        menu.ForeColor = Color.White;

        menu.Items.Add("🎮 显示摸鱼主控台 (Launcher)", null, (s, e) => ShowLauncherWindow());
        menu.Items.Add("💬 机器人社交中心 & 控制台", null, (s, e) => TerminalManagerForm.Instance.ShowWorldChat());
        menu.Items.Add("🌐 极速浏览器", null, (s, e) => {
            BrowserForm.Instance.Opacity = SettingsManager.Current.DefaultOpacity;
            BrowserForm.Instance.Show();
            BrowserForm.Instance.BringToFront();
        });
        menu.Items.Add("🏆 星核防线挂机塔防", null, (s, e) => {
            if (_gameInstance == null || _gameInstance.IsDisposed) _gameInstance = new BattleForm();
            _gameInstance.Opacity = SettingsManager.Current.DefaultOpacity;
            _gameInstance.Show();
            _gameInstance.BringToFront();
        });
        menu.Items.Add("🐜 像素电子宠终端", null, (s, e) => {
            if (_petInstance == null || _petInstance.IsDisposed) _petInstance = new PetForm();
            _petInstance.Opacity = SettingsManager.Current.DefaultOpacity;
            _petInstance.Show();
            _petInstance.BringToFront();
        });
        menu.Items.Add("-");
        menu.Items.Add("⚡ 摸鱼防挂显示/隐藏 (Alt+Space)", null, (s, e) => ToggleBossVisibility());
        menu.Items.Add("-");
        menu.Items.Add("❌ 彻底退出程序", null, (s, e) => ExitApplication());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.Visible = true;

        // 左键单击或双击托盘图标切换主界面显示与最小化
        _trayIcon.MouseClick += (s, e) => {
            if (e.Button == MouseButtons.Left)
            {
                ToggleLauncherWindow();
            }
        };
    }

    private void ToggleLauncherWindow()
    {
        if (this.Visible && this.Opacity > 0.1 && this.WindowState != FormWindowState.Minimized)
        {
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }
        else
        {
            ShowLauncherWindow();
        }
    }

    private void ShowLauncherWindow()
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Opacity = SettingsManager.Current.DefaultOpacity;
        this.ShowInTaskbar = true;
        this.BringToFront();
        this.Activate();
    }

    private void ExitApplication()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        Application.Exit();
    }

    protected override void OnBridgeReady(WebUIBridge bridge)
    {
        bridge.RegisterSyncHandler("getSettings", payload => new
        {
            opacity = SettingsManager.Current.DefaultOpacity,
            homeUrl = SettingsManager.Current.HomeUrl,
            autoStart = SettingsManager.Current.AutoStart,
            hideNameAndPersonality = SettingsManager.Current.HideNameAndPersonality,
            curseModeByDefault = SettingsManager.Current.CurseModeByDefault,
            battleMode = SettingsManager.Current.BattleMode,
            languageMode = SettingsManager.Current.LanguageInteractionMode,
            actionMode = SettingsManager.Current.ActionInteractionMode,
            robotSize = SettingsManager.Current.RobotSize,
            robotSpeed = SettingsManager.Current.RobotSpeed,
            skillScale = SettingsManager.Current.SkillScale,
            soundVolume = SettingsManager.Current.SoundVolume,
            fightFrequency = SettingsManager.Current.FightFrequency,
            enableAiThinking = SettingsManager.Current.EnableAiThinking,
            aiThoughtFrequency = SettingsManager.Current.AiThoughtFrequency,
            isWeaponMaster = SettingsManager.Current.IsWeaponMaster,
            robotMaxHp = SettingsManager.Current.RobotMaxHp,
            isGodMode = SettingsManager.Current.IsGodMode,
            apiKey = PersistenceManager.GetApiKey()
        });

        bridge.RegisterSyncHandler("saveSettings", payload =>
        {
            if (payload.TryGetProperty("opacity", out var opProp))
            {
                double op = Math.Clamp(opProp.GetDouble(), 0.1, 1.0);
                SettingsManager.Current.DefaultOpacity = op;
                this.Opacity = op;
                if (BrowserForm.Instance != null) BrowserForm.Instance.Opacity = op;
                if (_gameInstance != null) _gameInstance.Opacity = op;
                if (_petInstance != null) _petInstance.Opacity = op;
            }
            if (payload.TryGetProperty("homeUrl", out var urlProp))
                SettingsManager.Current.HomeUrl = urlProp.GetString() ?? "https://www.xiaoheiv.top";
            if (payload.TryGetProperty("autoStart", out var autoProp))
                SettingsManager.Current.AutoStart = autoProp.GetBoolean();

            if (payload.TryGetProperty("hideNameAndPersonality", out var hideProp))
                SettingsManager.Current.HideNameAndPersonality = hideProp.GetBoolean();
            if (payload.TryGetProperty("curseModeByDefault", out var curseProp))
                SettingsManager.Current.CurseModeByDefault = curseProp.GetBoolean();
            if (payload.TryGetProperty("languageMode", out var langProp))
                SettingsManager.Current.LanguageInteractionMode = langProp.GetString() ?? "互骂吐槽";
            if (payload.TryGetProperty("actionMode", out var actProp))
            {
                string act = actProp.GetString() ?? "近远交替";
                SettingsManager.Current.ActionInteractionMode = act;
                SettingsManager.Current.BattleMode = act;
            }

            if (payload.TryGetProperty("robotSize", out var sizeProp))
                SettingsManager.Current.RobotSize = sizeProp.GetInt32();
            if (payload.TryGetProperty("robotSpeed", out var speedProp))
                SettingsManager.Current.RobotSpeed = speedProp.GetInt32();
            if (payload.TryGetProperty("skillScale", out var scaleProp))
                SettingsManager.Current.SkillScale = scaleProp.GetInt32();
            if (payload.TryGetProperty("soundVolume", out var volProp))
            {
                int vol = volProp.GetInt32();
                SettingsManager.Current.SoundVolume = vol;
                PureBattleGame.Games.CockroachPet.AudioManager.VolumeScale = vol / 100.0f;
            }
            if (payload.TryGetProperty("fightFrequency", out var fightProp))
                SettingsManager.Current.FightFrequency = fightProp.GetInt32();
            if (payload.TryGetProperty("enableAiThinking", out var aiProp))
                SettingsManager.Current.EnableAiThinking = aiProp.GetBoolean();
            if (payload.TryGetProperty("aiThoughtFrequency", out var aiFreqProp))
                SettingsManager.Current.AiThoughtFrequency = aiFreqProp.GetInt32();
            if (payload.TryGetProperty("isWeaponMaster", out var masterProp))
                SettingsManager.Current.IsWeaponMaster = masterProp.GetBoolean();

            if (payload.TryGetProperty("robotMaxHp", out var hpProp))
            {
                int maxHp = Math.Max(100, hpProp.GetInt32());
                SettingsManager.Current.RobotMaxHp = maxHp;
                var activeRobots = PetForm.Instance?.GetRobots() ?? new List<PureBattleGame.Games.CockroachPet.Robot>();
                foreach (var r in activeRobots)
                {
                    r.MaxHP = maxHp;
                    if (r.HP > maxHp) r.HP = maxHp;
                }
            }

            if (payload.TryGetProperty("isGodMode", out var godProp))
            {
                bool god = godProp.GetBoolean();
                SettingsManager.Current.IsGodMode = god;
                var activeRobots = PetForm.Instance?.GetRobots() ?? new List<PureBattleGame.Games.CockroachPet.Robot>();
                foreach (var r in activeRobots)
                {
                    r.IsGodMode = god;
                    if (god) { r.HP = r.MaxHP; r.IsDead = false; }
                }
            }

            if (payload.TryGetProperty("apiKey", out var keyProp))
            {
                var appSet = PersistenceManager.LoadAppSettings();
                appSet.ApiKey = keyProp.GetString() ?? "";
                PersistenceManager.SaveAppSettings(appSet);
            }

            SettingsManager.Save();
            return true;
        });

        bridge.RegisterSyncHandler("getLauncherStats", payload => new
        {
            opacity = SettingsManager.Current.DefaultOpacity,
            homeUrl = SettingsManager.Current.HomeUrl,
            robotCount = PetForm.Instance?.GetRobots().Count ?? 0,
            isPetActive = _petInstance != null && _petInstance.Visible,
            isGameActive = _gameInstance != null && _gameInstance.Visible,
            isBrowserActive = BrowserForm.Instance != null && BrowserForm.Instance.Visible
        });

        bridge.RegisterSyncHandler("launchGameNav", payload =>
        {
            this.Invoke(() =>
            {
                BrowserForm.Instance.Opacity = SettingsManager.Current.DefaultOpacity;
                BrowserForm.Instance.Navigate("https://www.xiaoheiv.top");
            });
            return true;
        });

        bridge.RegisterSyncHandler("launchBrowser", payload =>
        {
            this.Invoke(() =>
            {
                BrowserForm.Instance.Opacity = SettingsManager.Current.DefaultOpacity;
                BrowserForm.Instance.Navigate(SettingsManager.Current.HomeUrl);
            });
            return true;
        });

        bridge.RegisterSyncHandler("launchStarDefense", payload =>
        {
            this.Invoke(() =>
            {
                if (_gameInstance == null || _gameInstance.IsDisposed) _gameInstance = new BattleForm();
                this.Hide();
                _gameInstance.Opacity = SettingsManager.Current.DefaultOpacity;
                _gameInstance.Show();
                _gameInstance.Focus();
            });
            return true;
        });

        bridge.RegisterSyncHandler("launchPixelPet", payload =>
        {
            this.Invoke(() =>
            {
                if (_petInstance == null || _petInstance.IsDisposed) _petInstance = new PetForm();
                this.Hide();
                _petInstance.Opacity = SettingsManager.Current.DefaultOpacity;
                _petInstance.Show();
                _petInstance.Focus();
            });
            return true;
        });

        bridge.RegisterSyncHandler("launchSocialHub", payload =>
        {
            this.Invoke(() =>
            {
                TerminalManagerForm.Instance.ShowWorldChat();
            });
            return true;
        });

        bridge.RegisterSyncHandler("minimizeLauncher", payload =>
        {
            this.Invoke(() =>
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            });
            return true;
        });

        bridge.RegisterSyncHandler("dragWindow", payload =>
        {
            this.Invoke(() =>
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
            });
            return true;
        });
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, VK_SPACE);
    }

    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID)
        {
            ToggleBossVisibility();
            return;
        }

        if (m.Msg == 0x0112 && ((int)m.WParam & 0xFFF0) == 0xF100)
        {
            ToggleBossVisibility();
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    private void ToggleBossVisibility()
    {
        bool isAnyVisible = this.Opacity > 0.1 ||
                           (BrowserForm.Instance != null && BrowserForm.Instance.Visible && BrowserForm.Instance.Opacity > 0.1) ||
                           (_gameInstance != null && _gameInstance.Visible && _gameInstance.Opacity > 0.1) ||
                           (_petInstance != null && _petInstance.Visible && _petInstance.Opacity > 0.1);

        if (isAnyVisible)
        {
            _wasBrowserVisible = BrowserForm.Instance.Visible;
            _wasGameVisible = (_gameInstance != null && _gameInstance.Visible);

            this.Tag = this.Opacity;
            this.Opacity = 0.0;
            this.ShowInTaskbar = false;
            if (BrowserForm.Instance.Visible) BrowserForm.Instance.Hide();
            if (_gameInstance != null && _gameInstance.Visible) _gameInstance.Hide();
            if (_petInstance != null && _petInstance.Visible) _petInstance.Hide();
        }
        else
        {
            this.Opacity = (this.Tag is double op && op > 0.1) ? op : SettingsManager.Current.DefaultOpacity;
            this.ShowInTaskbar = true;

            if (_wasGameVisible && _gameInstance != null)
            {
                this.Hide(); _gameInstance.Show(); _gameInstance.Opacity = this.Opacity;
                _gameInstance.BringToFront(); SetForegroundWindow(_gameInstance.Handle);
            }
            else if (_wasBrowserVisible)
            {
                this.Hide(); BrowserForm.Instance.Show(); BrowserForm.Instance.Opacity = this.Opacity;
                BrowserForm.Instance.BringToFront(); SetForegroundWindow(BrowserForm.Instance.Handle);
            }
            else
            {
                this.Show(); this.BringToFront(); this.Activate(); SetForegroundWindow(this.Handle);
            }
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData & Keys.Alt) == Keys.Alt)
        {
            Keys baseKey = keyData & ~Keys.Alt;
            if (baseKey == Keys.Up) { this.Opacity = Math.Min(1.0, this.Opacity + 0.1); return true; }
            if (baseKey == Keys.Down) { this.Opacity = Math.Max(0.1, this.Opacity - 0.1); return true; }
            if (baseKey == Keys.Space)
            {
                if (this.Opacity > 0.1) { this.Tag = this.Opacity; this.Opacity = 0.0; this.ShowInTaskbar = false; }
                else { this.Opacity = (this.Tag is double op && op > 0.1) ? op : SettingsManager.Current.DefaultOpacity; this.ShowInTaskbar = true; }
                return true;
            }
            if (baseKey == Keys.B) { BrowserForm.Instance.ToggleVisibility(); return true; }
            if (baseKey == Keys.Q) { if (!this.Visible) this.Show(); this.BringToFront(); return true; }
            if ((keyData & Keys.Shift) == Keys.Shift && baseKey == Keys.Q) Environment.Exit(0);
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            return;
        }
        UnregisterHotKey(this.Handle, HOTKEY_ID);
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnFormClosing(e);
    }
}
