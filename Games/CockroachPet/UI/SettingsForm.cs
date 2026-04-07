using System;
using System.Drawing;
using System.Windows.Forms;

namespace PureBattleGame.Games.CockroachPet;

public class SettingsForm : Form
{
    // 设置值
    public int RobotCount { get; set; } = 1;
    public bool ShowNamingDialog { get; set; } = true;
    public int RobotSize { get; set; } = 64;
    public int RobotSpeed { get; set; } = 100;
    public string RobotName { get; set; } = "Claude";
    public bool AutoStart { get; set; } = false;
    public bool EnableAiThinking { get; set; } = false;
    public int AiThoughtFrequency { get; set; } = 60; // 默认 60 秒
    public int FightFrequency { get; set; } = 15; // 默认 15% 几率打架
    public bool IsWeaponMaster { get; set; } = false; // 武器大师模式
    public string ApiKey { get; set; } = ""; // API Key
    public RobotPersonalityType DefaultPersonality { get; set; } = RobotPersonalityType.Friendly; // 默认个性


    private NumericUpDown _countInput;
    private NumericUpDown _sizeInput;
    private NumericUpDown _speedInput;
    private TextBox _nameInput;
    private CheckBox _namingCheck;
    private CheckBox _autoStartCheck;
    private CheckBox _enableAiThinkingCheck;
    private NumericUpDown _aiFrequencyInput;
    private NumericUpDown _fightFreqInput;
    private CheckBox _isWeaponMasterCheck;
    private TextBox _apiKeyInput;
    private Label _apiKeyStatusLabel;
    private ComboBox _personalityCombo;


    public SettingsForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "Robot Pet Settings";
        this.Size = new Size(500, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(40, 40, 40);
        this.ForeColor = Color.White;
        this.Font = new Font("Microsoft YaHei", 10);

        // 创建主容器
        var mainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(0),
            BackColor = Color.FromArgb(40, 40, 40)
        };
        mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 标题
        mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 内容区域
        mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // 按钮区域

        // 标题
        var titleLabel = new Label
        {
            Text = "⚙️ Robot Pet Settings",
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            ForeColor = Color.Lime,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // 创建内容面板并设置滚动
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(40, 40, 40)
        };

