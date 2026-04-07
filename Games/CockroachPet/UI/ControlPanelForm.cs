using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PureBattleGame.Games.CockroachPet;

public class ControlPanelForm : Form
{
    private PetForm _mainForm;
    private ListView _robotListView;
    private System.Windows.Forms.Timer _updateTimer;

    public ControlPanelForm(PetForm mainForm)
    {
        _mainForm = mainForm;
        InitializeComponent();
        InitializeTimer();
    }

    private void InitializeComponent()
    {
        this.Text = "Robot Pet Control Panel";
        this.Size = new Size(700, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.MinimumSize = new Size(600, 400);

        // 标题
        var titleLabel = new Label
        {
            Text = "Pixel Robot Pet - Control Panel",
            Dock = DockStyle.Top,
            Height = 50,
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            ForeColor = Color.Lime,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        // 机器人列表
        _robotListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };

        _robotListView.Columns.Add("ID", 40);
        _robotListView.Columns.Add("名称", 100);
        _robotListView.Columns.Add("个性", 80);
        _robotListView.Columns.Add("状态", 80);
        _robotListView.Columns.Add("意识", 70);
        _robotListView.Columns.Add("经验", 70);
        _robotListView.Columns.Add("位置", 100);
        _robotListView.Columns.Add("速度", 60);
        _robotListView.Columns.Add("大小", 50);
        _robotListView.Columns.Add("显示", 60);

        _robotListView.MouseDoubleClick += RobotListView_MouseDoubleClick;

        // 统计面板
        var statsPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            BackColor = Color.FromArgb(35, 35, 35),
            Padding = new Padding(10, 5, 10, 5)
        };

        var statsLabel = new Label
        {
            Name = "statsLabel",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft
        };
        statsPanel.Controls.Add(statsLabel);

        // 底部按钮面板
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 100,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.FromArgb(40, 40, 40),
            Padding = new Padding(10),
            WrapContents = true
        };

        var btnSpawn = CreateButton("投放机器人", Color.Lime);
        btnSpawn.Click += (s, e) => _mainForm.SpawnRobotWithName();

        var btnQuickSpawn = CreateButton("快速投放", Color.Cyan);
        btnQuickSpawn.Click += (s, e) =>
        {
            string[] names = { "小八", "阿呆", "像素仔", "蓝灵", "红豆", "大眼", "触手大王", "碳基生物" };
            _mainForm.SpawnRobot(names[new Random().Next(names.Length)], -1, -1);
        };

        var btnPauseAll = CreateButton("全部暂停", Color.Yellow);
        btnPauseAll.Click += (s, e) =>
        {
            foreach (var r in _mainForm.GetRobots()) r.IsMoving = false;
        };

        var btnResumeAll = CreateButton("全部启动", Color.Lime);
        btnResumeAll.Click += (s, e) =>
        {
            foreach (var r in _mainForm.GetRobots()) r.IsMoving = true;
        };

