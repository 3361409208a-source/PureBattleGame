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

    private void InitializeTray()
    {
        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "PURE BATTLE HUB";

        using (Bitmap bmp = new Bitmap(16, 16))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillEllipse(Brushes.DodgerBlue, 1, 1, 14, 14);
            _trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
        }

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主界面", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.BringToFront(); });
        menu.Items.Add("-");
        menu.Items.Add("彻底退出", null, (s, e) => Application.Exit());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.Visible = true;
        _trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.BringToFront(); };
    }

    protected override void OnBridgeReady(WebUIBridge bridge)
    {
        bridge.RegisterSyncHandler("getLauncherStats", payload => new
        {
            opacity = SettingsManager.Current.DefaultOpacity,
            homeUrl = SettingsManager.Current.HomeUrl,
            robotCount = PetForm.Instance?.GetRobots().Count ?? 0,
            isPetActive = _petInstance != null && _petInstance.Visible,
            isGameActive = _gameInstance != null && _gameInstance.Visible,
            isBrowserActive = BrowserForm.Instance != null && BrowserForm.Instance.Visible
        });

        bridge.RegisterSyncHandler("setLauncherOpacity", payload =>
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
            return true;
        });

        bridge.RegisterSyncHandler("saveLauncherSettings", payload =>
        {
            if (payload.TryGetProperty("homeUrl", out var urlProp))
                SettingsManager.Current.HomeUrl = urlProp.GetString() ?? "https://www.xiaoheiv.top";
            if (payload.TryGetProperty("opacity", out var opProp))
            {
                double op = Math.Clamp(opProp.GetDouble(), 0.1, 1.0);
                SettingsManager.Current.DefaultOpacity = op;
                this.Opacity = op;
            }
            SettingsManager.Save();
            return true;
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
        UnregisterHotKey(this.Handle, HOTKEY_ID);
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.OnFormClosing(e);
    }
}
