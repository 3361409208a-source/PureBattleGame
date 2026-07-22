using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using PureBattleGame.Games.CockroachPet;

namespace PureBattleGame.Games.CockroachPet.UI;

public class AiRobotGeneratorForm : Form
{
    private TextBox _promptInput = null!;
    private Label _statusLabel = null!;
    private Button _btnGenerate = null!;
    private Button _btnCancel = null!;
    private FlowLayoutPanel _presetPanel = null!;

    public List<AiGeneratedRobotConfig> GeneratedConfigs { get; private set; } = new();

    public AiRobotGeneratorForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "🤖 AI 自然语言智能生成机器人";
        this.Size = new Size(580, 440);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.TopMost = true;
        this.BackColor = Color.FromArgb(32, 32, 36);
        this.ForeColor = Color.White;
        this.Font = new Font("Microsoft YaHei UI", 9.5f);

        // Header Panel
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(24, 24, 28),
            Padding = new Padding(15, 10, 15, 10)
        };

        var titleLabel = new Label
        {
            Text = "✨ AI 自然语言智能生成助手",
            Font = new Font("Microsoft YaHei UI", 11.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 225, 255),
            AutoSize = true,
            Location = new Point(15, 12)
        };

        var descLabel = new Label
        {
            Text = "输入自然语言指令（如“加入赛罗”、“加入十个奥特曼成员”），AI 将自动解析并投放专属机器人！",
            Font = new Font("Microsoft YaHei UI", 8.5f),
            ForeColor = Color.FromArgb(170, 170, 180),
            AutoSize = true,
            Location = new Point(16, 38)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(descLabel);

        var inputLabel = new Label
        {
            Text = "💬 请输入生成指令：",
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 85),
            AutoSize = true
        };

        _promptInput = new TextBox
        {
            Location = new Point(20, 112),
            Size = new Size(524, 60),
            Multiline = true,
            BackColor = Color.FromArgb(48, 48, 54),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei UI", 10.5f),
            Text = "加入赛罗"
        };

        var presetLabel = new Label
        {
            Text = "💡 快捷指令推荐：",
            Font = new Font("Microsoft YaHei UI", 8.5f),
            ForeColor = Color.FromArgb(180, 180, 190),
            Location = new Point(20, 182),
            AutoSize = true
        };

        _presetPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 204),
            Size = new Size(524, 75),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent
        };

        AddPresetButton("⚡ 加入赛罗", "加入赛罗");
        AddPresetButton("🦸 10个奥特曼成员", "加入十个奥特曼成员");
        AddPresetButton("⚔️ 三国五虎上将", "生成五个三国武将");
        AddPresetButton("🛡️ 复仇者联盟", "召唤四个复仇者成员");
        AddPresetButton("🔥 4元素法师", "生成四个元素法师");

        _statusLabel = new Label
        {
            Text = "就绪。点击下方“智能生成并投放”按钮开始生成。",
            Font = new Font("Microsoft YaHei UI", 8.5f),
            ForeColor = Color.FromArgb(140, 200, 255),
            Location = new Point(20, 290),
            Size = new Size(524, 25),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Bottom Action Panel
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(24, 24, 28)
        };

        _btnCancel = new Button
        {
            Text = "取消",
            Size = new Size(100, 36),
            Location = new Point(300, 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        _btnCancel.FlatAppearance.BorderSize = 0;
        _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        _btnGenerate = new Button
        {
            Text = "✨ 智能生成并投放",
            Size = new Size(160, 36),
            Location = new Point(410, 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 180, 130),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnGenerate.FlatAppearance.BorderSize = 0;
        _btnGenerate.Click += async (s, e) => await StartGenerationAsync();

        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnGenerate);

        this.Controls.Add(_statusLabel);
        this.Controls.Add(_presetPanel);
        this.Controls.Add(presetLabel);
        this.Controls.Add(_promptInput);
        this.Controls.Add(inputLabel);
        this.Controls.Add(headerPanel);
        this.Controls.Add(bottomPanel);
    }

    private void AddPresetButton(string label, string promptText)
    {
        var btn = new Button
        {
            Text = label,
            AutoSize = true,
            Height = 30,
            Margin = new Padding(0, 0, 8, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(52, 52, 60),
            ForeColor = Color.FromArgb(220, 220, 240),
            Font = new Font("Microsoft YaHei UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 95);
        btn.Click += (s, e) =>
        {
            _promptInput.Text = promptText;
        };
        _presetPanel.Controls.Add(btn);
    }

    private async Task StartGenerationAsync()
    {
        string prompt = _promptInput.Text.Trim();
        if (string.IsNullOrEmpty(prompt))
        {
            MessageBox.Show("请输入生成指令！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _btnGenerate.Enabled = false;
        _btnCancel.Enabled = false;
        _promptInput.Enabled = false;
        _statusLabel.Text = "🤖 AI 正在智能分析指令拆解生成中，请稍候...";
        _statusLabel.ForeColor = Color.Yellow;

        try
        {
            var configs = await AiService.GenerateRobotsFromPromptAsync(prompt);
            if (configs != null && configs.Count > 0)
            {
                GeneratedConfigs = configs;
                _statusLabel.Text = $"🎉 成功生成 {configs.Count} 个专属机器人！正在投放...";
                _statusLabel.ForeColor = Color.Lime;
                await Task.Delay(500);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _statusLabel.Text = "❌ 生成失败，未获得有效配置。";
                _statusLabel.ForeColor = Color.Tomato;
                _btnGenerate.Enabled = true;
                _btnCancel.Enabled = true;
                _promptInput.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"❌ 出错: {ex.Message}";
            _statusLabel.ForeColor = Color.Tomato;
            _btnGenerate.Enabled = true;
            _btnCancel.Enabled = true;
            _promptInput.Enabled = true;
        }
    }
}