        var tableLayoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(0),
            RowCount = 15, // 增加行数以容纳个性选择
            ColumnCount = 2,
            BackColor = Color.FromArgb(40, 40, 40),
            AutoSize = true,
            MaximumSize = new Size(contentPanel.Width - 40, 0) // 减去滚动条宽度
        };
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // 机器人数量
        tableLayoutPanel.Controls.Add(CreateLabel("机器人数量:"), 0, 0);
        _countInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 10,
            Value = 1,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        tableLayoutPanel.Controls.Add(_countInput, 1, 0);

        // 显示命名对话框
        tableLayoutPanel.Controls.Add(CreateLabel("命名对话框:"), 0, 1);
        _namingCheck = new CheckBox
        {
            Text = "启动时询问命名",
            Checked = false, // 默认不询问
            ForeColor = Color.White,
            AutoSize = true
        };
        tableLayoutPanel.Controls.Add(_namingCheck, 1, 1);

        // 默认名字
        tableLayoutPanel.Controls.Add(CreateLabel("默认名字:"), 0, 2);
        _nameInput = new TextBox
        {
            Text = "Claude",
            Width = 150,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        tableLayoutPanel.Controls.Add(_nameInput, 1, 2);

        // 默认大小
        tableLayoutPanel.Controls.Add(CreateLabel("默认大小 (px):"), 0, 3);
        _sizeInput = new NumericUpDown
        {
            Minimum = 32,
            Maximum = 128,
            Value = 64,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        tableLayoutPanel.Controls.Add(_sizeInput, 1, 3);

        // 默认速度
        tableLayoutPanel.Controls.Add(CreateLabel("默认速度 (%):"), 0, 4);
        _speedInput = new NumericUpDown
        {
            Minimum = 50,
            Maximum = 300,
            Value = 100,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        tableLayoutPanel.Controls.Add(_speedInput, 1, 4);

        // 自动启动
        tableLayoutPanel.Controls.Add(CreateLabel("自动启动:"), 0, 5);
        _autoStartCheck = new CheckBox
        {
            Text = "设置后直接启动",
            Checked = false,
            ForeColor = Color.White,
            AutoSize = true
        };
        tableLayoutPanel.Controls.Add(_autoStartCheck, 1, 5);

        // 开启 AI 思考
        tableLayoutPanel.Controls.Add(CreateLabel("开启 AI 自主思考:"), 0, 6);
        _enableAiThinkingCheck = new CheckBox
        {
            Text = "允许机器人随机产生想法",
            Checked = false,
            ForeColor = Color.White,
            AutoSize = true
        };
        tableLayoutPanel.Controls.Add(_enableAiThinkingCheck, 1, 6);

        // AI 思考频率
        tableLayoutPanel.Controls.Add(CreateLabel("思考频率 (秒):"), 0, 7);
        _aiFrequencyInput = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 3600,
            Value = 60,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        tableLayoutPanel.Controls.Add(_aiFrequencyInput, 1, 7);

        // 打架频率
        tableLayoutPanel.Controls.Add(CreateLabel("互动打架几率 (%):"), 0, 8);
        _fightFreqInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100,
            Value = 15,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        tableLayoutPanel.Controls.Add(_fightFreqInput, 1, 8);

        // 武器大师模式
        tableLayoutPanel.Controls.Add(CreateLabel("武器大师模式:"), 0, 9);
        _isWeaponMasterCheck = new CheckBox
        {
            Text = "开启世界机器人大战 (实体子弹)",
            Checked = false,
            ForeColor = Color.Orange,
            AutoSize = true
        };
        tableLayoutPanel.Controls.Add(_isWeaponMasterCheck, 1, 9);

        // 默认个性
        tableLayoutPanel.Controls.Add(CreateLabel("默认个性:"), 0, 10);
        _personalityCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 150,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        // 添加个性选项
        var personalities = new[] {
            (RobotPersonalityType.Friendly, "🤝 友好 - 喜欢交朋友"),
            (RobotPersonalityType.Shy, "🙈 害羞 - 避开其他机器人"),
            (RobotPersonalityType.Rebel, "😈 叛逆 - 喜欢冲撞挑衅"),
            (RobotPersonalityType.Humorous, "😄 幽默 - 喜欢开玩笑"),
            (RobotPersonalityType.Serious, "🤔 严肃 - 行为稳重"),
            (RobotPersonalityType.Curious, "👀 好奇 - 喜欢探索"),
            (RobotPersonalityType.Lazy, "😴 懒惰 - 经常休息"),
            (RobotPersonalityType.Energetic, "⚡ 精力 - 快速移动")
        };
        foreach (var (type, desc) in personalities)
        {
            _personalityCombo.Items.Add(desc);
        }
        _personalityCombo.SelectedIndex = 0;
        tableLayoutPanel.Controls.Add(_personalityCombo, 1, 10);

        // API Key 设置
        tableLayoutPanel.Controls.Add(CreateLabel("API Key:"), 0, 11);
        var apiKeyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 50,
            BackColor = Color.FromArgb(40, 40, 40)
        };
        _apiKeyInput = new TextBox
        {
            Width = 200,
            Height = 25,
            Location = new Point(0, 0),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PasswordChar = '*',
            PlaceholderText = "输入 SiliconFlow API Key"
        };
        _apiKeyStatusLabel = new Label
        {
            Text = "⚠️ 未配置",
            ForeColor = Color.Red,
            Font = new Font("Microsoft YaHei", 8),
            Location = new Point(0, 28),
            AutoSize = true
        };
        apiKeyPanel.Controls.Add(_apiKeyInput);
        apiKeyPanel.Controls.Add(_apiKeyStatusLabel);
        tableLayoutPanel.Controls.Add(apiKeyPanel, 1, 10);

        // 说明标签
        var infoLabel = new Label
        {
            Text = "💡 提示: 左键点击机器人打开CMD终端\n    Ctrl+Shift+M 打开菜单 | Ctrl+Shift+P 暂停/继续 | Ctrl+Shift+H 摸鱼模式",
            ForeColor = Color.Gray,
            Font = new Font("Microsoft YaHei", 9),
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        tableLayoutPanel.Controls.Add(infoLabel, 0, 12);
        tableLayoutPanel.SetColumnSpan(infoLabel, 2);

        // 调整表格布局高度
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 机器人数量
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 命名对话框
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 默认名字
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 默认大小
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 默认速度
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 自动启动
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 开启 AI 思考
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // AI 思考频率
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 打架几率
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 武器大师
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 默认个性
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // API Key
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 说明

        contentPanel.Controls.Add(tableLayoutPanel);

        // 按钮面板
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(20, 10, 20, 10),
            BackColor = Color.FromArgb(40, 40, 40)
        };

        var btnCancel = new Button
        {
            Text = "取消",
            Width = 80,
            Height = 32,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 80, 80),
            ForeColor = Color.White
        };

        var btnSave = new Button
        {
            Text = "保存",
            Width = 100,
            Height = 32,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Lime,
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
        };
        btnSave.Click += (s, e) => Console.WriteLine("[Settings] Save button clicked.");

        buttonPanel.Controls.Add(btnCancel);
        buttonPanel.Controls.Add(btnSave);

        // 添加控件到主容器
        mainContainer.Controls.Add(titleLabel, 0, 0);
        mainContainer.Controls.Add(contentPanel, 0, 1);
        mainContainer.Controls.Add(buttonPanel, 0, 2);

        this.Controls.Add(mainContainer);
        
        // 关键修复：确保按钮在最上层
        buttonPanel.BringToFront();

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;

        // 重新调整TableLayoutPanel的大小以适应内容
        tableLayoutPanel.ResumeLayout(false);
        tableLayoutPanel.PerformLayout();
        contentPanel.ResumeLayout(false);
        contentPanel.PerformLayout();
        mainContainer.ResumeLayout(false);
        mainContainer.PerformLayout();
    }

    private Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.White,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleRight
        };
    }

    private void LoadSettings()
    {
        // 从文件加载设置（如果有）
        try
        {
            string settingsPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "RobotPetSettings.txt");
            if (System.IO.File.Exists(settingsPath))
            {
                var lines = System.IO.File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        switch (parts[0])
                        {
                            case "Count": _countInput.Value = int.Parse(parts[1]); break;
                            case "ShowNaming": _namingCheck.Checked = bool.Parse(parts[1]); break;
                            case "DefaultName": _nameInput.Text = parts[1]; break;
                            case "DefaultSize": _sizeInput.Value = int.Parse(parts[1]); break;
                            case "DefaultSpeed": _speedInput.Value = int.Parse(parts[1]); break;
                            case "AutoStart": _autoStartCheck.Checked = bool.Parse(parts[1]); break;
                            case "EnableAi": _enableAiThinkingCheck.Checked = bool.Parse(parts[1]); break;
                            case "AiFreq": _aiFrequencyInput.Value = int.Parse(parts[1]); break;
                            case "FightFreq": _fightFreqInput.Value = int.Parse(parts[1]); break;
                            case "WeaponMaster": _isWeaponMasterCheck.Checked = bool.Parse(parts[1]); break;
                            case "Personality":
                                if (int.TryParse(parts[1], out int personalityIndex))
                                {
                                    _personalityCombo.SelectedIndex = Math.Clamp(personalityIndex, 0, _personalityCombo.Items.Count - 1);
                                }
                                break;
                        }
                    }
                }
            }

            // 从新的 settings.json 加载 API Key
            var appSettings = PersistenceManager.LoadAppSettings();
            _apiKeyInput.Text = appSettings.ApiKey ?? "";
            UpdateApiKeyStatus();
        }
        catch { }
    }

    private void UpdateApiKeyStatus()
    {
        if (string.IsNullOrWhiteSpace(_apiKeyInput.Text))
        {
            _apiKeyStatusLabel.Text = "⚠️ 未配置（AI功能不可用）";
            _apiKeyStatusLabel.ForeColor = Color.Red;
        }
        else
        {
            _apiKeyStatusLabel.Text = "✓ 已配置";
            _apiKeyStatusLabel.ForeColor = Color.Lime;
        }
    }

    public void SaveSettings()
    {
        RobotCount = (int)_countInput.Value;
        ShowNamingDialog = _namingCheck.Checked;
        RobotName = _nameInput.Text.Trim();
        RobotSize = (int)_sizeInput.Value;
        RobotSpeed = (int)_speedInput.Value;
        AutoStart = _autoStartCheck.Checked;
        EnableAiThinking = _enableAiThinkingCheck.Checked;
        AiThoughtFrequency = (int)_aiFrequencyInput.Value;
        FightFrequency = (int)_fightFreqInput.Value;
        IsWeaponMaster = _isWeaponMasterCheck.Checked;
        DefaultPersonality = (RobotPersonalityType)_personalityCombo.SelectedIndex;
        ApiKey = _apiKeyInput.Text.Trim();


        // 保存到文件
        try
        {
            string settingsPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "RobotPetSettings.txt");
            var lines = new[]
            {
                $"Count={RobotCount}",
                $"ShowNaming={ShowNamingDialog}",
                $"DefaultName={RobotName}",
                $"DefaultSize={RobotSize}",
                $"DefaultSpeed={RobotSpeed}",
                $"AutoStart={AutoStart}",
                $"EnableAi={EnableAiThinking}",
                $"AiFreq={AiThoughtFrequency}",
                $"FightFreq={FightFrequency}",
                $"WeaponMaster={IsWeaponMaster}",
                $"Personality={(int)DefaultPersonality}",

            };
            System.IO.File.WriteAllLines(settingsPath, lines);

            // 保存 API Key 到新的 settings.json
            PersistenceManager.SetApiKey(ApiKey);
        }
        catch { }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (this.DialogResult == DialogResult.OK)
        {
            SaveSettings();
        }
        base.OnFormClosing(e);
    }
}
