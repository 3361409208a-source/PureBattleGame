using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PureBattleGame.Core;

public partial class BrowserForm : Form
{
    private static BrowserForm? _instance;
    public static BrowserForm Instance => _instance ??= new BrowserForm();

    private Panel _headerPanel = null!;
    private Panel _contentContainer = null!;
    private FlowLayoutPanel _tabList = null!;
    private TextBox _addressBar = null!;
    
    private class TabData {
        public Panel TabPanel;
        public WebView2 WebView;
        public Panel HeaderBtn;
        public string Title = "新标签页";
    }
    private List<TabData> _tabs = new List<TabData>();
    private TabData? _activeTab;

    private BrowserForm()
    {
        InitializeComponent();
        this.DoubleBuffered = true;
        this.Opacity = SettingsManager.Current.DefaultOpacity;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_tabs.Count == 0) CreateNewTab(SettingsManager.Current.HomeUrl);
    }

    private void InitializeComponent()
    {
        this.Text = "极速搜索";
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30,30,34);
        this.FormBorderStyle = FormBorderStyle.None;
        this.Padding = new Padding(0);
        this.MinimumSize = new Size(600, 400);

        // 使用 TableLayoutPanel 加固布局防止重叠或溢出
        var mainTable = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // 顶栏高度 45px
        mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 内容区填充剩余空间
        this.Controls.Add(mainTable);

        // 1. 顶部集成工具栏 (Row 0)
        _headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 22, 26), Margin = new Padding(0) };
        _headerPanel.MouseDown += Window_MouseDown;
        mainTable.Controls.Add(_headerPanel, 0, 0);

        _headerPanel.Controls.Add(CreateIconButton("◀", 5, 6, (s, e) => _activeTab?.WebView.GoBack()));
        _headerPanel.Controls.Add(CreateIconButton("▶", 35, 6, (s, e) => _activeTab?.WebView.GoForward()));
        _headerPanel.Controls.Add(CreateIconButton("🏠", 65, 6, (s, e) => _activeTab?.WebView.CoreWebView2?.Navigate(SettingsManager.Current.HomeUrl)));

        _tabList = new FlowLayoutPanel { Location = new Point(100, 0), Size = new Size(this.Width / 2 - 100, 45), BackColor = Color.Transparent, WrapContents = false, Padding = new Padding(0) };
        _tabList.MouseDown += Window_MouseDown;
        _headerPanel.Controls.Add(_tabList);

        _addressBar = new TextBox {
            Location = new Point(this.Width / 2 + 5, 10),
            Size = new Size(this.Width / 2 - 85, 24),
            BackColor = Color.FromArgb(45, 45, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 11),
            Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left
        };
        _addressBar.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                string url = _addressBar.Text.Trim();
                if (string.IsNullOrEmpty(url)) return;
                if (!url.StartsWith("http") && !url.Contains("://")) url = "https://" + url;
                _activeTab?.WebView.CoreWebView2?.Navigate(url);
                e.Handled = true; e.SuppressKeyPress = true;
            }
        };
        _headerPanel.Controls.Add(_addressBar);

        var btnNewTab = CreateIconButton("➕", 0, 6, (s, e) => CreateNewTab(SettingsManager.Current.HomeUrl));
        btnNewTab.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        var btnCloseWindow = CreateIconButton("✕", 35, 6, (s, e) => this.Hide());
        btnCloseWindow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCloseWindow.ForeColor = Color.IndianRed;
        
        var btnGroupRight = new Panel { Dock = DockStyle.Right, Width = 80, BackColor = Color.Transparent };
        btnGroupRight.Controls.AddRange(new Control[] { btnNewTab, btnCloseWindow });
        btnGroupRight.MouseDown += Window_MouseDown;
        _headerPanel.Controls.Add(btnGroupRight);

        // 2. 核心内容容器 (Row 1)
        _contentContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 34), Margin = new Padding(0), Padding = new Padding(1) };
        mainTable.Controls.Add(_contentContainer, 0, 1);

        // 3. 窗口拉伸支持
        var resizer = new Label { Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Size = new Size(12, 12), Cursor = Cursors.SizeNWSE, BackColor = Color.Transparent, Location = new Point(this.Width - 12, this.Height - 12) };
        resizer.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)17, IntPtr.Zero); } };
        this.Controls.Add(resizer);
        resizer.BringToFront();
    }

    private Button CreateIconButton(string text, int x, int y, EventHandler click)
    {
        var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(30, 32), FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Font = new Font("Segoe UI Emoji", 9), Cursor = Cursors.Hand };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 55);
        btn.Click += click;
        return btn;
    }

    private bool _isCreatingTab = false; // 防抖锁

    public async void CreateNewTab(string url)
    {
        if (_isCreatingTab) return;
        _isCreatingTab = true;

        try {
            var wv = new WebView2 { 
            Dock = DockStyle.Fill, 
            DefaultBackgroundColor = Color.FromArgb(30, 30, 34) 
        };
        
        var tabPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 34) };
        tabPanel.Controls.Add(wv);
        _contentContainer.Controls.Add(tabPanel);

        var tabBtn = new Panel { Size = new Size(140, 45), BackColor = Color.Transparent, Cursor = Cursors.Hand };
        var tabTitle = new Label { Text = "加载中...", Location = new Point(10, 12), AutoSize = false, Size = new Size(100, 20), ForeColor = Color.Gray, Enabled = false };
        var closeBtn = new Label { Text = "✕", Location = new Point(115, 12), Size = new Size(20, 20), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(80,80,80) };

        var tabData = new TabData { TabPanel = tabPanel, WebView = wv, HeaderBtn = tabBtn };
        _tabs.Add(tabData);
        tabBtn.Tag = tabData;

        tabBtn.Controls.AddRange(new Control[] { tabTitle, closeBtn });
        tabBtn.Click += (s, e) => SwitchToTab(tabData);
        closeBtn.Click += (s, e) => CloseTab(tabData);
        _tabList.Controls.Add(tabBtn);

        SwitchToTab(tabData);

        // 修正 0x8007139F: 指定独立的用户数据文件夹避免冲突 (必须在访问 CoreWebView2 之前调用)
        string userDataPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PureBattleGame_Browser_Data");
        var env = await CoreWebView2Environment.CreateAsync(null, userDataPath);
        await wv.EnsureCoreWebView2Async(env);

        wv.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
        wv.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
        
        // 核心：处理新窗口请求，重定向至内部新标签页 (满足“不跳转单独窗口”需求)
        wv.CoreWebView2.NewWindowRequested += (s, e) => {
             e.Handled = true; // 拦截原生弹出窗口
             CreateNewTab(e.Uri); // 在本应用中新建标签页打开
        };

        wv.CoreWebView2.NavigationCompleted += (s, e) => {
             wv.ExecuteScriptAsync("document.body.style.overflow = 'hidden';");
        };
        wv.CoreWebView2.SourceChanged += (s, e) => { if (tabData == _activeTab) _addressBar.Text = wv.Source.ToString(); };
        wv.CoreWebView2.DocumentTitleChanged += (s, e) => { tabData.Title = wv.CoreWebView2.DocumentTitle; tabTitle.Text = tabData.Title; };
        
        // 注入脚本以捕获网页内部的 Alt 快捷键 (改用 e.code 获取更可靠的物理键值)
        await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
            window.addEventListener('keydown', (e) => {
                if (e.altKey) {
                    window.chrome.webview.postMessage({ type: 'shortcut', code: e.code, shift: e.shiftKey });
                }
            });
        ");

        wv.CoreWebView2.WebMessageReceived += (s, e) => {
            try {
                var json = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson).RootElement;
                if (json.GetProperty("type").GetString() == "shortcut") {
                    string code = json.GetProperty("code").GetString() ?? "";
                    bool shift = json.GetProperty("shift").GetBoolean();
                    Keys key = Keys.None;
                    if (code == "ArrowUp") key = Keys.Up;
                    else if (code == "ArrowDown") key = Keys.Down;
                    else if (code == "Space") key = Keys.Space;
                    else if (code.StartsWith("Key")) Enum.TryParse(code.Substring(3), true, out key);
                    else Enum.TryParse(code, true, out key);
                    
                    if (key != Keys.None) {
                        if (shift) key |= Keys.Shift;
                        this.BeginInvoke(new Action(() => HandleGlobalShortcuts(key | Keys.Alt)));
                    }
                }
            } catch {}
        };

        wv.CoreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建标签页失败: {ex.Message}");
        }
        finally
        {
            _isCreatingTab = false;
        }
    }

    private void SwitchToTab(TabData tab)
    {
        _activeTab = tab;
        tab.TabPanel.BringToFront();
        foreach (var t in _tabs) {
            t.HeaderBtn.BackColor = (t == tab) ? Color.FromArgb(40, 40, 45) : Color.Transparent;
            t.HeaderBtn.Controls[0].ForeColor = (t == tab) ? Color.White : Color.Gray;
        }
        if (this.IsHandleCreated) {
            this.BeginInvoke(new Action(() => {
                if (!tab.WebView.IsDisposed) tab.WebView.Focus();
            }));
        } else {
            tab.WebView.Focus();
        }
    }

    private void CloseTab(TabData tab)
    {
        if (_tabs.Count <= 1) { this.Hide(); return; }
        _tabs.Remove(tab);
        _tabList.Controls.Remove(tab.HeaderBtn);
        _contentContainer.Controls.Remove(tab.TabPanel);
        tab.WebView.Dispose();
        if (_activeTab == tab) SwitchToTab(_tabs.Last());
    }

    public void Navigate(string url)
    {
        if (!this.Visible) this.Show();
        this.BringToFront();

        bool shouldCreateNew = false;
        if (_activeTab != null && _activeTab.WebView.Source != null) {
            string currentUrl = _activeTab.WebView.Source.ToString();
            if (!currentUrl.Contains(SettingsManager.Current.HomeUrl) && !currentUrl.Contains("about:blank"))
                shouldCreateNew = true;
        }
        if (shouldCreateNew || _activeTab == null) CreateNewTab(url);
        else {
            if (!this.Visible) this.Show();
            this.BringToFront();
            _activeTab?.WebView.CoreWebView2?.Navigate(url);
            _activeTab?.WebView.Focus();
        }
    }

    public void ToggleVisibility() {
        if (this.Visible) this.Hide();
        else {
            this.Opacity = SettingsManager.Current.DefaultOpacity;
            this.Show();
            this.BringToFront();
        }
    }

    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    private void Window_MouseDown(object? sender, MouseEventArgs e) {
        if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (HandleGlobalShortcuts(keyData)) return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void WndProc(ref Message m)
    {
        // 核心：拦截系统菜单快捷键 (Alt+Space)
        if (m.Msg == 0x0112 && ((int)m.WParam & 0xFFF0) == 0xF100) { // WM_SYSCOMMAND & SC_KEYMENU
            if (HandleGlobalShortcuts(Keys.Space | Keys.Alt)) {
                m.Result = IntPtr.Zero;
                return;
            }
        }
        base.WndProc(ref m);
    }

    private bool HandleGlobalShortcuts(Keys keyData)
    {
        if ((keyData & Keys.Alt) == Keys.Alt) {
            Keys baseKey = keyData & ~Keys.Alt;
            if (baseKey == Keys.B || baseKey == Keys.Q) { this.Hide(); MoyuLauncher.Instance?.Show(); return true; }
            else if (baseKey == Keys.Space) { ToggleBossKey(); return true; }
            else if (baseKey == Keys.Up) { this.Opacity = Math.Min(1.0, this.Opacity + 0.1); return true; }
            else if (baseKey == Keys.Down) { this.Opacity = Math.Max(0.1, this.Opacity - 0.1); return true; }
            else if (baseKey == Keys.T) { CreateNewTab(SettingsManager.Current.HomeUrl); return true; }
            else if (baseKey == Keys.W) { if (_activeTab != null) CloseTab(_activeTab); return true; }
            
            if ((keyData & Keys.Shift) == Keys.Shift && baseKey == Keys.Q) {
                Environment.Exit(0);
                return true;
            }
        }
        return false;
    }

    private void ToggleBossKey() {
        if (this.Opacity > 0.0) { this.Tag = this.Opacity; this.Opacity = 0.0; this.ShowInTaskbar = false; }
        else { this.Opacity = (this.Tag is double op) ? op : SettingsManager.Current.DefaultOpacity; this.ShowInTaskbar = true; }
    }
}
