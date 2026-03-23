using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using PureBattleGame.Games.StarCoreDefense;

namespace PureBattleGame.Core;

public partial class MoyuLauncher : Form
{
    public static MoyuLauncher? Instance { get; private set; }
    private BattleForm? _gameInstance;
    private Panel _settingsPanel = null!;
    private NotifyIcon _trayIcon = null!;
    private bool _wasBrowserVisible = false; 
    private bool _wasGameVisible = false; // 用于记忆游戏状态

    // 全局热键常量
    private const int HOTKEY_ID = 9000;
    private const uint MOD_ALT = 0x0001;
    private const uint VK_SPACE = 0x20;

    public MoyuLauncher()
    {
        Instance = this;
        InitializeComponent();
        InitializeTray();
        this.DoubleBuffered = true;
        this.Opacity = SettingsManager.Current.DefaultOpacity;
    }

    private void InitializeTray()
    {
        // 创建系统托盘图标
        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "PURE BATTLE HUB";
        
        // 生成一个极简的 16x16 图标 (实心圆)
        using (Bitmap bmp = new Bitmap(16, 16))
        using (Graphics g = Graphics.FromImage(bmp)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
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

    private void InitializeComponent()
    {
        this.Text = "PURE BATTLE HUB";
        this.Size = new Size(420, 420);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(28, 28, 34);

        var masterLayout = new FlowLayoutPanel {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
        };
        masterLayout.MouseDown += TitlePanel_MouseDown;
        this.Controls.Add(masterLayout);

        // 1. 顶部栏
        var titlePanel = new Panel { Size = new Size(420, 50), BackColor = Color.FromArgb(20, 20, 23), Margin = new Padding(0) };
        titlePanel.MouseDown += TitlePanel_MouseDown;
        masterLayout.Controls.Add(titlePanel);

        var lblTitle = new Label { Text = "PURE BATTLE HUB", Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), ForeColor = Color.FromArgb(160, 160, 160), AutoSize = true, Location = new Point(20, 15), Enabled = false };
        titlePanel.Controls.Add(lblTitle);

        var ctrlPanel = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 90, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 10, 0), BackColor = Color.Transparent };
        titlePanel.Controls.Add(ctrlPanel);
        
        // 关闭行为：最小化到托盘 (满足用户“要在状态栏显示”的需求)
        ctrlPanel.Controls.Add(CreateHeaderBtn("✕", (s, e) => {
            this.WindowState = FormWindowState.Minimized;
            this.Hide(); // 隐藏界面，仅在托盘显示
        }));
        ctrlPanel.Controls.Add(CreateHeaderBtn("⚙", (s, e) => ToggleSettings(), Color.FromArgb(100, 100, 110)));

        masterLayout.Controls.Add(new Panel { Size = new Size(420, 15), Margin = new Padding(0), BackColor = Color.Transparent });

        // 3. 入口卡片
        AddEntryCard(masterLayout, "🎮 H5 游戏大厅", "内置 Poki 海量精品游戏", (s, e) => {
             BrowserForm.Instance.Opacity = SettingsManager.Current.DefaultOpacity;
             BrowserForm.Instance.Navigate("https://poki.com/zh");
        });

        AddEntryCard(masterLayout, "🌐 极速浏览器", "沉浸式办公多标签环境", (s, e) => {
             BrowserForm.Instance.Opacity = SettingsManager.Current.DefaultOpacity;
             BrowserForm.Instance.Navigate(SettingsManager.Current.HomeUrl);
        });

        AddEntryCard(masterLayout, "🏆 星核防线", "宇宙级挂机塔防防御站", (s, e) => {
            if (_gameInstance == null || _gameInstance.IsDisposed) _gameInstance = new BattleForm();
            this.Hide(); _gameInstance.Opacity = SettingsManager.Current.DefaultOpacity; 
            _gameInstance.Show(); _gameInstance.Focus();
        });

