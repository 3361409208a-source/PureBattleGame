using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using PureBattleGame.Core;

namespace PureBattleGame.Games.CockroachPet;

public class TerminalManagerForm : Form
{
    private TabControl _tabControl = null!;
    private Dictionary<Robot, ChatTab> _terminals = new Dictionary<Robot, ChatTab>();
    private TabPage _worldChatPage = null!;
    private TabPage _overviewPage = null!;
    private FlowLayoutPanel _worldMessagePanel = null!;
    private FlowLayoutPanel _overviewPanel = null!;
    private TextBox _worldInputBox = null!;
    private TextBox _searchBox = null!;
    private Label _lblOnlinePill = null!;
    private Label _lblModePill = null!;
    private Label _lblTokenPill = null!;
    private ComboBox _comboPrivateChat = null!;
    private static TerminalManagerForm? _instance;
    private System.Windows.Forms.Timer _titleUpdateTimer = null!;
    private string _searchKeyword = "";

    public static TerminalManagerForm Instance
    {
        get
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new TerminalManagerForm();
            }
            return _instance;
        }
    }

    private TerminalManagerForm()
    {
        // 只开启不破坏背景绘制的双缓冲，去掉 UserPaint 防止窗口背景变透明
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        this.DoubleBuffered = true;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "💬 机器人社交中心 | Robot Social Hub";
        this.Size = new Size(800, 800);
        this.MinimumSize = new Size(720, 680);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(24, 25, 28);
        this.ForeColor = Color.White;
        this.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

        // 1. 顶部 Header 面板 (采用 FlowLayoutPanel 靠右响应式放置 Status Badges，防裁切)
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(17, 18, 20),
            Padding = new Padding(12, 10, 12, 10)
        };

        var titleLabel = new Label
        {
            Text = "💬 机器人社交中心",
            Font = new Font("Microsoft YaHei UI", 11.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 229, 255),
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var headerRightFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        _lblOnlinePill = CreatePillLabel("🟢 在线: 0", Color.FromArgb(0, 230, 118));
        _lblModePill = CreatePillLabel("⚔️ 模式: 近远交替", Color.FromArgb(0, 229, 255));
        _lblTokenPill = CreatePillLabel("🪙 Token: 0", Color.FromArgb(255, 215, 0));

        headerRightFlow.Controls.Add(_lblOnlinePill);
        headerRightFlow.Controls.Add(_lblModePill);
        headerRightFlow.Controls.Add(_lblTokenPill);

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(headerRightFlow);
        this.Controls.Add(headerPanel);

        // 2. 主 TabControl
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(0, 34),
            SizeMode = TabSizeMode.Normal,
            Padding = new Point(16, 6),
            BackColor = Color.FromArgb(24, 25, 28),
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold)
        };
        _tabControl.DrawItem += TabControl_DrawItem;

        // --- Tab 1: 🤖 机器人概览 ---
        _overviewPage = new TabPage { Text = "🤖 机器人概览", BackColor = Color.FromArgb(24, 25, 28) };
        var overviewLayout = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 25, 28) };

        // 概览页 FlowLayoutPanel 响应式工具栏 (防止硬编码坐标重叠与裁切)
        var overviewToolBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(30, 31, 35),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(10, 8, 10, 8)
        };

        _searchBox = new TextBox
        {
            Width = 135,
            BackColor = Color.FromArgb(45, 47, 52),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei UI", 9F),
            Margin = new Padding(0, 2, 8, 0)
        };
        _searchBox.PlaceholderText = "🔍 搜索名字或状态...";
        _searchBox.TextChanged += (s, e) =>
        {
            _searchKeyword = _searchBox.Text.Trim().ToLower();
            SyncOverview();
        };

        var btnAddRobot = CreateActionButton("➕ 添加", Color.FromArgb(0, 200, 83), 60);
        btnAddRobot.Margin = new Padding(0, 1, 6, 0);
        btnAddRobot.Click += (s, e) => PetForm.Instance?.SpawnRobotWithName();

        var btnAiGen = CreateActionButton("🤖 AI 生成", Color.FromArgb(156, 39, 176), 80);
        btnAiGen.Margin = new Padding(0, 1, 6, 0);
        btnAiGen.Click += (s, e) => PetForm.Instance?.ShowAiRobotGenerator();

        var btnCurseToggle = CreateActionButton("🤬 骂人开关", Color.FromArgb(244, 67, 54), 85);
        btnCurseToggle.Margin = new Padding(0, 1, 6, 0);
        btnCurseToggle.Click += (s, e) => PetForm.Instance?.ToggleCurseMode();

        var btnChaos = CreateActionButton("🥊 全员对决", Color.FromArgb(255, 152, 0), 85);
        btnChaos.Margin = new Padding(0, 1, 12, 0);
        btnChaos.Click += (s, e) =>
        {
            var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
            if (robots.Count >= 2)
            {
                var rand = new Random();
                var r1 = robots[rand.Next(robots.Count)];
                var r2 = robots.Where(r => r != r1).ElementAtOrDefault(rand.Next(robots.Count - 1));
                if (r1 != null && r2 != null)
                {
                    r1.InteractWith(r2);
                    BroadcastToWorld("系统广播", $"⚔️ 已强制触发对决：{r1.Name} VS {r2.Name}！", Color.Gold);
                }
            }
        };

        var lblPrivateTag = new Label
        {
            Text = "👤 单聊:",
            ForeColor = Color.FromArgb(0, 229, 255),
            Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 5, 4, 0)
        };

        _comboPrivateChat = new ComboBox
        {
            Width = 95,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 47, 52),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            Margin = new Padding(0, 1, 0, 0)
        };
        _comboPrivateChat.SelectedIndexChanged += (s, e) =>
        {
            if (_comboPrivateChat.SelectedItem is RobotItem item && item.RobotObj != null)
            {
                OpenTerminal(item.RobotObj);
            }
        };

        overviewToolBar.Controls.Add(_searchBox);
        overviewToolBar.Controls.Add(btnAddRobot);
        overviewToolBar.Controls.Add(btnAiGen);
        overviewToolBar.Controls.Add(btnCurseToggle);
        overviewToolBar.Controls.Add(btnChaos);
        overviewToolBar.Controls.Add(lblPrivateTag);
        overviewToolBar.Controls.Add(_comboPrivateChat);
        overviewLayout.Controls.Add(overviewToolBar);

        // 卡片网格 FlowLayoutPanel
        _overviewPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(24, 25, 28),
            Padding = new Padding(12),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        overviewLayout.Controls.Add(_overviewPanel);
        _overviewPanel.BringToFront();
        _overviewPage.Controls.Add(overviewLayout);

        // --- Tab 2: 🌍 世界广播频道 ---
        _worldChatPage = new TabPage { Text = "🌍 世界广播频道", BackColor = Color.FromArgb(24, 25, 28) };
        var worldLayout = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 25, 28) };

        // 世界频道顶部状态条
        var worldHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 35,
            BackColor = Color.FromArgb(30, 31, 35),
            Padding = new Padding(10, 6, 10, 6)
        };
        var worldTipLabel = new Label
        {
            Text = "📢 提示: 在此频道的发言将同步广播给所有在线机器人，引发全员响应与讨论",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Microsoft YaHei UI", 8.5F),
            Dock = DockStyle.Fill
        };
        var btnClearWorld = new Button
        {
            Text = "🧹 清屏",
            Size = new Size(60, 23),
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnClearWorld.FlatAppearance.BorderSize = 0;
        btnClearWorld.Click += (s, e) => _worldMessagePanel.Controls.Clear();

        worldHeader.Controls.Add(worldTipLabel);
        worldHeader.Controls.Add(btnClearWorld);
        worldLayout.Controls.Add(worldHeader);

        // 世界频道消息面板
        _worldMessagePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(18, 19, 21),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12)
        };
        worldLayout.Controls.Add(_worldMessagePanel);
        _worldMessagePanel.BringToFront();

        // 世界频道底部广播输入框栏
        var worldInputPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.FromArgb(30, 31, 35),
            Padding = new Padding(10, 8, 10, 8)
        };
        _worldInputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 47, 52),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei UI", 10.5F),
            PlaceholderText = "💬 喊话给全员机器人，广播全服消息..."
        };
        _worldInputBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendWorldBroadcastMessage();
                e.SuppressKeyPress = true;
            }
        };

        var btnWorldSend = new Button
        {
            Text = "📢 广播喊话",
            Dock = DockStyle.Right,
            Width = 110,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 229, 255),
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnWorldSend.FlatAppearance.BorderSize = 0;
        btnWorldSend.Click += (s, e) => SendWorldBroadcastMessage();

        worldInputPanel.Controls.Add(_worldInputBox);
        worldInputPanel.Controls.Add(btnWorldSend);
        worldLayout.Controls.Add(worldInputPanel);

        _worldChatPage.Controls.Add(worldLayout);

        _tabControl.TabPages.Add(_overviewPage);
        _tabControl.TabPages.Add(_worldChatPage);

        this.Controls.Add(_tabControl);
        headerPanel.BringToFront();

        this.FormClosing += (s, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        };

        // 实时刷新 Timer
        _titleUpdateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _titleUpdateTimer.Tick += (s, e) => UpdateUIState();
        _titleUpdateTimer.Start();
    }

    private Label CreatePillLabel(string text, Color borderColor)
    {
        return new Label
        {
            Text = text,
            ForeColor = borderColor,
            Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(4, 0, 4, 0),
            Padding = new Padding(8, 3, 8, 3),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(28, 30, 34)
        };
    }

    private Button CreateActionButton(string text, Color bg, int width)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var tabPage = _tabControl.TabPages[e.Index];
        bool isSelected = (_tabControl.SelectedIndex == e.Index);
        var tabRect = _tabControl.GetTabRect(e.Index);

        using var bgBrush = new SolidBrush(isSelected ? Color.FromArgb(35, 37, 42) : Color.FromArgb(20, 21, 24));
        e.Graphics.FillRectangle(bgBrush, tabRect);

        if (isSelected)
        {
            using var linePen = new Pen(Color.FromArgb(0, 229, 255), 3);
            e.Graphics.DrawLine(linePen, tabRect.Left, tabRect.Bottom - 2, tabRect.Right, tabRect.Bottom - 2);
        }

        TextRenderer.DrawText(
            e.Graphics,
            tabPage.Text,
            _tabControl.Font,
            tabRect,
            isSelected ? Color.FromArgb(0, 229, 255) : Color.FromArgb(170, 175, 185),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
        );
    }

    private void UpdateUIState()
    {
        var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
        _lblOnlinePill.Text = $"🟢 在线: {robots.Count} 个";
        _lblTokenPill.Text = $"🪙 Token: {AiService.TotalTokensUsed:N0}";

        string modeStr = PetForm.Instance?.GlobalCombatMode switch
        {
            CombatMode.MeleeOnly => "⚔️ 模式: 纯近战",
            CombatMode.RangedOnly => "⚔️ 模式: 纯远程",
            _ => "⚔️ 模式: 近远交替"
        };
        _lblModePill.Text = modeStr;

        if (_comboPrivateChat != null && _comboPrivateChat.Items.Count != robots.Count)
        {
            _comboPrivateChat.Items.Clear();
            foreach (var r in robots)
            {
                _comboPrivateChat.Items.Add(new RobotItem(r));
            }
        }

        if (_tabControl.SelectedTab == _overviewPage)
        {
            SyncOverview();
        }
    }

    private void SyncOverview()
    {
        var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
        var filteredRobots = string.IsNullOrWhiteSpace(_searchKeyword)
            ? robots
            : robots.Where(r => r.Name.ToLower().Contains(_searchKeyword) || r.StatusMessage.ToLower().Contains(_searchKeyword)).ToList();

        // 核心：仅在卡片数量变动或搜索条件变动时重建控件，彻底消除 Controls.Clear() 造成的每秒闪烁！
        if (_overviewPanel.Controls.Count != filteredRobots.Count || _overviewPanel.Tag as string != _searchKeyword)
        {
            _overviewPanel.SuspendLayout();
            _overviewPanel.Controls.Clear();
            foreach (var robot in filteredRobots)
            {
                var card = CreateRobotCard(robot);
                _overviewPanel.Controls.Add(card);
            }
            _overviewPanel.Tag = _searchKeyword;
            _overviewPanel.ResumeLayout(true);
        }
        else
        {
            // 数量一致时，仅原地平滑更新控件内容文本，零闪烁！
            _overviewPanel.SuspendLayout();
            for (int i = 0; i < filteredRobots.Count; i++)
            {
                var card = _overviewPanel.Controls[i] as Panel;
                var robot = filteredRobots[i];
                if (card != null && card.Tag == robot)
                {
                    UpdateRobotCardInPlace(card, robot);
                }
            }
            _overviewPanel.ResumeLayout(true);
        }
    }

    private void UpdateRobotCardInPlace(Panel card, Robot robot)
    {
        var statusLabel = card.Controls.Find("status", true).FirstOrDefault() as Label;
        if (statusLabel != null)
        {
            string newStatus = robot.IsAiSpeaking ? $"🤖 \"{robot.ChatText}\"" : (string.IsNullOrWhiteSpace(robot.StatusMessage) ? "🌱 准备就绪..." : $"💬 {robot.StatusMessage}");
            if (statusLabel.Text != newStatus)
            {
                statusLabel.Text = newStatus;
                statusLabel.ForeColor = robot.IsAiSpeaking ? Color.Gold : Color.FromArgb(160, 165, 175);
            }
        }

        var traitLabel = card.Controls.Find("trait", true).FirstOrDefault() as Label;
        if (traitLabel != null)
        {
            string newTrait = $"性格: {robot.GetPersonalityName()} | HP: {robot.HP}/{robot.MaxHP}";
            if (traitLabel.Text != newTrait) traitLabel.Text = newTrait;
        }

        var lvLabel = card.Controls.Find("lv", true).FirstOrDefault() as Label;
        if (lvLabel != null)
        {
            string newLv = $"Lvl {robot.ConsciousnessLevel:F1}";
            if (lvLabel.Text != newLv) lvLabel.Text = newLv;
        }
    }

    private Panel CreateRobotCard(Robot robot)
    {
        var card = new Panel
        {
            Size = new Size(225, 170),
            BackColor = Color.FromArgb(32, 34, 38),
            Margin = new Padding(8),
            Padding = new Padding(10),
            Tag = robot,
            Cursor = Cursors.Hand,
            BorderStyle = BorderStyle.FixedSingle
        };

        // 头部: 名字 + 意识等级 Pill
        var topLayout = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent };

        var nameLabel = new Label
        {
            Text = robot.Name,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            Location = new Point(5, 4),
            AutoSize = true
        };

        var lvLabel = new Label
        {
            Name = "lv",
            Text = $"Lvl {robot.ConsciousnessLevel:F1}",
            ForeColor = Color.FromArgb(0, 230, 118),
            Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold),
            Location = new Point(145, 5),
            AutoSize = true,
            BackColor = Color.FromArgb(20, 40, 30),
            Padding = new Padding(4, 2, 4, 2)
        };

        topLayout.Controls.Add(nameLabel);
        topLayout.Controls.Add(lvLabel);

        // 性格 Pill & HP/XP 进度
        var traitLabel = new Label
        {
            Name = "trait",
            Text = $"性格: {robot.GetPersonalityName()} | HP: {robot.HP}/{robot.MaxHP}",
            ForeColor = Color.FromArgb(180, 185, 195),
            Font = new Font("Microsoft YaHei UI", 8F),
            Dock = DockStyle.Top,
            Height = 22
        };

        // 状态信息消息
        var statusLabel = new Label
        {
            Name = "status",
            Text = robot.IsAiSpeaking ? $"🤖 \"{robot.ChatText}\"" : (string.IsNullOrWhiteSpace(robot.StatusMessage) ? "🌱 准备就绪..." : $"💬 {robot.StatusMessage}"),
            ForeColor = robot.IsAiSpeaking ? Color.Gold : Color.FromArgb(160, 165, 175),
            Font = new Font("Microsoft YaHei UI", 8.5F),
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 0, 4)
        };

        // 底部快捷动作按钮栏
        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.Transparent };

        var btnChat = CreateCardBtn("💬 对话", Color.FromArgb(0, 150, 136), 65);
        btnChat.Location = new Point(0, 0);
        btnChat.Click += (s, e) => OpenTerminal(robot);

        var btnThink = CreateCardBtn("⚡ 启发", Color.FromArgb(103, 58, 183), 65);
        btnThink.Location = new Point(70, 0);
        btnThink.Click += (s, e) => _ = robot.SendUserMessage("发表一下你现在的思考想法");

        var btnDuel = CreateCardBtn("🥊 对决", Color.FromArgb(230, 81, 0), 65);
        btnDuel.Location = new Point(140, 0);
        btnDuel.Click += (s, e) =>
        {
            var other = PetForm.Instance?.GetRobots().FirstOrDefault(r => r != robot && !r.IsDead);
            if (other != null) robot.InteractWith(other);
        };

        bottomBar.Controls.Add(btnChat);
        bottomBar.Controls.Add(btnThink);
        bottomBar.Controls.Add(btnDuel);

        card.Controls.Add(statusLabel);
        card.Controls.Add(traitLabel);
        card.Controls.Add(topLayout);
        card.Controls.Add(bottomBar);

        // 双击卡片直接开启私聊
        card.DoubleClick += (s, e) => OpenTerminal(robot);

        return card;
    }

    private Button CreateCardBtn(string text, Color bg, int width)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, 24),
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void SendWorldBroadcastMessage()
    {
        string msg = _worldInputBox.Text.Trim();
        if (string.IsNullOrEmpty(msg)) return;
        _worldInputBox.Clear();

        BroadcastToWorld("我 (管理员)", $"📢 {msg}", Color.Cyan);

        var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
        foreach (var r in robots)
        {
            if (r.IsActive && !r.IsDead)
            {
                r.SocialHistory.Add(new SocialMessage("管理员", msg));
                _ = r.SendUserMessage(msg);
            }
        }
    }

    public void Shutdown()
    {
        this.FormClosing -= null;
        this.Dispose();
    }

    public void BroadcastToWorld(string sender, string message, Color color)
    {
        if (_worldMessagePanel.InvokeRequired)
        {
            _worldMessagePanel.Invoke(new Action(() => BroadcastToWorld(sender, message, color)));
            return;
        }

        var itemContainer = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Width = Math.Max(300, _worldMessagePanel.ClientSize.Width - 30),
            AutoSize = true,
            BackColor = Color.FromArgb(28, 30, 34),
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 10)
        };
        itemContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var header = new Label
        {
            Text = $"[{DateTime.Now:HH:mm:ss}] {sender}",
            ForeColor = color,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 4)
        };

        var textBody = new Label
        {
            Text = message,
            ForeColor = Color.WhiteSmoke,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            AutoSize = true,
            Dock = DockStyle.Top,
            MaximumSize = new Size(_worldMessagePanel.ClientSize.Width - 50, 0)
        };

        itemContainer.Controls.Add(header, 0, 0);
        itemContainer.Controls.Add(textBody, 0, 1);

        _worldMessagePanel.Controls.Add(itemContainer);
        _worldMessagePanel.ScrollControlIntoView(itemContainer);

        // 保持最多 150 条记录
        if (_worldMessagePanel.Controls.Count > 150)
        {
            _worldMessagePanel.Controls.RemoveAt(0);
        }
    }

    public void ShowWorldChat()
    {
        _tabControl.SelectedTab = _worldChatPage;
        this.Show();
        this.Activate();
    }

    public void OpenTerminal(Robot robot)
    {
        if (_terminals.ContainsKey(robot))
        {
            _tabControl.SelectedTab = _terminals[robot].TabPage;
        }
        else
        {
            var tab = new ChatTab(robot);
            _terminals[robot] = tab;
            _tabControl.TabPages.Add(tab.TabPage);
            _tabControl.SelectedTab = tab.TabPage;
        }
        this.Show();
        this.Activate();
    }

    public void CloseTerminal(Robot robot)
    {
        if (_terminals.ContainsKey(robot))
        {
            var tab = _terminals[robot];
            _tabControl.TabPages.Remove(tab.TabPage);
            _terminals.Remove(robot);
        }
    }
}

