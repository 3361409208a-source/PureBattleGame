using System;
using System.Drawing;
using System.Windows.Forms;

using PureBattleGame.Games.StarCoreDefense;

namespace PureBattleGame.Core;

public partial class MoyuLauncher : Form
{
    public static MoyuLauncher? Instance { get; private set; }
    private BattleForm? _gameInstance;

    public MoyuLauncher()
    {
        Instance = this;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "办公神器 v1.0";
        this.Size = new Size(400, 300);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 35);
        this.Opacity = 0.9; 

        Label title = new Label
        {
            Text = "办公神器合集",
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(125, 30)
        };
        this.Controls.Add(title);

        Button btnGame1 = CreateBtn("BtnGame1", "🎮 星核防线 (星际挂机塔防)", 100, 100);
        btnGame1.Click += (s, e) => {
            if (_gameInstance == null || _gameInstance.IsDisposed)
            {
                _gameInstance = new BattleForm();
            }
            this.Hide();
            _gameInstance.Show();
            _gameInstance.BringToFront();
            _gameInstance.Focus();
        };
        this.Controls.Add(btnGame1);

        Button btnGame2 = CreateBtn("BtnGame2", "🔒 更多办公功能开发中...", 100, 160);
        btnGame2.Enabled = false;
        this.Controls.Add(btnGame2);
        
        Label tip = new Label
        {
            Text = "提示: 任何界面按 Alt+Space 一键隐藏\n在游戏中按 Alt+Q 可返回主页",
            Font = new Font("Microsoft YaHei", 8),
            ForeColor = Color.Gray,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(90, 220)
        };
        this.Controls.Add(tip);
    }

    private Button CreateBtn(string name, string text, int x, int y)
    {
        var btn = new Button
        {
            Name = name,
            Text = text,
            Location = new Point(x, y),
            Size = new Size(200, 40),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(50, 50, 60),
            Font = new Font("Microsoft YaHei", 10, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 90);
        return btn;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // 只有在按下 Alt 键时才触发办公快捷键
        if ((keyData & Keys.Alt) == Keys.Alt)
        {
            Keys baseKey = keyData & ~Keys.Alt;

            // Alt + Up: 增加透明度
            if (baseKey == Keys.Up)
            {
                if (this.Opacity < 1.0) this.Opacity += 0.1;
                if (this.Opacity > 1.0) this.Opacity = 1.0;
                return true;
            }
            // Alt + Down: 减少透明度
            else if (baseKey == Keys.Down)
            {
                if (this.Opacity > 0.1) this.Opacity -= 0.1;
                if (this.Opacity < 0.1) this.Opacity = 0.1;
                return true;
            }
            // Alt + Space: 老板键
            else if (baseKey == Keys.Space)
            {
                if (this.Opacity > 0.0)
                {
                    this.Tag = this.Opacity;
                    this.Opacity = 0.0;
                    this.ShowInTaskbar = false;
                }
                else
                {
                    this.Opacity = (this.Tag is double op) ? op : 1.0;
                    this.ShowInTaskbar = true;
                }
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
