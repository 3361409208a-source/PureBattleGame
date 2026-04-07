using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PureBattleGame.Games.CockroachPet;

public class TerminalManagerForm : Form
{
    private TabControl _tabControl;
    private Dictionary<Robot, ChatTab> _terminals = new Dictionary<Robot, ChatTab>();
    private TabPage _worldChatPage;
    private TabPage _overviewPage;
    private FlowLayoutPanel _worldMessagePanel;
    private FlowLayoutPanel _overviewPanel;
    private static TerminalManagerForm? _instance;
    private System.Windows.Forms.Timer _titleUpdateTimer;

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
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "💬 机器人社交中心";
        this.Size = new Size(600, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10)
        };

        // 初始化世界频道
        _worldChatPage = new TabPage { Text = " 🌍 世界频道 ", BackColor = Color.FromArgb(25, 25, 25) };
        _worldMessagePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(15, 15, 15),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(10)
        };
        _worldChatPage.Controls.Add(_worldMessagePanel);

        // 初始化概览频道 (网格布局)
        _overviewPage = new TabPage { Text = " 🤖 机器人概览 ", BackColor = Color.FromArgb(25, 25, 25) };
        _overviewPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(20, 20, 20),
            Padding = new Padding(15),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        _overviewPage.Controls.Add(_overviewPanel);

        _tabControl.TabPages.Add(_overviewPage);
        _tabControl.TabPages.Add(_worldChatPage);

        this.Controls.Add(_tabControl);
        this.FormClosing += (s, e) => { 
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; 
                this.Hide(); 
            }
        };

        // 实时更新 Token 显示在标题栏
        _titleUpdateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _titleUpdateTimer.Tick += (s, e) => {
            this.Text = $"💬 机器人社交中心 | 🪙 Token: {AiService.TotalTokensUsed:N0}";
            UpdateOverviewIfVisible();
        };
        _titleUpdateTimer.Start();
    }

    private void UpdateOverviewIfVisible()
    {
        if (_tabControl.SelectedTab == _overviewPage)
        {
            SyncOverview();
        }
    }

    private void SyncOverview()
    {
        var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
        
        // 简单的差异更新：如果数量不一致或内容需要更新
        if (_overviewPanel.Controls.Count != robots.Count)
        {
            _overviewPanel.Controls.Clear();
            foreach (var robot in robots)
            {
                var card = CreateRobotCard(robot);
                _overviewPanel.Controls.Add(card);
            }
        }
        else
        {
            // 更新状态
            for (int i = 0; i < robots.Count; i++)
            {
                var card = _overviewPanel.Controls[i] as Panel;
                if (card != null && card.Tag is Robot r)
                {
                    var statusLabel = card.Controls.Find("status", true).FirstOrDefault() as Label;
                    if (statusLabel != null)
                    {
                        statusLabel.Text = r.StatusMessage;
                        statusLabel.ForeColor = r.IsAiSpeaking ? Color.Gold : Color.Gray;
                    }
                }
            }
        }
    }

    private Panel CreateRobotCard(Robot robot)
    {
        var card = new Panel
        {
            Size = new Size(130, 110),
            BackColor = Color.FromArgb(45, 45, 45),
            Margin = new Padding(5),
            Padding = new Padding(5),
            Tag = robot,
            Cursor = Cursors.Hand
        };

        var nameLabel = new Label
        {
            Text = robot.Name,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 30
        };

        var statusLabel = new Label
        {
            Name = "status",
            Text = robot.StatusMessage,
            ForeColor = Color.Gray,
            Font = new Font("Consolas", 8),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lvLabel = new Label
        {
            Text = $"Lvl {robot.ConsciousnessLevel:F1}",
            ForeColor = Color.Lime,
            Font = new Font("Microsoft YaHei", 8),
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 20
        };

        card.Controls.Add(statusLabel);
        card.Controls.Add(nameLabel);
        card.Controls.Add(lvLabel);

        card.Click += (s, e) => OpenTerminal(robot);
        card.DoubleClick += (s, e) => OpenTerminal(robot);
        foreach (Control c in card.Controls) 
        {
            c.Click += (s, e) => OpenTerminal(robot);
            c.DoubleClick += (s, e) => OpenTerminal(robot);
        }

        return card;
    }

    public void Shutdown()
    {
        // 彻底关闭自己，不触发隐藏逻辑
        this.FormClosing -= null; // 清除之前的隐藏逻辑
        this.Dispose();
    }

    public void BroadcastToWorld(string sender, string message, Color color)
    {
        if (_worldMessagePanel.InvokeRequired)
        {
            _worldMessagePanel.Invoke(new Action(() => BroadcastToWorld(sender, message, color)));
            return;
        }

        var logLabel = new Label
        {
            Text = $"[{DateTime.Now:HH:mm:ss}] {sender}: {message}",
            ForeColor = color,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 2, 0, 2),
            MaximumSize = new Size(_worldMessagePanel.Width - 30, 0)
        };
        
        _worldMessagePanel.Controls.Add(logLabel);
        _worldMessagePanel.ScrollControlIntoView(logLabel);
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
        _tabPage = new TabPage { Text = $"  {robot.Name}  ", BackColor = Color.FromArgb(30, 30, 30) };
        
        // 主布局
        var mainLayout = new TableLayoutPanel { 
            Dock = DockStyle.Fill, 
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        
        // 1. 成长面板
        var growthPanel = new Panel { 
            Dock = DockStyle.Fill, 
            BackColor = Color.FromArgb(45, 45, 45),
            Padding = new Padding(10)
        };
        
        _growthStatus = new Label {
            Text = $"意识等级: Lvl {robot.ConsciousnessLevel:F1} | XP: {robot.Experience}/100",
            ForeColor = Color.SpringGreen,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            Dock = DockStyle.Top,
            AutoSize = true
        };

        _skillPanel = new FlowLayoutPanel {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            AutoScroll = true
        };

        _insightPanel = new FlowLayoutPanel {
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

        // 2. 消息面板 (改为普通的 Panel 嵌套，避免 FlowLayoutPanel 宽度计算错误)
        _messagePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(20, 20, 20),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(10)
        };

        // 3. 输入框
        _inputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei", 11),
            Margin = new Padding(5)
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // Index 0: Growth
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Index 1: Messages
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));  // Index 2: Input

        mainLayout.Controls.Add(growthPanel, 0, 0);
        mainLayout.Controls.Add(_messagePanel, 0, 1);
        mainLayout.Controls.Add(_inputBox, 0, 2);

        _tabPage.Controls.Add(mainLayout);

        _messagePanel.SizeChanged += (s, e) => {
            foreach (Control c in _messagePanel.Controls) {
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
        
        // 加载历史（历史消息目前不带思考过程，仅显示回复）
        foreach(var msg in _robot.ChatHistory)
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
            var insightChip = new Label {
                Text = insight,
                AutoSize = true,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Microsoft YaHei", 8),
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 5, 0)
            };
            _insightPanel.Controls.Add(insightChip);
        }

        // 更新技能显示
        _skillPanel.Controls.Clear();
        foreach (var skill in _robot.Skills.Values)
        {
            var skillDisplay = new Label {
                Text = $"{skill.Name} Lvl.{skill.Level} ({skill.Experience}/{skill.NextLevelXp})",
                AutoSize = true,
                ForeColor = Color.SkyBlue,
                Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
                Padding = new Padding(10, 0, 15, 0),
                Margin = new Padding(0, 5, 0, 0)
            };
            _skillPanel.Controls.Add(skillDisplay);
        }
    }

    private void SendMessage()
    {
        string text = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        
        _inputBox.Clear();

        // 处理特殊命令
        if (text.StartsWith("/"))
        {
            HandleInternalCommand(text);
            return;
        }
        
        _robot.SendUserMessage(text).ConfigureAwait(false);
    }

    private void HandleInternalCommand(string cmd)
    {
        string log = "";
        switch (cmd.ToLower())
        {
            case "/log-social":
                _robot.LogSocialInteractions = !_robot.LogSocialInteractions;
                log = _robot.LogSocialInteractions ? "已开启社交日志" : "已隐藏社交日志";
                break;
            case "/status":
                log = $"状态: {_robot.StatusMessage} | 动作: {(_robot.IsMoving ? "移动中" : "静止")}";
                break;
            case "/help":
                log = "可用指令:\n/log-social - 切换社交对话显示\n/status - 查看状态\n/help - 显示此帮助";
                break;
            default:
                log = "未知指令。输入 /help 查看列表。";
                break;
        }
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
            ForeColor = text.Contains("[SOCIAL]") ? Color.LightBlue : Color.Gray,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Italic),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(10, 2, 0, 2),
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

        // 使用 TableLayoutPanel 代替 Panel，这是 WinForms 处理纵向堆叠最稳健的方式
        var msgContainer = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 0, // 动态增加
            Width = _messagePanel.ClientSize.Width - 30,
            AutoSize = true,
            BackColor = Color.FromArgb(35, 35, 35),
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 15)
        };
        msgContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // 1. 名字头部
        var header = new Label
        {
            Text = role == "user" ? " 我:" : $" {_robot.Name}:",
            ForeColor = role == "user" ? Color.Cyan : Color.Gold,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 5)
        };
        msgContainer.Controls.Add(header);

        // 2. 思考过程 (如果有)
        if (!string.IsNullOrEmpty(thought))
        {
            var thoughtLabel = new Label
            {
                Text = thought,
                ForeColor = Color.DarkGray,
                BackColor = Color.FromArgb(25, 25, 25),
                Font = new Font("Consolas", 9),
                AutoSize = true,
                Dock = DockStyle.Top,
                Visible = false, // 默认折叠
                Padding = new Padding(8),
                Margin = new Padding(10, 5, 0, 5)
            };

            var toggleBtn = new Label
            {
                Text = " 💭 思考过程 (点击展开)",
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei", 8, FontStyle.Italic),
                Cursor = Cursors.Hand,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 2, 0, 2)
            };

            toggleBtn.Click += (s, e) =>
            {
                thoughtLabel.Visible = !thoughtLabel.Visible;
                toggleBtn.Text = thoughtLabel.Visible ? " 💭 思考过程 (点击折叠)" : " 💭 思考过程 (点击展开)";
                // TableLayoutPanel 会因为 AutoSize 自动重绘
            };

            msgContainer.Controls.Add(toggleBtn);
            msgContainer.Controls.Add(thoughtLabel);
        }

        // 3. 正文内容
        var textBody = new Label
        {
            Text = content,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(5, 5, 0, 0)
        };
        msgContainer.Controls.Add(textBody);

        // 核心：强制让父容器下的所有 Label 在 TableLayoutPanel 里触发换行
        msgContainer.Paint += (s, e) => {
            if (header.Width != msgContainer.Width - 20) {
                header.MaximumSize = new Size(msgContainer.Width - 20, 0);
                textBody.MaximumSize = new Size(msgContainer.Width - 20, 0);
                // 思考内容也需要限制
                foreach (Control c in msgContainer.Controls) {
                    if (c is Label l && c != header && c != textBody)
                        l.MaximumSize = new Size(msgContainer.Width - 30, 0);
                }
            }
        };

        _messagePanel.Controls.Add(msgContainer);
        
        // 自动滚动
        _messagePanel.ScrollControlIntoView(msgContainer);
        _messagePanel.PerformLayout();
    }
}