public class ChatTab
{
    private Robot _robot;
    private TabPage _tabPage;
    private FlowLayoutPanel _messagePanel;
    private TextBox _inputBox;
    private Label _growthStatus;
    private FlowLayoutPanel _insightPanel;
    private FlowLayoutPanel _skillPanel;

    public TabPage TabPage => _tabPage;

    public ChatTab(Robot robot)
    {
        _robot = robot;
        _tabPage = new TabPage { Text = $"🤖 {robot.Name}", BackColor = Color.FromArgb(24, 25, 28) };

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.FromArgb(24, 25, 28)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. 顶部成长与状态面板
        var growthPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(32, 34, 38),
            Padding = new Padding(12)
        };

        _growthStatus = new Label
        {
            Text = $"意识等级: Lvl {robot.ConsciousnessLevel:F1} | XP: {robot.Experience}/100",
            ForeColor = Color.FromArgb(0, 230, 118),
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            Dock = DockStyle.Top,
            AutoSize = true
        };

        _skillPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            AutoScroll = true
        };

        _insightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 5, 0, 0)
        };

        growthPanel.Controls.Add(_insightPanel);
        growthPanel.Controls.Add(_skillPanel);
        growthPanel.Controls.Add(_growthStatus);

        // 2. 消息面板
        _messagePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(18, 19, 21),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12)
        };

        // 3. 底部快捷对话与输入栏
        var bottomContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 31, 35),
            Padding = new Padding(8)
        };

        var quickBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 30,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var quickPrompts = new[] {
            "👋 介绍一下你自己", "😄 讲个笑话听听", "🥊 去挑战最近的对手", "⚡ 显示你目前的技能列表"
        };
        foreach (var prompt in quickPrompts)
        {
            var btnChip = new Button
            {
                Text = prompt,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 47, 52),
                ForeColor = Color.FromArgb(0, 229, 255),
                Font = new Font("Microsoft YaHei UI", 8F),
                Margin = new Padding(0, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            btnChip.FlatAppearance.BorderSize = 0;
            btnChip.Click += (s, e) =>
            {
                _inputBox.Text = prompt.Substring(2).Trim();
                SendMessage();
            };
            quickBar.Controls.Add(btnChip);
        }

        var inputRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        _inputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 47, 52),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei UI", 10.5F),
            PlaceholderText = $"💬 输入消息与 {robot.Name} 专属对话..."
        };

        var btnSend = new Button
        {
            Text = "🚀 发送",
            Dock = DockStyle.Right,
            Width = 90,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 229, 255),
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSend.FlatAppearance.BorderSize = 0;
        btnSend.Click += (s, e) => SendMessage();

        inputRow.Controls.Add(_inputBox);
        inputRow.Controls.Add(btnSend);

        bottomContainer.Controls.Add(inputRow);
        bottomContainer.Controls.Add(quickBar);

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 105)); // Index 0: Growth
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Index 1: Messages
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));  // Index 2: Input & Presets

        mainLayout.Controls.Add(growthPanel, 0, 0);
        mainLayout.Controls.Add(_messagePanel, 0, 1);
        mainLayout.Controls.Add(bottomContainer, 0, 2);

        _tabPage.Controls.Add(mainLayout);

        _messagePanel.SizeChanged += (s, e) =>
        {
            foreach (Control c in _messagePanel.Controls)
            {
                if (c is TableLayoutPanel) c.Width = _messagePanel.ClientSize.Width - 25;
            }
        };

        _inputBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendMessage();
                e.SuppressKeyPress = true;
            }
        };

        _robot.OnChatMessageReceived += HandleChatMessage;
        _robot.OnTerminalOutput += HandleTerminalOutput;
        _robot.OnGrowthUpdated += (r) => UpdateGrowthUI();

        UpdateGrowthUI();

        foreach (var msg in _robot.ChatHistory)
        {
            HandleChatMessage(msg.role, msg.content, "");
        }
    }

    private void UpdateGrowthUI()
    {
        if (_tabPage.InvokeRequired)
        {
            _tabPage.Invoke(new Action(UpdateGrowthUI));
            return;
        }

        _growthStatus.Text = $"意识等级: Lvl {_robot.ConsciousnessLevel:F1} | XP: {_robot.Experience}/100\n行为准则: {_robot.InternalGuidelines}";
        _insightPanel.Controls.Clear();
        foreach (var insight in _robot.LearnedInsights)
        {
            var insightChip = new Label
            {
                Text = insight,
                AutoSize = true,
                BackColor = Color.FromArgb(50, 52, 58),
                ForeColor = Color.FromArgb(220, 225, 235),
                Font = new Font("Microsoft YaHei UI", 8F),
                Padding = new Padding(6, 3, 6, 3),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 6, 0)
            };
            _insightPanel.Controls.Add(insightChip);
        }

        _skillPanel.Controls.Clear();
        foreach (var skill in _robot.Skills.Values)
        {
            var skillDisplay = new Label
            {
                Text = $"⚔️ {skill.Name} Lvl.{skill.Level} ({skill.Experience}/{skill.NextLevelXp})",
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 229, 255),
                Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold),
                Padding = new Padding(8, 2, 12, 2),
                Margin = new Padding(0, 4, 0, 0)
            };
            _skillPanel.Controls.Add(skillDisplay);
        }
    }

    private void SendMessage()
    {
        string text = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _inputBox.Clear();

        if (text.StartsWith("/"))
        {
            HandleInternalCommand(text);
            return;
        }

        _robot.SendUserMessage(text).ConfigureAwait(false);
    }

    private void HandleInternalCommand(string cmd)
    {
        string log = cmd.ToLower() switch
        {
            "/log-social" => (_robot.LogSocialInteractions = !_robot.LogSocialInteractions) ? "已开启社交日志" : "已隐藏社交日志",
            "/status" => $"状态: {_robot.StatusMessage} | 动作: {(_robot.IsMoving ? "移动中" : "静止")}",
            "/help" => "可用指令:\n/log-social - 切换社交对话显示\n/status - 查看状态\n/help - 显示此帮助",
            _ => "未知指令。输入 /help 查看列表。"
        };
        HandleTerminalOutput(log);
    }

    private void HandleTerminalOutput(string text)
    {
        if (_messagePanel.InvokeRequired)
        {
            _messagePanel.Invoke(new Action(() => HandleTerminalOutput(text)));
            return;
        }

        var logLabel = new Label
        {
            Text = text.StartsWith("[") ? text : $"[SYS] {text}",
            ForeColor = text.Contains("[SOCIAL]") ? Color.FromArgb(128, 222, 234) : Color.Gray,
            Font = new Font("Consolas", 8.5F, FontStyle.Italic),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(8, 2, 0, 2),
            Margin = new Padding(0)
        };

        _messagePanel.Controls.Add(logLabel);
        _messagePanel.ScrollControlIntoView(logLabel);
    }

    private void HandleChatMessage(string role, string content, string thought)
    {
        if (_messagePanel.InvokeRequired)
        {
            _messagePanel.Invoke(new Action(() => HandleChatMessage(role, content, thought)));
            return;
        }

        var msgContainer = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 0,
            Width = _messagePanel.ClientSize.Width - 30,
            AutoSize = true,
            BackColor = role == "user" ? Color.FromArgb(20, 38, 48) : Color.FromArgb(32, 34, 38),
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 12)
        };
        msgContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var header = new Label
        {
            Text = role == "user" ? "👤 我 (User)" : $"🤖 {_robot.Name}:",
            ForeColor = role == "user" ? Color.FromArgb(0, 229, 255) : Color.FromArgb(255, 215, 0),
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 4)
        };
        msgContainer.Controls.Add(header);

        if (!string.IsNullOrEmpty(thought))
        {
            var thoughtLabel = new Label
            {
                Text = thought,
                ForeColor = Color.FromArgb(160, 165, 175),
                BackColor = Color.FromArgb(20, 21, 24),
                Font = new Font("Consolas", 8.5F),
                AutoSize = true,
                Dock = DockStyle.Top,
                Visible = false,
                Padding = new Padding(8),
                Margin = new Padding(5, 4, 0, 4)
            };

            var toggleBtn = new Label
            {
                Text = "💭 思考过程 (点击展开)",
                ForeColor = Color.FromArgb(140, 145, 155),
                Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Italic),
                Cursor = Cursors.Hand,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 2, 0, 2)
            };

            toggleBtn.Click += (s, e) =>
            {
                thoughtLabel.Visible = !thoughtLabel.Visible;
                toggleBtn.Text = thoughtLabel.Visible ? "💭 思考过程 (点击折叠)" : "💭 思考过程 (点击展开)";
            };

            msgContainer.Controls.Add(toggleBtn);
            msgContainer.Controls.Add(thoughtLabel);
        }

        var textBody = new Label
        {
            Text = content,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(2, 4, 0, 0)
        };
        msgContainer.Controls.Add(textBody);

        msgContainer.Paint += (s, e) =>
        {
            if (header.Width != msgContainer.Width - 20)
            {
                header.MaximumSize = new Size(msgContainer.Width - 20, 0);
                textBody.MaximumSize = new Size(msgContainer.Width - 20, 0);
                foreach (Control c in msgContainer.Controls)
                {
                    if (c is Label l && c != header && c != textBody)
                        l.MaximumSize = new Size(msgContainer.Width - 30, 0);
                }
            }
        };

        _messagePanel.Controls.Add(msgContainer);
        _messagePanel.ScrollControlIntoView(msgContainer);
        _messagePanel.PerformLayout();
    }
}

public class RobotItem
{
    public Robot RobotObj { get; set; }
    public RobotItem(Robot r) { RobotObj = r; }
    public override string ToString() => $"🤖 {RobotObj.Name}";
}
