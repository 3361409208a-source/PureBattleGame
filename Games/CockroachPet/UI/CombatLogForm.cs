using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PureBattleGame.Games.CockroachPet.UI;

public class CombatLogForm : Form
{
    private static CombatLogForm? _instance;
    public static CombatLogForm Instance => _instance ??= new CombatLogForm();

    private readonly TabControl _tabControl;
    private readonly ListView _statsListView;
    private readonly RichTextBox _logRichTextBox;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly Label _mvpLabel;

    public CombatLogForm()
    {
        _instance = this;
        Text = "⚔️ 实时战斗日志与 MVP 战况统计看板 (Ctrl+Shift+L)";
        Size = new Size(820, 560);
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        BackColor = Color.FromArgb(18, 18, 24);
        ForeColor = Color.FromArgb(240, 240, 245);
        Icon = SystemIcons.Shield;

        // 顶栏 MVP Banner
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(28, 28, 38),
            Padding = new Padding(15, 10, 15, 10)
        };

        var titleLabel = new Label
        {
            Text = "⚔️ 实时战斗风云与战力统计",
            Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 77, 109),
            AutoSize = true,
            Location = new Point(15, 10)
        };

        _mvpLabel = new Label
        {
            Text = "👑 桌面 MVP: 暂无数据",
            Font = new Font("Microsoft YaHei", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 200, 80),
            AutoSize = true,
            Location = new Point(15, 33)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(_mvpLabel);
        Controls.Add(headerPanel);

        // 主 Tab 页
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(12, 6),
            Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold)
        };

        var statsPage = new TabPage("📊 MVP 伤害击杀榜")
        {
            BackColor = Color.FromArgb(22, 22, 30),
            ForeColor = Color.FromArgb(240, 240, 245)
        };

        var logPage = new TabPage("📜 实时战况滚屏")
        {
            BackColor = Color.FromArgb(22, 22, 30),
            ForeColor = Color.FromArgb(240, 240, 245)
        };

        // ListView 初始化
        _statsListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            BackColor = Color.FromArgb(16, 16, 22),
            ForeColor = Color.FromArgb(230, 230, 240),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
        };

        _statsListView.Columns.Add("排名", 50);
        _statsListView.Columns.Add("角色名称", 120);
        _statsListView.Columns.Add("性格", 70);
        _statsListView.Columns.Add("当前HP", 80);
        _statsListView.Columns.Add("累积输出(DPS)", 110);
        _statsListView.Columns.Add("承受伤害", 90);
        _statsListView.Columns.Add("击杀数", 70);
        _statsListView.Columns.Add("专属流派技能", 200);

        statsPage.Controls.Add(_statsListView);

        // RichTextBox 初始化
        _logRichTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(14, 14, 18),
            ForeColor = Color.FromArgb(220, 220, 230),
            Font = new Font("Consolas", 10f, FontStyle.Regular),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };

        logPage.Controls.Add(_logRichTextBox);

        _tabControl.TabPages.Add(statsPage);
        _tabControl.TabPages.Add(logPage);
        Controls.Add(_tabControl);

        // 底部控制栏
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            BackColor = Color.FromArgb(28, 28, 38),
            Padding = new Padding(10)
        };

        var btnClear = new Button
        {
            Text = "🗑️ 清空日志",
            Width = 100,
            Height = 28,
            Location = new Point(15, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 58),
            ForeColor = Color.White
        };
        btnClear.FlatAppearance.BorderSize = 0;
        btnClear.Click += (s, e) =>
        {
            PetForm.Instance?.ClearCombatLogs();
            _logRichTextBox.Clear();
        };

        var btnClose = new Button
        {
            Text = "关闭窗口",
            Width = 90,
            Height = 28,
            Location = new Point(695, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(225, 29, 72),
            ForeColor = Color.White
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => Hide();

        bottomPanel.Controls.Add(btnClear);
        bottomPanel.Controls.Add(btnClose);
        Controls.Add(bottomPanel);

        // 定时刷新器 (1秒刷新一次)
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 800 };
        _refreshTimer.Tick += (s, e) => RefreshData();
    }

    public void ShowWindow()
    {
        if (IsDisposed)
        {
            _instance = new CombatLogForm();
            _instance.ShowWindow();
            return;
        }

        Show();
        BringToFront();
        Focus();
        _refreshTimer.Start();
        RefreshData();
    }

    private void RefreshData()
    {
        if (PetForm.Instance == null || IsDisposed) return;

        // 1. 刷新排行榜
        var robots = PetForm.Instance.GetRobots();
        var sorted = robots.OrderByDescending(r => r.DamageDealt).ToList();

        _statsListView.BeginUpdate();
        _statsListView.Items.Clear();

        if (sorted.Count > 0 && sorted[0].DamageDealt > 0)
        {
            _mvpLabel.Text = $"👑 全场 MVP: {sorted[0].Name} (累积输出: {sorted[0].DamageDealt} HP, 击杀: {sorted[0].Kills})";
        }
        else
        {
            _mvpLabel.Text = "👑 桌面 MVP: 战斗火热准备中...";
        }

        for (int i = 0; i < sorted.Count; i++)
        {
            var r = sorted[i];
            var item = new ListViewItem((i + 1).ToString());
            item.SubItems.Add(r.Name);
            item.SubItems.Add(r.Personality.ToString());
            item.SubItems.Add($"{r.HP}/{r.MaxHP}");
            item.SubItems.Add(r.DamageDealt.ToString());
            item.SubItems.Add(r.DamageTaken.ToString());
            item.SubItems.Add(r.Kills.ToString());
            item.SubItems.Add(r.PersonalWeapons.Count > 0 ? string.Join(", ", r.PersonalWeapons) : "全技能");

            if (i == 0 && r.DamageDealt > 0)
            {
                item.ForeColor = Color.Gold;
                item.Font = new Font(_statsListView.Font, FontStyle.Bold);
            }

            _statsListView.Items.Add(item);
        }

        _statsListView.EndUpdate();

        // 2. 刷新实时日志
        var logs = PetForm.Instance.GetCombatLogs();
        if (logs.Count > 0)
        {
            _logRichTextBox.SuspendLayout();
            _logRichTextBox.Clear();
            foreach (var l in logs)
            {
                Color c = l.Type == "KILL" ? Color.HotPink : Color.LightBlue;
                _logRichTextBox.SelectionColor = Color.Gray;
                _logRichTextBox.AppendText($"[{l.Time}] ");
                _logRichTextBox.SelectionColor = c;
                _logRichTextBox.AppendText($"{l.Message}\n");
            }
            _logRichTextBox.SelectionStart = _logRichTextBox.Text.Length;
            _logRichTextBox.ScrollToCaret();
            _logRichTextBox.ResumeLayout();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            _refreshTimer.Stop();
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }
}
