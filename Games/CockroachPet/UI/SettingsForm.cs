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
    public int AiThoughtFrequency { get; set; } = 60;
    public int FightFrequency { get; set; } = 15;
    public bool IsWeaponMaster { get; set; } = false;
    public int SkillScale { get; set; } = 100;
    public string ApiKey { get; set; } = "";
    public RobotPersonalityType DefaultPersonality { get; set; } = RobotPersonalityType.Friendly;

    private NumericUpDown _countInput = null!;
    private NumericUpDown _sizeInput = null!;
    private NumericUpDown _speedInput = null!;
    private NumericUpDown _skillScaleInput = null!;
    private TextBox _nameInput = null!;
    private CheckBox _namingCheck = null!;
    private CheckBox _autoStartCheck = null!;
    private CheckBox _enableAiThinkingCheck = null!;
    private NumericUpDown _aiFrequencyInput = null!;
    private NumericUpDown _fightFreqInput = null!;
    private CheckBox _isWeaponMasterCheck = null!;
    private TextBox _apiKeyInput = null!;
    private Label _apiKeyStatusLabel = null!;
    private ComboBox _personalityCombo = null!;

    public SettingsForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "设置 - 像素机器人 (Robot Pet Settings)";
        this.Size = new Size(560, 640);
        this.MinimumSize = new Size(560, 640);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.TopMost = true;
        this.BackColor = Color.FromArgb(32, 32, 32);
        this.ForeColor = Color.White;
        this.Font = new Font("Microsoft YaHeiUI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

        // 顶栏标题
        var titlePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.FromArgb(24, 24, 24)
        };
        var titleLabel = new Label
        {
            Text = "⚙️ 游戏与机器人设置",
            Font = new Font("Microsoft YaHeiUI", 14F, FontStyle.Bold),
            ForeColor = Color.Lime,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        titlePanel.Controls.Add(titleLabel);

        // 底栏按钮
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(24, 24, 24),
            Padding = new Padding(15)
        };

        var btnCancel = new Button
        {
            Text = "取消",
            Size = new Size(90, 32),
            Location = new Point(440, 14),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        var btnSave = new Button
        {
            Text = "保存设置",
            Size = new Size(110, 32),
            Location = new Point(315, 14),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Lime,
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHeiUI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => SaveSettings();

        buttonPanel.Controls.Add(btnSave);
        buttonPanel.Controls.Add(btnCancel);

        // 中间滚动内容面板
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(32, 32, 32)
        };

        int curY = 15;
        int rowHeight = 38;
        int labelWidth = 160;
        int inputLeft = 180;

        void AddRow(string labelText, Control control, string subTip = "")
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(10, curY + 4),
                Size = new Size(labelWidth, 25),
                ForeColor = Color.FromArgb(220, 220, 220),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Microsoft YaHeiUI", 9.5F, FontStyle.Regular)
            };
            control.Location = new Point(inputLeft, curY);

            contentPanel.Controls.Add(lbl);
            contentPanel.Controls.Add(control);

            if (!string.IsNullOrEmpty(subTip))
            {
                var tipLbl = new Label
                {
                    Text = subTip,
                    Location = new Point(inputLeft + control.Width + 10, curY + 4),
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Font = new Font("Microsoft YaHeiUI", 8.5F)
                };
                contentPanel.Controls.Add(tipLbl);
            }

            curY += rowHeight;
        }

        // 1. 机器人数量
        _countInput = new NumericUpDown
        {
            Minimum = 1, Maximum = 10, Value = 1, Width = 100,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };
        AddRow("机器人初始数量:", _countInput);

        // 2. 默认名字
        _nameInput = new TextBox
        {
            Text = "Claude", Width = 180,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        AddRow("默认机器人名字:", _nameInput);

        // 3. 询问命名
        _namingCheck = new CheckBox
        {
            Text = "启动时询问自定义名字", Checked = false,
            ForeColor = Color.White, AutoSize = true
        };
        AddRow("命名确认对话框:", _namingCheck);

        // 4. 尺寸大小
        _sizeInput = new NumericUpDown
        {
            Minimum = 8, Maximum = 128, Value = 32, Width = 100,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };
        AddRow("机器人尺寸 (px):", _sizeInput, "(8px ~ 128px, 调小可精细微型化)");

        // 5. 移动速度
        _speedInput = new NumericUpDown
        {
            Minimum = 50, Maximum = 300, Value = 100, Width = 100,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };
        AddRow("全局基础速度 (%):", _speedInput);

        // 5.5 技能特效尺寸缩放
        _skillScaleInput = new NumericUpDown
        {
            Minimum = 10, Maximum = 200, Value = 100, Width = 100,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };
        AddRow("技能特效尺寸 (%):", _skillScaleInput, "(10% ~ 200%, 按角色自适应并可手调)");

        // 6. 自动启动
        _autoStartCheck = new CheckBox
        {
            Text = "设置后直接进入战斗", Checked = false,
            ForeColor = Color.White, AutoSize = true
        };
        AddRow("自动启动模式:", _autoStartCheck);

        // 7. 开启 AI 自主思考
        _enableAiThinkingCheck = new CheckBox
        {
            Text = "允许机器人产生自主想法", Checked = false,
            ForeColor = Color.White, AutoSize = true
        };
        AddRow("AI 自主思考模式:", _enableAiThinkingCheck);

        // 7. 思考频率
        _aiFrequencyInput = new NumericUpDown
        {
            Minimum = 10, Maximum = 3600, Value = 60, Width = 100,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };
        AddRow("AI 思考间隔 (秒):", _aiFrequencyInput);

        // 8. 互动打架几率
        _fightFreqInput = new NumericUpDown
        {
            Minimum = 0, Maximum = 100, Value = 15, Width = 100,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };
        AddRow("遭遇打架几率 (%):", _fightFreqInput);

        // 9. 武器大师模式
        _isWeaponMasterCheck = new CheckBox
        {
            Text = "开启超级武器库 (火箭/等离子/重炮)", Checked = false,
            ForeColor = Color.Orange, AutoSize = true
        };
        AddRow("武器大师技能:", _isWeaponMasterCheck);

        // 10. 默认个性
        _personalityCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Width = 220,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
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
        foreach (var (_, desc) in personalities) _personalityCombo.Items.Add(desc);
        _personalityCombo.SelectedIndex = 0;
        AddRow("默认初始个性:", _personalityCombo);

        // 11. API Key
        var apiContainer = new Panel { Size = new Size(320, 50), BackColor = Color.Transparent };
        _apiKeyInput = new TextBox
        {
            Width = 220, Location = new Point(0, 0),
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle, PasswordChar = '*'
        };
        _apiKeyInput.TextChanged += (s, e) => UpdateApiKeyStatus();

        _apiKeyStatusLabel = new Label
        {
            Text = "⚠️ 未配置 (离线战吼可用, 大模型禁用)",
            ForeColor = Color.Red,
            Font = new Font("Microsoft YaHeiUI", 8.5F),
            Location = new Point(0, 26), AutoSize = true
        };
        apiContainer.Controls.Add(_apiKeyInput);
        apiContainer.Controls.Add(_apiKeyStatusLabel);

        AddRow("API Key (硅基流动):", apiContainer);
        curY += 15;

        // 提示说明卡片
        var infoPanel = new Panel
        {
            Location = new Point(30, curY),
            Size = new Size(470, 75),
            BackColor = Color.FromArgb(24, 24, 24),
            BorderStyle = BorderStyle.FixedSingle
        };
        var infoLabel = new Label
        {
            Text = "💡 快捷键指南:\n • Ctrl + N : 添加新机器人  |  Ctrl + M : 投放怪物\n • Ctrl + K : 开/关骂人模式  |  Ctrl + R : 重置局势\n • 鼠标左键选中机器人，右键可指点集火发射！",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Microsoft YaHeiUI", 8.8F),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        infoPanel.Controls.Add(infoLabel);
        contentPanel.Controls.Add(infoPanel);

        // 组装窗体
        this.Controls.Add(contentPanel);
        this.Controls.Add(titlePanel);
        this.Controls.Add(buttonPanel);

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    private void LoadSettings()
    {
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
                            case "Count": _countInput.Value = Math.Clamp(int.Parse(parts[1]), 1, 10); break;
                            case "ShowNaming": _namingCheck.Checked = bool.Parse(parts[1]); break;
                            case "DefaultName": _nameInput.Text = parts[1]; break;
                            case "DefaultSize": _sizeInput.Value = Math.Clamp(int.Parse(parts[1]), 8, 128); break;
                            case "DefaultSpeed": _speedInput.Value = Math.Clamp(int.Parse(parts[1]), 50, 300); break;
                            case "SkillScale": _skillScaleInput.Value = Math.Clamp(int.Parse(parts[1]), 10, 200); break;
                            case "AutoStart": _autoStartCheck.Checked = bool.Parse(parts[1]); break;
                            case "EnableAi": _enableAiThinkingCheck.Checked = bool.Parse(parts[1]); break;
                            case "AiFreq": _aiFrequencyInput.Value = Math.Clamp(int.Parse(parts[1]), 10, 3600); break;
                            case "FightFreq": _fightFreqInput.Value = Math.Clamp(int.Parse(parts[1]), 0, 100); break;
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
            _apiKeyStatusLabel.Text = "⚠️ 未配置 (离线战吼可用, 大模型禁用)";
            _apiKeyStatusLabel.ForeColor = Color.OrangeRed;
        }
        else
        {
            _apiKeyStatusLabel.Text = "✓ 已配置 (大模型聊天已就绪)";
            _apiKeyStatusLabel.ForeColor = Color.Lime;
        }
    }

    public void SaveSettings()
    {
        if (_countInput != null) RobotCount = (int)_countInput.Value;
        if (_namingCheck != null) ShowNamingDialog = _namingCheck.Checked;
        if (_nameInput != null) RobotName = _nameInput.Text.Trim();
        if (_sizeInput != null) RobotSize = (int)_sizeInput.Value;
        if (_speedInput != null) RobotSpeed = (int)_speedInput.Value;
        if (_skillScaleInput != null) SkillScale = (int)_skillScaleInput.Value;
        if (_autoStartCheck != null) AutoStart = _autoStartCheck.Checked;
        if (_enableAiThinkingCheck != null) EnableAiThinking = _enableAiThinkingCheck.Checked;
        if (_aiFrequencyInput != null) AiThoughtFrequency = (int)_aiFrequencyInput.Value;
        if (_fightFreqInput != null) FightFrequency = (int)_fightFreqInput.Value;
        if (_isWeaponMasterCheck != null) IsWeaponMaster = _isWeaponMasterCheck.Checked;
        if (_personalityCombo != null) DefaultPersonality = (RobotPersonalityType)Math.Max(0, _personalityCombo.SelectedIndex);
        if (_apiKeyInput != null) ApiKey = _apiKeyInput.Text.Trim();

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
                $"SkillScale={SkillScale}",
                $"AutoStart={AutoStart}",
                $"EnableAi={EnableAiThinking}",
                $"AiFreq={AiThoughtFrequency}",
                $"FightFreq={FightFrequency}",
                $"WeaponMaster={IsWeaponMaster}",
                $"Personality={(int)DefaultPersonality}",
            };
            System.IO.File.WriteAllLines(settingsPath, lines);

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