        masterLayout.Controls.Add(new Label { Size = new Size(420, 25), Text = "右键托盘图标可彻底退出", Font = new Font("Segoe UI", 7), ForeColor = Color.FromArgb(60, 60, 65), TextAlign = ContentAlignment.MiddleCenter });
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, VK_SPACE);
    }

    private void AddEntryCard(FlowLayoutPanel parent, string title, string desc, EventHandler click)
    {
        var card = new Panel { Size = new Size(380, 75), BackColor = Color.FromArgb(36, 36, 42), Cursor = Cursors.Hand, Margin = new Padding(20, 0, 20, 12) };
        card.Paint += (s, e) => {
             using var pen = new Pen(Color.FromArgb(50, 50, 60), 1);
             e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        var lblTitle = new Label { Text = title, ForeColor = Color.White, Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold), Location = new Point(15, 12), AutoSize = true, Enabled = false };
        var lblDesc = new Label { Text = desc, ForeColor = Color.Gray, Font = new Font("Microsoft YaHei UI", 8), Location = new Point(16, 40), AutoSize = true, Enabled = false };
        card.Controls.AddRange(new Control[] { lblTitle, lblDesc });
        card.Click += click;
        card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(45, 45, 55);
        card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(36, 36, 42);
        parent.Controls.Add(card);
    }

    private Button CreateHeaderBtn(string text, EventHandler click, Color? foreColor = null)
    {
        var btn = new Button { Text = text, Size = new Size(32, 28), FlatStyle = FlatStyle.Flat, ForeColor = foreColor ?? Color.Gray, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(2) };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 45, 52);
        btn.Click += click;
        return btn;
    }

    private void InitializeSettingsPanel()
    {
        _settingsPanel = new Panel { Size = new Size(this.Width, 310), Location = new Point(0, this.Height), BackColor = Color.FromArgb(34, 34, 40), BorderStyle = BorderStyle.FixedSingle };
        this.Controls.Add(_settingsPanel);
        _settingsPanel.BringToFront();
        var lbl = new Label { Text = "系统配置", ForeColor = Color.White, Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold), Location = new Point(25, 20), AutoSize = true };
        _settingsPanel.Controls.Add(lbl);
        var trackOp = new TrackBar { Minimum = 1, Maximum = 10, Value = (int)(SettingsManager.Current.DefaultOpacity * 10), Location = new Point(25, 80), Size = new Size(320, 45), TickStyle = TickStyle.None };
        trackOp.Scroll += (s, e) => { SettingsManager.Current.DefaultOpacity = trackOp.Value / 10.0; this.Opacity = SettingsManager.Current.DefaultOpacity; };
        _settingsPanel.Controls.Add(trackOp);
        var btnSave = new Button { Text = "应用并关闭", Location = new Point(25, 210), Size = new Size(110, 40), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(0, 120, 215), Cursor = Cursors.Hand };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => { SettingsManager.Save(); ToggleSettings(); };
        _settingsPanel.Controls.Add(btnSave);
    }

    private void ToggleSettings()
    {
        bool isShowing = _settingsPanel.Top < this.Height;
        var timer = new System.Windows.Forms.Timer { Interval = 1 };
        timer.Tick += (s, e) => {
            if (isShowing) { 
                _settingsPanel.Top += 15; 
                if (_settingsPanel.Top >= this.Height) { _settingsPanel.Top = this.Height; timer.Stop(); timer.Dispose(); } 
            } else { 
                _settingsPanel.Top -= 15;
                if (_settingsPanel.Top <= this.Height - 310) { _settingsPanel.Top = this.Height - 310; _settingsPanel.BringToFront(); timer.Stop(); timer.Dispose(); } 
            }
        };
        timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        using var pen = new Pen(Color.FromArgb(60, 60, 75), 1);
        e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
    }

    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    private void TitlePanel_MouseDown(object? sender, MouseEventArgs e) {
        if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); }
    }

    protected override void WndProc(ref Message m)
    {
        // 1. 处理全局热键 (Alt + Space) - 无论焦点在哪都生效
        if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID) {
            ToggleBossVisibility();
            return;
        }

        // 2. 拦截系统菜单快捷键 (Alt + Space) - 防止焦点在程序内时弹出菜单
        if (m.Msg == 0x0112 && ((int)m.WParam & 0xFFF0) == 0xF100) { 
            ToggleBossVisibility();
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    private void ToggleBossVisibility() {
        bool isAnyVisible = this.Opacity > 0.1 || 
                           (BrowserForm.Instance != null && BrowserForm.Instance.Visible && BrowserForm.Instance.Opacity > 0.1) ||
                           (_gameInstance != null && _gameInstance.Visible && _gameInstance.Opacity > 0.1);

        if (isAnyVisible) { 
            // 隐蔽一切并记忆现场
            _wasBrowserVisible = BrowserForm.Instance.Visible;
            _wasGameVisible = (_gameInstance != null && _gameInstance.Visible);
            
            this.Tag = this.Opacity;
            this.Opacity = 0.0;
            this.ShowInTaskbar = false;
            if (BrowserForm.Instance.Visible) BrowserForm.Instance.Hide(); 
            if (_gameInstance != null && _gameInstance.Visible) _gameInstance.Hide();
        } else { 
            // 回归现场
            this.Opacity = (this.Tag is double op && op > 0.1) ? op : SettingsManager.Current.DefaultOpacity; 
            this.ShowInTaskbar = true; 
            
            if (_wasGameVisible && _gameInstance != null) {
                this.Hide(); _gameInstance.Show(); _gameInstance.Opacity = this.Opacity;
                _gameInstance.BringToFront(); SetForegroundWindow(_gameInstance.Handle);
            } else if (_wasBrowserVisible) {
                this.Hide(); BrowserForm.Instance.Show(); BrowserForm.Instance.Opacity = this.Opacity;
                BrowserForm.Instance.BringToFront(); SetForegroundWindow(BrowserForm.Instance.Handle);
            } else {
                this.Show(); this.BringToFront(); this.Activate(); SetForegroundWindow(this.Handle);
            }
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData & Keys.Alt) == Keys.Alt) {
            Keys baseKey = keyData & ~Keys.Alt;
            if (baseKey == Keys.Up) { this.Opacity = Math.Min(1.0, this.Opacity + 0.1); return true; }
            if (baseKey == Keys.Down) { this.Opacity = Math.Max(0.1, this.Opacity - 0.1); return true; }
            if (baseKey == Keys.Space) {
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
        // 确保退出时释放图标
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.OnFormClosing(e);
    }
}