        var btnClearAll = CreateButton("清除全部", Color.Red);
        btnClearAll.Click += (s, e) =>
        {
            if (MessageBox.Show("确定要清除所有机器人吗？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _mainForm.ClearAllRobots();
                UpdateRobotList();
            }
        };

        var btnSettings = CreateButton("设置", Color.Orange);
        btnSettings.Click += (s, e) => _mainForm.ShowSettings();

        var btnEnvCheck = CreateButton("环境检测", Color.Cyan);
        btnEnvCheck.Click += (s, e) => ShowEnvironmentCheck();

        buttonPanel.Controls.Add(btnSpawn);
        buttonPanel.Controls.Add(btnQuickSpawn);
        buttonPanel.Controls.Add(btnPauseAll);
        buttonPanel.Controls.Add(btnResumeAll);
        buttonPanel.Controls.Add(btnClearAll);
        buttonPanel.Controls.Add(btnSettings);
        buttonPanel.Controls.Add(btnEnvCheck);

        // 信息标签
        var infoLabel = new Label
        {
            Text = "双击机器人打开/显示终端 | 右键查看更多操作",
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        this.Controls.Add(_robotListView);
        this.Controls.Add(buttonPanel);
        this.Controls.Add(statsPanel);
        this.Controls.Add(infoLabel);
        this.Controls.Add(titleLabel);

        // 右键菜单
        var contextMenu = new ContextMenuStrip();
        contextMenu.Opening += (s, e) =>
        {
            contextMenu.Items.Clear();
            if (_robotListView.SelectedItems.Count > 0)
            {
                var robot = _robotListView.SelectedItems[0].Tag as Robot;
                if (robot != null)
                {
                    contextMenu.Items.Add("📺 打开聊天室", null, (s2, e2) => robot.OpenTerminal());
                    contextMenu.Items.Add("✏️ 编辑机器人", null, (s2, e2) => ShowEditDialog(robot));
                    contextMenu.Items.Add(new ToolStripSeparator());
                    
                    var status = robot.IsMoving ? "⏸ 暂停" : "▶ 启动";
                    contextMenu.Items.Add(status, null, (s2, e2) => robot.IsMoving = !robot.IsMoving);
                    
                    var visibility = robot.IsVisible ? "👻 隐藏" : "👁️ 显示";
                    contextMenu.Items.Add(visibility, null, (s2, e2) => robot.IsVisible = !robot.IsVisible);

                    contextMenu.Items.Add(new ToolStripSeparator());
                    contextMenu.Items.Add("❌ 删除该机器人", null, (s2, e2) => {
                        if (MessageBox.Show($"确定要删除 {robot.Name} 吗？", "警告", 
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                            _mainForm.RemoveRobot(robot);
                        }
                    });
                }
            }
        };
        _robotListView.ContextMenuStrip = contextMenu;
    }

    private Button CreateButton(string text, Color color)
    {
        return new Button
        {
            Text = text,
            Width = 100,
            Height = 35,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Margin = new Padding(5)
        };
    }

    private void InitializeTimer()
    {
        _updateTimer = new System.Windows.Forms.Timer();
        _updateTimer.Interval = 500;
        _updateTimer.Tick += (s, e) => UpdateRobotList();
        _updateTimer.Start();
    }

    private void UpdateRobotList()
    {
        _robotListView.Items.Clear();
        var robots = _mainForm.GetRobots();
        int movingCount = 0;
        int pausedCount = 0;
        float avgSpeed = 0;

        foreach (var robot in robots)
        {
            var item = new ListViewItem(robot.Id.ToString());
            item.SubItems.Add(robot.Name);
            item.SubItems.Add(robot.Personality);
            item.SubItems.Add(robot.IsMoving ? "▶ 移动" : "⏸ 暂停");
            item.SubItems.Add($"Lvl {robot.ConsciousnessLevel:F1}");
            item.SubItems.Add($"{robot.Experience}/100");
            item.SubItems.Add($"({robot.X:F0}, {robot.Y:F0})");
            item.SubItems.Add($"{robot.SpeedMultiplier:F1}x");
            item.SubItems.Add($"{robot.Size}px");
            item.SubItems.Add(robot.IsVisible ? "👁️" : "👻");
            item.Tag = robot;
            _robotListView.Items.Add(item);

            // 统计
            if (robot.IsMoving) movingCount++;
            else pausedCount++;
            avgSpeed += robot.SpeedMultiplier;
        }

        // 更新统计面板
        var statsLabel = this.Controls.Find("statsLabel", true).FirstOrDefault() as Label;
        if (statsLabel != null && robots.Count > 0)
        {
            avgSpeed /= robots.Count;
            statsLabel.Text = $"总数: {robots.Count} | 移动中: {movingCount} | 已暂停: {pausedCount} | 平均速度: {avgSpeed:F1}x | 🪙 Token 使用: {AiService.TotalTokensUsed:N0}";
        }
        else if (statsLabel != null)
        {
            statsLabel.Text = "暂无机器人";
        }
    }

    private void ShowEnvironmentCheck()
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine("=== 环境检测结果 ===");
        result.AppendLine();

        // 检测 CLI 工具
        var tools = new[] {
            ("Claude", "claude"),
            ("OpenClaw", "openClaw"),
            ("OpenCode", "opencode"),
            ("Gemini CLI", "gemincli"),
            ("VS Code", "code"),
            ("VS Code Insiders", "code-insiders"),
            ("Cursor", "cursor"),
            ("Windsurf", "windsurf"),
            ("Node.js", "node"),
            ("Python", "python"),
            ("Git", "git"),
            ("Docker", "docker")
        };

        foreach (var (name, cmd) in tools)
        {
            bool exists = CheckCommandExists(cmd);
            result.AppendLine($"{name}: {(exists ? "✓ 已安装" : "✗ 未安装")}");
        }

        result.AppendLine();
        result.AppendLine("=== 嵌入终端说明 ===");
        result.AppendLine("Claude/OpenClaw/Codex 等 AI 工具需要交互式控制台，");
        result.AppendLine("当前版本会在独立窗口中启动。");
        result.AppendLine("如需完全嵌入，需要使用 Windows Terminal 或 ConPTY API。");

        MessageBox.Show(result.ToString(), "环境检测", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private bool CheckCommandExists(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;
            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void RobotListView_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (_robotListView.SelectedItems.Count > 0)
        {
            var robot = _robotListView.SelectedItems[0].Tag as Robot;
            if (robot != null) ShowEditDialog(robot);
        }
    }

    private void ShowEditDialog(Robot robot)
    {
        using var dialog = new Form
        {
            Text = $"编辑机器人: {robot.Name} (ID: {robot.Id})",
            Size = new Size(440, 640),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(20) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

        void AddRow(string labelText, Control input)
        {
            layout.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight });
            input.Dock = DockStyle.Fill;
            layout.Controls.Add(input);
        }

        var nameInput = new TextBox { Text = robot.Name };
        var personalityInput = new TextBox { Text = robot.Personality };
        var sizeInput = new NumericUpDown { Minimum = 32, Maximum = 256, Value = Math.Clamp(robot.Size, 32, 256) };
        var speedInput = new NumericUpDown { Minimum = 0, Maximum = 500, Value = Math.Clamp((decimal)(robot.SpeedMultiplier * 100), 0, 500) };
        var levelInput = new NumericUpDown { Minimum = 1, Maximum = 100, Value = Math.Clamp((decimal)robot.ConsciousnessLevel, 1, 100), DecimalPlaces = 1 };
        var guidelineInput = new TextBox { Text = robot.InternalGuidelines, Multiline = true, Height = 40 };


        var phraseInput = new TextBox { 
            Text = string.Join(Environment.NewLine, robot.CustomPhrases), 
            Multiline = true, 
            Height = 80,
            ScrollBars = ScrollBars.Vertical
        };

        AddRow("名称:", nameInput);
        AddRow("个性:", personalityInput);
        AddRow("大小 (px):", sizeInput);
        AddRow("速度 (%):", speedInput);
        AddRow("意识等级:", levelInput);
        AddRow("行为准则:", guidelineInput);

        // 自定义台词行 (带导入按钮)
        var phraseLabelContainer = new FlowLayoutPanel { 
            FlowDirection = FlowDirection.TopDown, 
            Dock = DockStyle.Fill, 
            Margin = new Padding(0),
            AutoSize = true
        };
        phraseLabelContainer.Controls.Add(new Label { 
            Text = "自定义台词\n(每行一条):", 
            AutoSize = true, 
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Right
        });


        
        layout.Controls.Add(phraseLabelContainer);
        phraseInput.Dock = DockStyle.Fill;
        layout.Controls.Add(phraseInput);

        var btnOk = new Button { Text = "保存修改", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.Lime, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 10, FontStyle.Bold) };
        btnOk.Click += (s, e) => {
            robot.Name = nameInput.Text;
            robot.Personality = personalityInput.Text;
            robot.Size = (int)sizeInput.Value;
            robot.SpeedMultiplier = (float)speedInput.Value / 100f;
            robot.ConsciousnessLevel = (double)levelInput.Value;
            robot.InternalGuidelines = guidelineInput.Text;


            // 解析台词
            robot.CustomPhrases = phraseInput.Text
                .Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
            
            robot.SaveSkills(); // 触发保存
            PersistenceManager.SaveRobots(_mainForm.GetRobots());
            dialog.DialogResult = DialogResult.OK;
        };

        dialog.Controls.Add(layout);
        dialog.Controls.Add(btnOk);
        dialog.ShowDialog();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        base.OnFormClosing(e);
    }
}
