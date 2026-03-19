using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace PureBattleGame;

public class FloatingText
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Dy { get; set; }
    public string Text { get; set; } = "";
    public Color TextColor { get; set; }
    public int Life { get; set; }
    public int MaxLife { get; set; }
    public bool IsActive => Life > 0;
}

public partial class BattleForm : Form
{
    public static BattleForm? Instance { get; private set; }

    // 游戏状态
    private List<Robot> _robots = new List<Robot>();
    private List<Monster> _monsters = new List<Monster>();
    private List<Projectile> _projectiles = new List<Projectile>();
    private List<FloatingText> _floatingTexts = new List<FloatingText>();

    // 游戏参数
    private int _robotIdCounter = 1;
    private int _baseWaveTimer = 0;
    private bool _isGameEnding = false;
    private Robot? _winner = null;
    private int _resetTimer = 0;
    private Random _rand = new Random();

    // 资源与波次系统
    public int Gold { get; set; } = 2000;
    public int Minerals { get; set; } = 500;
    public int CurrentWave { get; set; } = 1;
    private int _waveTimer = 600; // 10秒后开始第一波
    private int _monstersToSpawnInWave = 0;
    private int _spawnInterval = 0;

    // 全局升级系统
    public float GlobalDamageMultiplier { get; set; } = 1.0f;
    public float GlobalHealthMultiplier { get; set; } = 1.0f;

    // 兵种升级等级
    public int _baseLevel = 1;
    public int _workerLevel = 1;
    public int _healerLevel = 1;
    public int _shooterLevel = 1;
    public int _guardianLevel = 1;

    // 机器人价格递增
    private int _workerCost = 50;
    private int _defenderCost = 150;
    private int _shooterCost = 100;
    private int _guardianCost = 200;

    // 办公浏览器
    private WebView2? _webView;
    private Panel? _browserPanel;
    private bool _isBrowserVisible = false;

    // 渲染
    private Bitmap? _backBuffer;
    private Graphics? _bufferGraphics;

    // 粒子系统
    private List<Particle> _particles = new List<Particle>();
    private List<Mineral> _minerals = new List<Mineral>();
    private int _mineralSpawnTimer = 300; // 每 5 秒尝试生成一个矿物

    // 鼠标交互
    private Robot? _selectedRobot = null;
    private Monster? _selectedMonster = null;
    private bool _isSpawningMonster = false;
    private Point _monsterSpawnPoint;

    private sealed class FlickerFreePanel : Panel
    {
        public FlickerFreePanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }
    }

    public BattleForm()
    {
        Instance = this;
        InitializeComponent();
        SetupGame();
    }

    private void InitializeComponent()
    {
        this.Text = "纯粹战斗游戏";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        this.FormBorderStyle = FormBorderStyle.None; // 无边框模式
        this.Opacity = 0.1; // 启动时默认最低透明度档位，方便办公

        // 键盘快捷键
        this.KeyPreview = true;
        this.KeyDown += BattleForm_KeyDown;

        // 鼠标事件
        this.MouseDown += BattleForm_MouseDown;
        this.MouseMove += BattleForm_MouseMove;
        this.MouseUp += BattleForm_MouseUp;

        InitializeBrowser();
    }

    private async void InitializeBrowser()
    {
        _browserPanel = new Panel
        {
            Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 120),
            Location = new Point(20, 20),
            BackColor = Color.White,
            Visible = false,
            BorderStyle = BorderStyle.FixedSingle
        };

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };

        _browserPanel.Controls.Add(_webView);
        this.Controls.Add(_browserPanel);

        // 初始化 WebView2
        await _webView.EnsureCoreWebView2Async(null);

        // 阻止新窗口弹出，强制在当前 WebView 内打开链接
        _webView.CoreWebView2.NewWindowRequested += (sender, e) =>
        {
            e.Handled = true; // 拦截新窗口
            _webView.CoreWebView2.Navigate(e.Uri); // 在当前窗口加载新链接
        };

        // 监听浏览器内的键盘事件，将快捷键传递回主窗体
        _webView.KeyDown += (sender, e) =>
        {
            if (e.Alt)
            {
                if (e.KeyCode == Keys.Up)
                {
                    if (this.Opacity < 1.0) this.Opacity += 0.1;
                    if (this.Opacity > 1.0) this.Opacity = 1.0;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    if (this.Opacity > 0.1) this.Opacity -= 0.1;
                    if (this.Opacity < 0.1) this.Opacity = 0.1;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Space)
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
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.B)
                {
                    _isBrowserVisible = false;
                    _browserPanel!.Visible = false;
                    this.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Q)
                {
                    ReturnToHome();
                    e.Handled = true;
                }
            }
        };

        _webView.CoreWebView2.Navigate("https://bing.com"); // 默认打开 Bing
    }

    private void SetupGame()
    {
        // 创建双缓冲
        _backBuffer = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
        _bufferGraphics = Graphics.FromImage(_backBuffer);
        _bufferGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        _bufferGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        _bufferGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        // 初始化音效系统
        AudioManager.Initialize();

        // 生成基地
        var baseRobot = SpawnRobot("主基地", this.ClientSize.Width / 2 - 20, this.ClientSize.Height / 2 - 20, RobotClass.Base);
        baseRobot.ApplyClassProperties();

        // 生成初始机器人 (1个治疗，2个输出)
        var def = SpawnRobot("医疗兵", this.ClientSize.Width / 2, this.ClientSize.Height / 2 + 50, RobotClass.Healer);
        def.ApplyClassProperties();

        SpawnRobot("游侠A", this.ClientSize.Width / 2 - 60, this.ClientSize.Height / 2 + 80, RobotClass.Shooter).ApplyClassProperties();
        SpawnRobot("游侠B", this.ClientSize.Width / 2 + 20, this.ClientSize.Height / 2 + 80, RobotClass.Shooter).ApplyClassProperties();

        // 启动游戏循环
        var timer = new System.Windows.Forms.Timer();
        timer.Interval = 16; // 约60FPS
        timer.Tick += (s, e) => GameLoop();
        timer.Start();

        // 控制面板定时器
        var uiTimer = new System.Windows.Forms.Timer();
        uiTimer.Interval = 100;
        uiTimer.Tick += (s, e) => UpdateUI();
        uiTimer.Start();

        // 创建控制面板
        CreateControlPanel();
        CreateHomeButton();
    }

    private void ReturnToHome()
    {
        this.Hide();
        MoyuLauncher.Instance?.Show();
    }

    private void CreateHomeButton()
    {
        Button btnHome = new Button
        {
            Text = "🏠 主页",
            Location = new Point(this.ClientSize.Width - 75, 5),
            Size = new Size(70, 25),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(50, 50, 60),
            Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnHome.FlatAppearance.BorderSize = 1;
        btnHome.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
        btnHome.Click += (s, e) => ReturnToHome();
        this.Controls.Add(btnHome);
        btnHome.BringToFront();
    }

    public Robot? GetBaseRobot()
    {
        return _robots.FirstOrDefault(r => r.ClassType == RobotClass.Base && r.IsActive && !r.IsDead);
    }

    private void GameLoop()
    {
        if (_isGameEnding)
        {
            HandleGameEnding();
            return;
        }

        // 更新机器人
        foreach (var robot in _robots)
        {
            if (robot.IsActive && !robot.IsDead)
            {
                robot.Update(this.ClientSize.Width, this.ClientSize.Height, _robots, _monsters);
            }
        }

        // 更新怪物
        foreach (var monster in _monsters)
        {
            if (monster.IsActive && !monster.IsDead)
            {
                monster.Update(this.ClientSize.Width, this.ClientSize.Height, _robots);
            }
        }

        // 更新矿物生成逻辑
        _mineralSpawnTimer--;
        if (_mineralSpawnTimer <= 0)
        {
            _mineralSpawnTimer = 300 + _rand.Next(300);
            if (_minerals.Count < 8) // 最多存在 8 个矿物
            {
                int mx = 50 + _rand.Next(this.ClientSize.Width - 100);
                int my = 50 + _rand.Next(this.ClientSize.Height - 150);
                _minerals.Add(new Mineral(mx, my));
            }
        }

        // 更新粒子
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Update();
            if (!p.IsActive)
            {
                _particles.RemoveAt(i);
            }
        }

        // 更新浮动文字
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            var ft = _floatingTexts[i];
            ft.Y += ft.Dy;
            ft.Life--;
            if (!ft.IsActive) _floatingTexts.RemoveAt(i);
        }

        // 更新投射物
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var p = _projectiles[i];
            if (!p.IsActive)
            {
                _projectiles.RemoveAt(i);
                continue;
            }

            p.Update();

            // 检测与机器人碰撞
            foreach (var robot in _robots)
            {
                // 只有怪物的投射物才能对机器人造成伤害
                if (robot.IsActive && !robot.IsDead && p.IsMonsterProjectile)
                {
                    if (CheckCollision(p, robot))
                    {
                        robot.HandleProjectileHit(p);
                        p.IsActive = false;
                        _projectiles.RemoveAt(i);
                        break;
                    }
                }
            }

            // 检测与怪物碰撞 (只有机器人的投射物才对怪物造成伤害)
            if (p.IsActive && !p.IsMonsterProjectile)
            {
                foreach (var monster in _monsters)
                {
                    if (monster.IsActive && !monster.IsDead)
                    {
                        if (CheckCollision(p, monster))
                        {
                            monster.OnHit(p);

                            // 命中特效
                            AddExplosion(p.X, p.Y, p.ProjectileColor, 5, "SPARK");
                            if (p.Type == "METEOR") AddExplosion(p.X, p.Y, Color.OrangeRed, 10, "SMOKE");
                            if (p.Type == "BLACK_HOLE") AddExplosion(p.X, p.Y, Color.Purple, 1, "RING");

                            if (p.Type != "BLACK_HOLE" && p.Type != "DEATH_RAY") // 穿透性武器不销毁
                            {
                                p.IsActive = false;
                                _projectiles.RemoveAt(i);
                            }
                            break;
                        }
                    }
                }
            }
        }

        // 处理所有实体之间的物理碰撞
        HandleAllCollisions();

        // 基地能量波技能 (每6秒)
        UpdateBaseWave();

        // 检查波次逻辑
        HandleWaveLogic();

        // 检查游戏结束条件
        CheckGameEnd();

        // 重绘
        this.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_bufferGraphics != null && _backBuffer != null)
        {
            _bufferGraphics.Clear(Color.FromArgb(20, 20, 25)); // 更深邃的背景
            Render(_bufferGraphics);
            e.Graphics.DrawImage(_backBuffer, 0, 0);
        }

        // 绘制极简边框
        using var borderPen = new Pen(Color.FromArgb(60, 60, 80), 2);
        e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);

        base.OnPaint(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_backBuffer != null && this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
        {
            _backBuffer.Dispose();
            _backBuffer = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            if (_bufferGraphics != null)
            {
                _bufferGraphics.Dispose();
                _bufferGraphics = Graphics.FromImage(_backBuffer);
                _bufferGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                _bufferGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            }
        }
    }

    private void Render(Graphics g)
    {
        // 绘制网格背景
        DrawGrid(g);

        // 绘制粒子 (底层)
        foreach (var p in _particles)
        {
            if (p.IsActive && (p.Type == "SMOKE" || p.Type == "RING"))
            {
                DrawParticle(g, p);
            }
        }

        // 绘制矿物
        foreach (var m in _minerals)
        {
            if (m.IsActive) m.Draw(g);
        }

        // 绘制怪物
        foreach (var monster in _monsters)
        {
            if (monster.IsActive)
            {
                MonsterRenderer.DrawMonster(g, monster);
            }
        }

        // 绘制投射物
        foreach (var p in _projectiles)
        {
            if (p.IsActive)
            {
                DrawProjectile(g, p);
            }
        }

        // 绘制治疗连接线
        foreach (var robot in _robots)
        {
            if (robot.IsVisible && robot.ClassType == RobotClass.Healer && robot.HealingTargets.Count > 0)
            {
                float sx = robot.X + robot.Size / 2;
                float sy = robot.Y + robot.Size / 2;
                using var pen = new Pen(Color.FromArgb(150, 50, 255, 100), 2);
                foreach (var target in robot.HealingTargets)
                {
                    if (target.IsActive && !target.IsDead)
                    {
                        float tx = target.X + target.Size / 2;
                        float ty = target.Y + target.Size / 2;
                        g.DrawLine(pen, sx, sy, tx, ty);
                    }
                }
            }
        }

        // 绘制机器人
        foreach (var robot in _robots)
        {
            if (robot.IsVisible)
            {
                DrawRobot(g, robot);
            }
        }

        // 绘制粒子 (顶层)
        foreach (var p in _particles)
        {
            if (p.IsActive && p.Type != "SMOKE" && p.Type != "RING")
            {
                DrawParticle(g, p);
            }
        }

        // 绘制浮动文字
        foreach (var ft in _floatingTexts)
        {
            if (ft.IsActive)
            {
                using var brush = new SolidBrush(Color.FromArgb((int)(255 * (ft.Life / (float)ft.MaxLife)), ft.TextColor));
                using var font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
                g.DrawString(ft.Text, font, brush, ft.X, ft.Y);
            }
        }

        // 绘制选中指示器
        if (_selectedRobot != null && _selectedRobot.IsActive && !_selectedRobot.IsDead)
        {
            DrawSelectionRing(g, _selectedRobot);
        }

        if (_selectedMonster != null && _selectedMonster.IsActive && !_selectedMonster.IsDead)
        {
            DrawSelectionRing(g, _selectedMonster);
        }

        // 绘制资源UI
        DrawResourceUI(g);

        // 绘制游戏结束覆盖层
        if (_isGameEnding)
        {
            DrawGameEndingOverlay(g);
        }

        // 绘制鼠标放置预览
        if (_isSpawningMonster)
        {
            using var brush = new SolidBrush(Color.FromArgb(100, Color.Red));
            g.FillEllipse(brush, _monsterSpawnPoint.X - 40, _monsterSpawnPoint.Y - 40, 80, 80);
            using var pen = new Pen(Color.Red, 2);
            g.DrawEllipse(pen, _monsterSpawnPoint.X - 40, _monsterSpawnPoint.Y - 40, 80, 80);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // 只有在按下 Alt 键时才触发办公快捷键，避免与浏览器内的正常输入冲突
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
            // Alt + B: 切换浏览器
            else if (baseKey == Keys.B)
            {
                if (_browserPanel != null)
                {
                    _isBrowserVisible = !_isBrowserVisible;
                    _browserPanel.Visible = _isBrowserVisible;
                    if (_isBrowserVisible)
                    {
                        _browserPanel.BringToFront();
                        _webView?.Focus();
                    }
                    else
                    {
                        this.Focus();
                    }
                }
                return true;
            }
            // Alt + Q: 退出当前游戏，返回启动器
            else if (baseKey == Keys.Q)
            {
                ReturnToHome();
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BattleForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl + N: 添加新机器人
        if (e.Control && e.KeyCode == Keys.N)
        {
            string name = $"机器人-{_robotIdCounter}";
            var robot = SpawnRobot(name, -1, -1);
            if (_robots.Count > 5) robot.IsVisible = false;
        }
        // Ctrl + M: 添加怪物（或点击模式）
        else if (e.Control && e.KeyCode == Keys.M)
        {
            _isSpawningMonster = !_isSpawningMonster;
            if (_isSpawningMonster)
                this.Cursor = Cursors.Cross;
            else
                this.Cursor = Cursors.Default;
        }
        // Ctrl + R: 重置游戏
        else if (e.Control && e.KeyCode == Keys.R)
        {
            ResetGame();
        }
        // Escape: 取消怪物放置
        else if (e.KeyCode == Keys.Escape)
        {
            _isSpawningMonster = false;
            this.Cursor = Cursors.Default;
        }
    }

    private void BattleForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_isSpawningMonster)
            {
                // 放置怪物
                var monster = new Monster(e.X - 48, e.Y - 48);
                _monsters.Add(monster);

                // 所有机器人立即攻击这个怪物
                foreach (var robot in _robots)
                {
                    if (robot.IsActive && !robot.IsDead)
                    {
                        robot.SetMonsterTarget(monster);
                    }
                }

                _isSpawningMonster = false;
                this.Cursor = Cursors.Default;
            }
            else
            {
                // 选择机器人/怪物
                _selectedRobot = null;
                _selectedMonster = null;

                foreach (var robot in _robots)
                {
                    if (robot.IsActive && !robot.IsDead && robot.HitTest(e.X, e.Y))
                    {
                        _selectedRobot = robot;
                        break;
                    }
                }

                if (_selectedRobot == null)
                {
                    foreach (var monster in _monsters)
                    {
                        if (monster.IsActive && !monster.IsDead)
                        {
                            if (e.X >= monster.X && e.X <= monster.X + monster.Size &&
                                e.Y >= monster.Y && e.Y <= monster.Y + monster.Size)
                            {
                                _selectedMonster = monster;
                                break;
                            }
                        }
                    }
                }
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            // 右键：让选中的机器人朝鼠标位置发射
            if (_selectedRobot != null && _selectedRobot.IsActive && !_selectedRobot.IsDead)
            {
                _selectedRobot.SetMonsterTarget(null);
                _selectedRobot.LaunchRemoteAttackAtPosition(e.X, e.Y);
            }
        }
    }

    private void BattleForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isSpawningMonster)
        {
            _monsterSpawnPoint = e.Location;
        }
    }

    private void BattleForm_MouseUp(object? sender, MouseEventArgs e)
    {
    }

    private Robot SpawnRobot(string name, float x, float y, RobotClass classType = RobotClass.Shooter)
    {
        if (x < 0) x = _rand.Next(50, this.ClientSize.Width - 100);
        if (y < 0) y = _rand.Next(50, this.ClientSize.Height - 150);

        var robot = new Robot(_robotIdCounter++, name, x, y, classType);
        _robots.Add(robot);
        return robot;
    }

    // 供 Robot 调用的方法
    public void AddProjectile(Projectile p)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => _projectiles.Add(p)));
        }
        else
        {
            _projectiles.Add(p);
        }
    }

    public IEnumerable<Robot> GetRobots()
    {
        return _robots;
    }

    public IEnumerable<Mineral> GetMinerals()
    {
        return _minerals;
    }

    public void RemoveMineral(Mineral m)
    {
        _minerals.Remove(m);
    }

    public int ScreenWidth => this.ClientSize.Width;
    public int ScreenHeight => this.ClientSize.Height;

    private bool CheckCollision(Projectile p, Robot r)
    {
        // 点检测：弹丸中心是否在机器人矩形内
        return p.X >= r.X && p.X <= r.X + r.Size &&
               p.Y >= r.Y && p.Y <= r.Y + r.Size;
    }

    private bool CheckCollision(Robot r1, Robot r2)
    {
        // 两个矩形之间的碰撞
        return r1.X < r2.X + r2.Size &&
               r1.X + r1.Size > r2.X &&
               r1.Y < r2.Y + r2.Size &&
               r1.Y + r1.Size > r2.Y;
    }

    private bool CheckCollision(Projectile p, Monster m)
    {
        // 弹丸与怪物的碰撞
        return p.X < m.X + m.Size &&
               p.X + 4 > m.X &&
               p.Y < m.Y + m.Size &&
               p.Y + 4 > m.Y;
    }

    private int GetProjectileDamage(string type)
    {
        // 伤害计算已移至 Robot.cs 中的对应方法
        return 10;
    }

    private void HandleAllCollisions()
    {
        // 1. 机器人之间
        for (int i = 0; i < _robots.Count; i++)
        {
            var r1 = _robots[i];
            if (!r1.IsActive || r1.IsDead) continue;
            for (int j = i + 1; j < _robots.Count; j++)
            {
                var r2 = _robots[j];
                if (!r2.IsActive || r2.IsDead) continue;

                float c1x = r1.X + r1.Size / 2f;
                float c1y = r1.Y + r1.Size / 2f;
                float c2x = r2.X + r2.Size / 2f;
                float c2y = r2.Y + r2.Size / 2f;

                float dx = c2x - c1x;
                float dy = c2y - c1y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float minDist = (r1.Size + r2.Size) / 2f * 0.9f; // 稍微给点容差，0.9倍半径

                if (dist < minDist)
                {
                    if (dist == 0) { dx = 1; dy = 0; dist = 1; }
                    float overlap = minDist - dist;
                    float pushX = (dx / dist) * overlap * 0.5f;
                    float pushY = (dy / dist) * overlap * 0.5f;

                    bool f1 = r1.ClassType == RobotClass.Base;
                    bool f2 = r2.ClassType == RobotClass.Base;

                    if (f1 && !f2) { r2.X += pushX * 2; r2.Y += pushY * 2; }
                    else if (!f1 && f2) { r1.X -= pushX * 2; r1.Y -= pushY * 2; }
                    else if (!f1 && !f2) { r1.X -= pushX; r1.Y -= pushY; r2.X += pushX; r2.Y += pushY; }
                }
            }
        }

        // 2. 怪物之间
        for (int i = 0; i < _monsters.Count; i++)
        {
            var m1 = _monsters[i];
            if (!m1.IsActive || m1.IsDead) continue;
            for (int j = i + 1; j < _monsters.Count; j++)
            {
                var m2 = _monsters[j];
                if (!m2.IsActive || m2.IsDead) continue;

                float c1x = m1.X + m1.Size / 2f;
                float c1y = m1.Y + m1.Size / 2f;
                float c2x = m2.X + m2.Size / 2f;
                float c2y = m2.Y + m2.Size / 2f;

                float dx = c2x - c1x;
                float dy = c2y - c1y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float minDist = (m1.Size + m2.Size) / 2f * 0.8f; // 怪物允许稍微重叠一点点，避免卡死

                if (dist < minDist)
                {
                    if (dist == 0) { dx = 1; dy = 0; dist = 1; }
                    float overlap = minDist - dist;
                    float pushX = (dx / dist) * overlap * 0.5f;
                    float pushY = (dy / dist) * overlap * 0.5f;
                    m1.X -= pushX; m1.Y -= pushY;
                    m2.X += pushX; m2.Y += pushY;
                }
            }
        }

        // 3. 机器人与怪物之间
        for (int i = 0; i < _robots.Count; i++)
        {
            var r = _robots[i];
            if (!r.IsActive || r.IsDead) continue;
            for (int j = 0; j < _monsters.Count; j++)
            {
                var m = _monsters[j];
                if (!m.IsActive || m.IsDead) continue;

                float c1x = r.X + r.Size / 2f;
                float c1y = r.Y + r.Size / 2f;
                float c2x = m.X + m.Size / 2f;
                float c2y = m.Y + m.Size / 2f;

                float dx = c2x - c1x;
                float dy = c2y - c1y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float minDist = (r.Size + m.Size) / 2f * 0.85f;

                if (dist < minDist)
                {
                    if (dist == 0) { dx = 1; dy = 0; dist = 1; }
                    float overlap = minDist - dist;
                    float pushX = (dx / dist) * overlap * 0.5f;
                    float pushY = (dy / dist) * overlap * 0.5f;

                    if (r.ClassType == RobotClass.Base)
                    {
                        // 基地不动，怪物全吃反弹
                        m.X += pushX * 2;
                        m.Y += pushY * 2;
                    }
                    else
                    {
                        // 互相推开
                        r.X -= pushX;
                        r.Y -= pushY;
                        m.X += pushX;
                        m.Y += pushY;
                    }
                }
            }
        }
    }

    private void CheckGameEnd()
    {
        int aliveRobots = _robots.Count(r => r.IsActive && !r.IsDead && r.ClassType != RobotClass.Base);
        bool baseDead = _robots.Any(r => r.ClassType == RobotClass.Base && (r.IsDead || !r.IsActive));

        // 游戏失败条件：所有机器人死亡，或基地被摧毁
        if ((aliveRobots == 0 && _robots.Count(r => r.ClassType != RobotClass.Base) > 0) || baseDead)
        {
            _isGameEnding = true;
            if (_resetTimer == 0) _resetTimer = 300;
        }
    }

    private void UpdateBaseWave()
    {
        var baseRobot = GetBaseRobot();
        if (baseRobot == null) return;

        _baseWaveTimer++;
        
        // 1. 蓄力视觉效果优化
        if (_baseWaveTimer > 300) // 最后 1 秒蓄力
        {
            float baseX = baseRobot.X + baseRobot.Size / 2f;
            float baseY = baseRobot.Y + baseRobot.Size / 2f;
            
            // 产生吸入粒子
            if (_rand.Next(2) == 0)
            {
                float angle = (float)(_rand.NextDouble() * Math.PI * 2);
                float dist = 120 - (_baseWaveTimer - 300) * 1.8f;
                if (dist < 10) dist = 10;
                float px = baseX + (float)Math.Cos(angle) * dist;
                float py = baseY + (float)Math.Sin(angle) * dist;
                AddExplosion(px, py, Color.FromArgb(200, Color.Cyan), 1, "SPARK");
            }
            
            // 基地核心大幅脉动缩放
            // 绘制逻辑在 DrawRobot 中利用 _baseWaveTimer 决定核心发光半径 (此处无需逻辑，DrawRobot 自动关联)
        }

        if (_baseWaveTimer >= 360) // 达到触发阈值
        {
            _baseWaveTimer = 0;

            // 获取参数
            float waveRadius = 200f + (_baseLevel - 1) * 20f;
            float pushForce = 35f + (_baseLevel - 1) * 5f;
            int waveDamage = 35 + (_baseLevel - 1) * 15;

            float baseX = baseRobot.X + baseRobot.Size / 2f;
            float baseY = baseRobot.Y + baseRobot.Size / 2f;

            // 爆发视觉特效
            AddExplosion(baseX, baseY, Color.White, 35, "RING"); // 增加白色闪光环
            AddExplosion(baseX, baseY, Color.Cyan, 30, "SPARK");
            AddExplosion(baseX, baseY, Color.Blue, 10, "SMOKE");

            // 震动处理（如果有此功能）
            
            // 影响范围内的怪物
            foreach (var monster in _monsters)
            {
                if (!monster.IsActive || monster.IsDead) continue;

                var (mx, my) = monster.GetCenter();
                float dx = mx - baseX;
                float dy = my - baseY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (dist <= waveRadius)
                {
                    monster.TakeDamage(waveDamage);
                    if (dist < 1f) dist = 1f;
                    monster.Dx += (dx / dist) * pushForce;
                    monster.Dy += (dy / dist) * pushForce;
                }
            }
        }
    }

    private void HandleWaveLogic()
    {
        // 如果当前没有怪物，且也没有待生成的怪物，进入倒计时
        if (_monsters.Count(m => m.IsActive && !m.IsDead) == 0 && _monstersToSpawnInWave <= 0)
        {
            if (_waveTimer <= 0)
            {
                // 开始新的一波
                _monstersToSpawnInWave = 3 + CurrentWave * 2; // 每波怪物递增
                _spawnInterval = 0;
            }
            else
            {
                _waveTimer--;
            }
        }

        // 生成怪物逻辑
        if (_monstersToSpawnInWave > 0)
        {
            _spawnInterval--;
            if (_spawnInterval <= 0)
            {
                // 在边缘生成怪物
                int edge = _rand.Next(4); // 0:上 1:右 2:下 3:左
                int spawnX = 0, spawnY = 0;

                switch (edge)
                {
                    case 0: spawnX = _rand.Next(this.ClientSize.Width); spawnY = -30; break;
                    case 1: spawnX = this.ClientSize.Width + 30; spawnY = _rand.Next(this.ClientSize.Height); break;
                    case 2: spawnX = _rand.Next(this.ClientSize.Width); spawnY = this.ClientSize.Height + 30; break;
                    case 3: spawnX = -30; spawnY = _rand.Next(this.ClientSize.Height); break;
                }

                var monster = new Monster(spawnX, spawnY);
                // 根据波次增加怪物血量，初始第1波血量为 100
                monster.MaxHP = 80 + CurrentWave * 20;
                monster.HP = monster.MaxHP;

                // 每10波出一个大Boss
                if (CurrentWave % 10 == 0 && _monstersToSpawnInWave == 1)
                {
                    monster.MaxHP *= 10;
                    monster.HP = monster.MaxHP;
                    monster.Size = 80;
                }

                _monsters.Add(monster);

                _monstersToSpawnInWave--;
                _spawnInterval = 60; // 1秒生成一个

                // 告诉所有机器人有新怪物了
                foreach (var robot in _robots)
                    if (robot.IsActive && !robot.IsDead) robot.SetMonsterTarget(monster);

                if (_monstersToSpawnInWave <= 0)
                {
                    // 这波刷完了，准备下一波的计时
                    CurrentWave++;
                    _waveTimer = 600; // 10秒后下一波
                }
            }
        }
    }

    private void HandleGameEnding()
    {
        _resetTimer--;
        if (_resetTimer <= 0)
        {
            ResetGame();
        }
    }

    private void ResetGame()
    {
        _robots.Clear();
        _monsters.Clear();
        _projectiles.Clear();
        _isGameEnding = false;
        _winner = null;
        _robotIdCounter = 1;

        Gold = 2000;
        Minerals = 500;
        CurrentWave = 1;
        _waveTimer = 600;
        _monstersToSpawnInWave = 0;

        // 重新生成基地
        var baseRobot = SpawnRobot("主基地", this.ClientSize.Width / 2 - 20, this.ClientSize.Height / 2 - 20, RobotClass.Base);
        baseRobot.ApplyClassProperties();

        // 重新生成初始机器人 (1个治疗，2个输出)
        var def = SpawnRobot("医疗兵", this.ClientSize.Width / 2, this.ClientSize.Height / 2 + 50, RobotClass.Healer);
        def.ApplyClassProperties();

        var atk1 = SpawnRobot("游侠A", this.ClientSize.Width / 2 - 60, this.ClientSize.Height / 2 + 80);
        var atk2 = SpawnRobot("游侠B", this.ClientSize.Width / 2 + 20, this.ClientSize.Height / 2 + 80);
    }

    public void AddFloatingText(float x, float y, string text, Color color)
    {
        _floatingTexts.Add(new FloatingText
        {
            X = x,
            Y = y,
            Dy = -1.5f - (float)_rand.NextDouble(),
            Text = text,
            TextColor = color,
            Life = 40,
            MaxLife = 40
        });
    }

    public void AddExplosion(float x, float y, Color color, int count = 10, string type = "SPARK")
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_rand.NextDouble() * Math.PI * 2);
            float speed = (float)(_rand.NextDouble() * 5 + 1);
            float size = (float)(_rand.NextDouble() * 4 + 2);
            int life = _rand.Next(20, 60);

            if (type == "SMOKE")
            {
                speed *= 0.5f;
                size *= 2;
                life += 30;
                color = Color.FromArgb(100, Color.Gray);
            }
            else if (type == "RING")
            {
                _particles.Add(new Particle(x, y, 0, 0, color, 30, 10, "RING"));
                return; // Ring is single particle
            }
            else if (type == "SPARK")
            {
                speed *= 1.5f;
                life = _rand.Next(10, 30);
            }

            _particles.Add(new Particle(x, y, (float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed, color, life, size, type));
        }
    }

    private void DrawParticle(Graphics g, Particle p)
    {
        int alpha = (int)((float)p.Life / p.MaxLife * 255);
        if (alpha > 255) alpha = 255;
        if (alpha < 0) alpha = 0;

        using var brush = new SolidBrush(Color.FromArgb(alpha, p.Color));

        if (p.Type == "RING")
        {
            using var pen = new Pen(Color.FromArgb(alpha, p.Color), 3);
            g.DrawEllipse(pen, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
        }
        else
        {
            g.FillEllipse(brush, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
        }
    }

    private void DrawResourceUI(Graphics g)
    {
        // 顶部资源栏背景
        using var bgBrush = new SolidBrush(Color.FromArgb(180, 20, 20, 25));
        g.FillRectangle(bgBrush, 0, 0, this.ClientSize.Width, 35);

        using var borderPen = new Pen(Color.FromArgb(100, 60, 60, 80), 1);
        g.DrawLine(borderPen, 0, 35, this.ClientSize.Width, 35);

        using var font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
        using var waveFont = new Font("Microsoft YaHei", 12, FontStyle.Bold);

        // 金币
        using var goldBrush = new SolidBrush(Color.Gold);
        g.DrawString($"💰 {Gold}", font, goldBrush, 20, 8);

        // 星矿
        using var mineralBrush = new SolidBrush(Color.Cyan);
        g.DrawString($"💎 {Minerals}", font, mineralBrush, 120, 8);

        // 波次信息居中
        using var textBrush = new SolidBrush(Color.White);
        string waveText = _monstersToSpawnInWave > 0 ? $"第 {CurrentWave} 波 - 怪物涌入中" :
                          _waveTimer > 0 ? $"第 {CurrentWave} 波 - {(_waveTimer / 60)}s 后开始" :
                          $"第 {CurrentWave} 波 - 战斗中";

        var size = g.MeasureString(waveText, waveFont);
        g.DrawString(waveText, waveFont, textBrush, (this.ClientSize.Width - size.Width) / 2, 6);
    }

    private void DrawGrid(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 80), 1);
        int gridSize = 50;

        for (int x = 0; x < this.ClientSize.Width; x += gridSize)
        {
            g.DrawLine(pen, x, 0, x, this.ClientSize.Height);
        }
        for (int y = 0; y < this.ClientSize.Height; y += gridSize)
        {
            g.DrawLine(pen, 0, y, this.ClientSize.Width, y);
        }
    }

    private void DrawProjectile(Graphics g, Projectile p)
    {
        Color color = p.Type switch
        {
            "ROCKET" => Color.OrangeRed,
            "CANNON" => Color.DarkGray,
            "PLASMA" => Color.Cyan,
            "LIGHTNING" => Color.Yellow,
            "SPIT" => Color.Green,
            "INK" => Color.Black,
            "METEOR" => Color.Orange,
            "BLACK_HOLE" => Color.Purple,
            "DEATH_RAY" => Color.Red,
            _ => p.ProjectileColor
        };

        using var brush = new SolidBrush(color);

        // 绘制弹丸
        float size = p.Type switch
        {
            "CANNON" => 10,
            "LIGHTNING" => 3,
            "ROCKET" => 8,
            "METEOR" => 20,
            "BLACK_HOLE" => 15,
            "DEATH_RAY" => 40,
            _ => 6
        };

        if (p.Type == "DEATH_RAY")
        {
            // 绘制超级粗大的激光束
            if (p.Owner != null)
            {
                float startX = p.Owner.X + p.Owner.Size / 2;
                float startY = p.Owner.Y + p.Owner.Size / 2;

                // 核心光束
                using var laserPen = new Pen(Color.Red, 20 + (float)Math.Sin(Environment.TickCount / 20.0) * 5);
                g.DrawLine(laserPen, startX, startY, p.X, p.Y);

                // 外围光晕
                using var glowPen = new Pen(Color.FromArgb(100, Color.OrangeRed), 40);
                g.DrawLine(glowPen, startX, startY, p.X, p.Y);

                // 粒子效果
                if (_rand.Next(10) < 5)
                {
                    AddExplosion(p.X, p.Y, Color.Red, 2, "SPARK");
                }
            }
        }
        else if (p.Type == "BLACK_HOLE")
        {
            // 黑洞特效：吸入效果
            using var bhPen = new Pen(Color.Purple, 2);
            float angle = (Environment.TickCount / 50f) % (float)(Math.PI * 2);
            g.DrawArc(bhPen, p.X - 15, p.Y - 15, 30, 30, angle * 180 / (float)Math.PI, 270);
            g.FillEllipse(Brushes.Black, p.X - 8, p.Y - 8, 16, 16);

            // 粒子吸入
            if (_rand.Next(100) < 30)
            {
                // 添加吸入粒子视觉效果（不增加实体粒子，直接画）
                for (int i = 0; i < 3; i++)
                {
                    float pAngle = (float)(_rand.NextDouble() * Math.PI * 2);
                    float pDist = 30 + (float)_rand.NextDouble() * 20;
                    float px = p.X + (float)Math.Cos(pAngle) * pDist;
                    float py = p.Y + (float)Math.Sin(pAngle) * pDist;
                    using var pPen = new Pen(Color.Violet, 1);
                    g.DrawLine(pPen, px, py, p.X, p.Y);
                }
            }
        }
        else if (p.Type == "METEOR")
        {
            // 陨石特效：火焰尾迹
            using var fireBrush = new SolidBrush(Color.FromArgb(150, Color.OrangeRed));
            g.FillEllipse(fireBrush, p.X - size / 2 - 5, p.Y - size / 2 - 5, size + 10, size + 10);
            g.FillEllipse(brush, p.X - size / 2, p.Y - size / 2, size, size);

            // 随机生成尾迹粒子
            if (_rand.Next(10) < 5)
            {
                AddExplosion(p.X, p.Y, Color.Orange, 1, "SMOKE");
            }
        }
        else
        {
            g.FillEllipse(brush, p.X - size / 2, p.Y - size / 2, size, size);
        }

        // 特殊效果
        if (p.Type == "LIGHTNING")
        {
            var r = new Random();
            using var arcPen = new Pen(Color.White, 1);
            float angle = (float)Math.Atan2(p.Dy, p.Dx);
            for (int i = 0; i < 3; i++)
            {
                float offset = (float)(r.NextDouble() - 0.5) * 20;
                float len = size + r.Next(10, 20);
                float jitter = (float)(r.NextDouble() - 0.5) * 10;
                g.DrawLine(arcPen,
                    p.X + (float)Math.Cos(angle + offset) * size,
                    p.Y + (float)Math.Sin(angle + offset) * size,
                    p.X + (float)Math.Cos(angle) * (size + len) + jitter,
                    p.Y + (float)Math.Sin(angle) * (size + len) + jitter);
            }
        }
    }

    private void DrawSelectionRing(Graphics g, Robot robot)
    {
        using var pen = new Pen(Color.Yellow, 2);
        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        g.DrawEllipse(pen, robot.X - 5, robot.Y - 5, robot.Size + 10, robot.Size + 10);
    }

    private void DrawSelectionRing(Graphics g, Monster monster)
    {
        using var pen = new Pen(Color.Red, 2);
        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        g.DrawEllipse(pen, monster.X - 5, monster.Y - 5, monster.Size + 10, monster.Size + 10);
    }

    private void DrawGameEndingOverlay(Graphics g)
    {
        using var brush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        g.FillRectangle(brush, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

        using var font = new Font("Microsoft YaHei", 36, FontStyle.Bold);
        using var largeFont = new Font("Microsoft YaHei", 48, FontStyle.Bold);
        using var whiteBrush = new SolidBrush(Color.White);

        string title = "战斗结束";
        SizeF titleSize = g.MeasureString(title, largeFont);
        g.DrawString(title, largeFont, whiteBrush,
            (this.ClientSize.Width - titleSize.Width) / 2,
            this.ClientSize.Height / 2 - 100);

        if (_winner != null)
        {
            string winText = $"{_winner.Name} 获胜！";
            SizeF winSize = g.MeasureString(winText, font);
            g.DrawString(winText, font, whiteBrush,
                (this.ClientSize.Width - winSize.Width) / 2,
                this.ClientSize.Height / 2);
        }

        string resetText = $"{(int)Math.Ceiling(_resetTimer / 60f)} 秒后重置...";
        SizeF resetSize = g.MeasureString(resetText, font);
        g.DrawString(resetText, font, whiteBrush,
            (this.ClientSize.Width - resetSize.Width) / 2,
            this.ClientSize.Height / 2 + 80);
    }

    private void CreateControlPanel()
    {
        int panelWidth = 495; // 增加宽度容纳基地升级
        int btnWidth = 80;
        int btnHeight = 30;
        int spacing = 15;

        var panel = new FlickerFreePanel
        {
            Name = "ControlPanel",
            Size = new Size(panelWidth, 70), // 高度降低
            Location = new Point((this.ClientSize.Width - panelWidth) / 2, this.ClientSize.Height - 75), // 更贴近底部
            BackColor = Color.FromArgb(20, 20, 25),
            BorderStyle = BorderStyle.None,
            Anchor = AnchorStyles.Bottom
        };

        // 按钮样式生成器
        Button CreateBtn(string id, string text, int x, int y, EventHandler onClick, bool isUpgrade = false)
        {
            var btn = new Button
            {
                Name = id,
                Text = text,
                Location = new Point(x, y),
                Size = new Size(btnWidth, isUpgrade ? 20 : btnHeight), // 升级按钮更扁
                FlatStyle = FlatStyle.Flat,
                ForeColor = isUpgrade ? Color.Cyan : Color.White,
                BackColor = Color.FromArgb(40, 40, 50),
                Font = new Font("Microsoft YaHei", 7, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 90);
            btn.Click += onClick;
            return btn;
        }

        int startX = 10;

        // 0. 基地升级 (无购买按钮，只有一个占位或恢复血量)
        int baseUpgradeCost = 150 * _baseLevel;
        panel.Controls.Add(CreateBtn("UpgBase", $"Lv.{_baseLevel} 💎{baseUpgradeCost}", startX, 5, (s, e) =>
        {
            int cost = 150 * _baseLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _baseLevel++;
                var b = GetBaseRobot();
                if (b != null)
                {
                    b.MaxHP = 3000 + (_baseLevel - 1) * 1000;
                    b.HP = b.MaxHP; // 升级顺便回满血
                }
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("HealBase", $"维修基地\n💰100", startX, 30, (s, e) =>
        {
            if (Gold >= 100)
            {
                var b = GetBaseRobot();
                if (b != null && b.HP < b.MaxHP)
                {
                    Gold -= 100;
                    b.HP = Math.Min(b.MaxHP, b.HP + 500);
                    AddFloatingText(b.X + b.Size / 2, b.Y - 10, "+500", Color.LimeGreen);
                    UpdateUI();
                }
            }
        }));

        // 1. 采集工
        int workerStartX = startX + btnWidth + spacing;
        int workerUpgradeCost = 50 * _workerLevel;
        panel.Controls.Add(CreateBtn("UpgWorker", $"Lv.{_workerLevel} 💎{workerUpgradeCost}", workerStartX, 5, (s, e) =>
        {
            int cost = 50 * _workerLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _workerLevel++;
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyWorker", $"采集工\n💰{_workerCost}", workerStartX, 30, (s, e) =>
        {
            if (Gold >= _workerCost)
            {
                Gold -= _workerCost;
                var r = SpawnRobot("采集者", -1, -1, RobotClass.Worker);
                r.ApplyClassProperties();
                _workerCost = (int)(_workerCost * 1.2f);
                UpdateUI();
            }
        }));

        // 2. 治疗者
        int healerStartX = workerStartX + btnWidth + spacing;
        int healerUpgradeCost = 80 * _healerLevel;
        panel.Controls.Add(CreateBtn("UpgHealer", $"Lv.{_healerLevel} 💎{healerUpgradeCost}", healerStartX, 5, (s, e) =>
        {
            int cost = 80 * _healerLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _healerLevel++;
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyHealer", $"治疗者\n💰{_defenderCost}", healerStartX, 30, (s, e) =>
        {
            if (Gold >= _defenderCost)
            {
                Gold -= _defenderCost;
                var r = SpawnRobot("守护者", -1, -1, RobotClass.Healer);
                r.ApplyClassProperties();
                _defenderCost = (int)(_defenderCost * 1.3f);
                UpdateUI();
            }
        }));

        // 3. 攻击者
        int shooterStartX = healerStartX + btnWidth + spacing;
        int shooterUpgradeCost = 60 * _shooterLevel;
        panel.Controls.Add(CreateBtn("UpgShooter", $"Lv.{_shooterLevel} 💎{shooterUpgradeCost}", shooterStartX, 5, (s, e) =>
        {
            int cost = 60 * _shooterLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _shooterLevel++;
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyShooter", $"攻击者\n💰{_shooterCost}", shooterStartX, 30, (s, e) =>
        {
            if (Gold >= _shooterCost)
            {
                Gold -= _shooterCost;
                var r = SpawnRobot("游侠", -1, -1, RobotClass.Shooter);
                r.ApplyClassProperties();
                _shooterCost = (int)(_shooterCost * 1.25f);
                UpdateUI();
            }
        }));

        // 4. 守卫者
        int guardianStartX = shooterStartX + btnWidth + spacing;
        int guardianUpgradeCost = 100 * _guardianLevel;
        panel.Controls.Add(CreateBtn("UpgGuardian", $"Lv.{_guardianLevel} 💎{guardianUpgradeCost}", guardianStartX, 5, (s, e) =>
        {
            int cost = 100 * _guardianLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _guardianLevel++;
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyGuardian", $"守卫者\n💰{_guardianCost}", guardianStartX, 30, (s, e) =>
        {
            if (Gold >= _guardianCost)
            {
                Gold -= _guardianCost;
                var r = SpawnRobot("守卫者", -1, -1, RobotClass.Guardian);
                r.ApplyClassProperties();
                _guardianCost = (int)(_guardianCost * 1.35f);
                UpdateUI();
            }
        }));

        this.Controls.Add(panel);

        // 支持拖拽窗口
        this.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        };
    }

    // 拖拽窗口所需
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HT_CAPTION = 0x2;

    private void UpdateUI()
    {
        var panel = this.Controls["ControlPanel"] as Panel;
        if (panel == null) return;

        // 更新按钮文字和价格
        if (panel.Controls["UpgBase"] is Button uB) uB.Text = $"Lv.{_baseLevel} 💎{150 * _baseLevel}";
        // HealBase 不变

        if (panel.Controls["UpgWorker"] is Button uW) uW.Text = $"Lv.{_workerLevel} 💎{50 * _workerLevel}";
        if (panel.Controls["BuyWorker"] is Button btnW) btnW.Text = $"采集工\n💰{_workerCost}";

        if (panel.Controls["UpgHealer"] is Button uH) uH.Text = $"Lv.{_healerLevel} 💎{80 * _healerLevel}";
        if (panel.Controls["BuyHealer"] is Button btnD) btnD.Text = $"治疗者\n💰{_defenderCost}";

        if (panel.Controls["UpgShooter"] is Button uS) uS.Text = $"Lv.{_shooterLevel} 💎{60 * _shooterLevel}";
        if (panel.Controls["BuyShooter"] is Button btnS) btnS.Text = $"攻击者\n💰{_shooterCost}";

        if (panel.Controls["UpgGuardian"] is Button uG) uG.Text = $"Lv.{_guardianLevel} 💎{100 * _guardianLevel}";
        if (panel.Controls["BuyGuardian"] is Button btnG) btnG.Text = $"守卫者\n💰{_guardianCost}";
    }

    /// <summary>
    /// 绘制机器人 - 简化版本
    /// </summary>
    private void DrawRobot(Graphics g, Robot robot)
    {
        float x = robot.X;
        float y = robot.Y;
        int size = robot.Size;

        if (robot.IsDead)
        {
            using var deadBrush = new SolidBrush(Color.Gray);
            g.FillEllipse(deadBrush, x, y, size, size);
            return;
        }

        float centerX = x + size / 2;
        float centerY = y + size / 2;

        if (robot.ClassType == RobotClass.Base)
        {
            // 绘制基地的特殊外观
            using var baseBodyBrush = new SolidBrush(robot.PrimaryColor);
            using var baseDarkBrush = new SolidBrush(robot.SecondaryColor);

            // 底座
            g.FillRectangle(baseDarkBrush, x - 5, y + size - 10, size + 10, 15);
            // 主体
            g.FillRectangle(baseBodyBrush, x, y, size, size);
            // 核心发光
            using (var baseCoreBrush = new SolidBrush(Color.Cyan))
            {
                float basePulse = 1 + (float)Math.Sin(Environment.TickCount / 200.0) * 0.2f;
                g.FillEllipse(baseCoreBrush, centerX - 10 * basePulse, centerY - 10 * basePulse, 20 * basePulse, 20 * basePulse);
            }

            // 不绘制眼睛和触手，直接画名字和血条等
        }
        else
        {
            // 触手
            DrawTentacles(g, robot, centerX, centerY);

            // 身体
            using var bodyBrush = new SolidBrush(robot.PrimaryColor);
            using var darkBrush = new SolidBrush(robot.SecondaryColor);
            g.FillEllipse(bodyBrush, x, y, size, size);
            for (int dx = -8; dx <= 8; dx += 4)
            {
                for (int dy = -8; dy <= 8; dy += 4)
                {
                    if (dx * dx + dy * dy <= 64 && (dx != 0 || dy != 0))
                    {
                        g.FillRectangle(darkBrush, x + size / 2 + dx, y + size / 2 + dy, 4, 4);
                    }
                }
            }

            // 核心
            using var coreBrush = new SolidBrush(Color.White);
            float pulse = 1 + (float)Math.Sin(Environment.TickCount / 200.0) * 0.2f;
            g.FillEllipse(coreBrush, centerX - 4 * pulse, centerY + 2 - 3 * pulse, 8 * pulse, 6 * pulse);

            // 眼睛
            DrawEyes(g, robot, centerX, centerY);

            // 天线
            DrawAntennas(g, robot, centerX, centerY);
        }

        // --- 血条优化显示 ---
        if (!robot.IsDead)
        {
            if (robot.ClassType == RobotClass.Base)
            {
                // 基地专用华丽血条 (始终显示)
                float barWidth = 140; 
                float barHeight = 12;
                float barX = x + (size - barWidth) / 2;
                float barY = y - 30;

                // 背景（半透明底框）
                using (var bgBrush = new SolidBrush(Color.FromArgb(180, 20, 20, 25)))
                {
                    g.FillRectangle(bgBrush, barX, barY, barWidth, barHeight);
                }

                // 核心血量（能量绿色，危机红色）
                float hpPercent = (float)robot.HP / robot.MaxHP;
                Color hpColor = (hpPercent > 0.3f) ? Color.FromArgb(0, 255, 127) : Color.FromArgb(255, 60, 60);
                using (var hpBrush = new SolidBrush(hpColor))
                {
                    g.FillRectangle(hpBrush, barX + 2, barY + 2, (barWidth - 4) * Math.Clamp(hpPercent, 0, 1), barHeight - 4);
                }

                // 外部高对比边框
                using (var borderPen = new Pen(Color.White, 2))
                {
                    g.DrawRectangle(borderPen, barX, barY, barWidth, barHeight);
                }

                // 数值文本 (带有阴影)
                using (var font = new Font("Consolas", 9, FontStyle.Bold))
                {
                    string hpText = $"{robot.HP} / {robot.MaxHP}";
                    var textSize = g.MeasureString(hpText, font);
                    g.DrawString(hpText, font, Brushes.Black, barX + (barWidth - textSize.Width) / 2 + 1, barY - 14 + 1);
                    g.DrawString(hpText, font, Brushes.White, barX + (barWidth - textSize.Width) / 2, barY - 14);
                }
            }
            else if (robot.HP < robot.MaxHP)
            {
                // 普通机器人或小弟的精致血条 (受伤即显)
                float barWidth = size * 0.9f;
                float barHeight = 4;
                float barX = x + (size - barWidth) / 2;
                float barY = y - 8;

                using var bgBrush = new SolidBrush(Color.FromArgb(150, 50, 50, 50));
                g.FillRectangle(bgBrush, barX, barY, barWidth, barHeight);

                float hpPercent = (float)robot.HP / robot.MaxHP;
                Color hpColor = hpPercent > 0.5 ? Color.LimeGreen : (hpPercent > 0.2 ? Color.Gold : Color.Red);
                using var hpBrush = new SolidBrush(hpColor);
                g.FillRectangle(hpBrush, barX, barY, barWidth * Math.Clamp(hpPercent, 0, 1), barHeight);
            }
        }

        // 名字
        if (!string.IsNullOrEmpty(robot.Name))
        {
            using var font = new Font("Consolas", 8, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            using var shadowBrush = new SolidBrush(Color.Black);
            float textX = centerX;
            float textY = y - 15;

            g.DrawString(robot.Name, font, shadowBrush, textX + 1, textY + 1, new StringFormat { Alignment = StringAlignment.Center });
            g.DrawString(robot.Name, font, brush, textX, textY, new StringFormat { Alignment = StringAlignment.Center });
        }

        // 伤害数字
        if (robot.DamageTextTimer > 0 && !string.IsNullOrEmpty(robot.LastDamageText))
        {
            float alpha = Math.Min(1.0f, robot.DamageTextTimer / 30f);
            using var font = new Font("Impact", 14, FontStyle.Bold);
            using var brush = new SolidBrush(Color.FromArgb((int)(alpha * 255), Color.OrangeRed));
            using var shadowBrush = new SolidBrush(Color.FromArgb((int)(alpha * 255), Color.Black));

            float textY = y - 30 - (45 - robot.DamageTextTimer) * 1.5f;
            g.DrawString(robot.LastDamageText, font, shadowBrush, centerX + 1, textY + 1, new StringFormat { Alignment = StringAlignment.Center });
            g.DrawString(robot.LastDamageText, font, brush, centerX, textY, new StringFormat { Alignment = StringAlignment.Center });
        }

        // 激光
        if (robot.IsFiringLaser)
        {
            DrawLaserAttack(g, robot, centerX, centerY);
        }

        // 格斗特效
        if (robot.SpecialState == "SHAKING" && robot.DuelTimer > 0 && robot.DuelTarget != null)
        {
            DrawDuelEffect(g, robot, centerX, centerY);
        }
    }

    private void DrawEyes(Graphics g, Robot robot, float cx, float cy)
    {
        float eyeY = cy - 5;
        float leftEyeX = cx - 8;
        float rightEyeX = cx + 8;

        using var eyeWhiteBrush = new SolidBrush(Color.White);
        using var eyeBrush = new SolidBrush(robot.EyeColor);
        using var pupilBrush = new SolidBrush(Color.Black);

        // 左眼
        g.FillEllipse(eyeWhiteBrush, leftEyeX, eyeY, 10, 8);
        g.FillEllipse(eyeBrush, leftEyeX + 1, eyeY + 1, 6, 6);
        g.FillRectangle(pupilBrush, leftEyeX + 2, eyeY, 2, 4);

        // 右眼
        g.FillEllipse(eyeWhiteBrush, rightEyeX, eyeY, 10, 8);
        g.FillEllipse(eyeBrush, rightEyeX + 1, eyeY + 1, 6, 6);
        g.FillRectangle(pupilBrush, rightEyeX + 2, eyeY, 2, 4);
    }

    private void DrawAntennas(Graphics g, Robot robot, float cx, float cy)
    {
        using var antennaBrush = new SolidBrush(robot.SecondaryColor);

        // 左天线
        g.FillRectangle(antennaBrush, cx - 6 - 2, cy - 6, 2, 12);
        g.FillRectangle(antennaBrush, cx - 6 / 2 - 2, cy - 12 - 2, 4, 4);

        // 右天线
        g.FillRectangle(antennaBrush, cx + 6, cy - 6, 2, 12);
        g.FillRectangle(antennaBrush, cx + 6 / 2 - 2, cy - 12 - 2, 4, 4);
    }

    private void DrawTentacles(Graphics g, Robot robot, float cx, float cy)
    {
        using var tentacleBrush = new SolidBrush(robot.SecondaryColor);

        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(i * Math.PI / 4 + robot.TentacleOffsets[i] * 0.1);
            float startX = cx + (float)Math.Cos(angle) * 15;
            float startY = cy + (float)Math.Sin(angle) * 15;
            float wave = (float)Math.Sin(robot.TentacleOffsets[i] + i) * 5;
            float endX = startX + (float)Math.Cos(angle) * (20 + wave);
            float endY = startY + (float)Math.Sin(angle) * (20 + wave);

            g.FillRectangle(tentacleBrush, endX - 2, endY - 2, 4, 4);
        }
    }

    private void DrawLaserAttack(Graphics g, Robot robot, float cx, float cy)
    {
        // 只有没有激光目标时才返回
        if (!robot.IsFiringLaser) return;

        float targetCx = robot.LaserTargetX;
        float targetCy = robot.LaserTargetY;

        using var laserPen = new Pen(robot.PrimaryColor, 4);
        using var corePen = new Pen(Color.White, 2);

        g.DrawLine(laserPen, cx, cy, targetCx, targetCy);
        g.DrawLine(corePen, cx, cy, targetCx, targetCy);

        using var startGlow = new SolidBrush(Color.FromArgb(100, 255, 100, 100));
        g.FillEllipse(startGlow, cx - 8, cy - 8, 16, 16);

        using var hitGlow = new SolidBrush(Color.FromArgb(150, 255, 200, 200));
        g.FillEllipse(hitGlow, targetCx - 6, targetCy - 6, 12, 12);
    }

    private void DrawDuelEffect(Graphics g, Robot robot, float cx, float cy)
    {
        if (robot.DuelTarget == null) return;

        float targetCx = robot.DuelTarget.X + robot.DuelTarget.Size / 2;
        float targetCy = robot.DuelTarget.Y + robot.DuelTarget.Size / 2;

        using var duelPen = new Pen(Color.FromArgb(180, 255, 100, 100), 3);
        duelPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
        g.DrawLine(duelPen, cx, cy, targetCx, targetCy);

        float midX = (cx + targetCx) / 2;
        float midY = (cy + targetCy) / 2;

        using var glowBrush = new SolidBrush(Color.FromArgb(120, 255, 200, 100));
        g.FillEllipse(glowBrush, midX - 10, midY - 10, 20, 20);
    }
}



// 简化版 MonsterRenderer
public static class MonsterRenderer
{
    public static void DrawMonster(Graphics g, Monster m)
    {
        if (!m.IsActive) return;

        int size = m.Size;
        float cx = m.X + size / 2;
        float cy = m.Y + size / 2;

        switch (m.Type)
        {
            case "SPIDER":
                // 绘制蜘蛛风格
                using (var spiderBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                {
                    g.FillEllipse(spiderBrush, m.X + size * 0.1f, m.Y + size * 0.1f, size * 0.8f, size * 0.8f);
                    // 蜘蛛腿
                    using var legPen = new Pen(Color.FromArgb(60, 60, 60), 2);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = (float)(i * Math.PI / 4 + Math.Sin(m.AnimationFrame * 0.5f) * 0.2f);
                        float lx = cx + (float)Math.Cos(angle) * (size * 0.4f);
                        float ly = cy + (float)Math.Sin(angle) * (size * 0.4f);
                        float ex = cx + (float)Math.Cos(angle) * (size * 0.8f);
                        float ey = cy + (float)Math.Sin(angle) * (size * 0.8f);
                        g.DrawLine(legPen, lx, ly, ex, ey);
                    }
                }
                break;
            case "BAT":
                // 绘制蝙蝠/飞行器风格
                using (var batBrush = new SolidBrush(Color.FromArgb(80, 0, 120)))
                {
                    // 翅膀
                    float wingWave = (float)Math.Sin(m.AnimationFrame * 1.0f) * 15;
                    PointF[] points = {
                        new PointF(cx, cy),
                        new PointF(cx - size * 0.8f, cy - wingWave),
                        new PointF(cx - size * 0.5f, cy + size * 0.2f),
                        new PointF(cx + size * 0.5f, cy + size * 0.2f),
                        new PointF(cx + size * 0.8f, cy - wingWave)
                    };
                    g.FillPolygon(batBrush, points);
                    g.FillEllipse(batBrush, m.X + size * 0.25f, m.Y + size * 0.1f, size * 0.5f, size * 0.6f);
                }
                break;
            case "WORM":
                // 绘制蠕虫/机械虫风格
                using (var wormBrush = new SolidBrush(Color.FromArgb(0, 100, 50)))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float offX = (float)Math.Sin(m.AnimationFrame * 0.3f + i) * 10;
                        g.FillEllipse(wormBrush, m.X + offX + size * 0.2f, m.Y + i * (size * 0.2f), size * 0.6f, size * 0.3f);
                    }
                }
                break;
            default: // SLIME
                // 原有的红色史莱姆风格
                using (var bodyBrush = new SolidBrush(Color.FromArgb(180, 50, 50)))
                {
                    g.FillEllipse(bodyBrush, m.X, m.Y, size, size);
                    // 触手
                    using var tentacleBrush = new SolidBrush(Color.FromArgb(150, 30, 30));
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = (float)(i * Math.PI / 4 + m.AnimationFrame * 0.2);
                        float tx = cx + (float)Math.Cos(angle) * size * 0.5f;
                        float ty = cy + (float)Math.Sin(angle) * size * 0.5f;
                        g.FillEllipse(tentacleBrush, tx - 3, ty - 3, 6, 6);
                    }
                }
                break;
        }

        // 共通：眼睛
        using (var eyeBrush = new SolidBrush(Color.Yellow))
        {
            float eyeSize = size * 0.15f;
            g.FillEllipse(eyeBrush, m.X + size * 0.25f, m.Y + size * 0.3f, eyeSize, eyeSize);
            g.FillEllipse(eyeBrush, m.X + size * 0.6f, m.Y + size * 0.3f, eyeSize, eyeSize);
        }

        // 血条 (共通)
        if (m.HP < m.MaxHP)
        {
            float barWidth = size * 0.8f;
            float barHeight = 5;
            float barX = m.X + (size - barWidth) / 2;
            float barY = m.Y - 10;
            using var bgBrush = new SolidBrush(Color.Gray);
            g.FillRectangle(bgBrush, barX, barY, barWidth, barHeight);
            float hpPercent = (float)m.HP / m.MaxHP;
            using var hpBrush = new SolidBrush(Color.Red);
            g.FillRectangle(hpBrush, barX, barY, barWidth * hpPercent, barHeight);
        }

        // 受击闪烁 (共通)
        if (m.HitFlashTimer > 0)
        {
            using var flashBrush = new SolidBrush(Color.FromArgb(150, Color.White));
            g.FillEllipse(flashBrush, m.X, m.Y, size, size);
        }

        // 伤害文字 (共通)
        if (!string.IsNullOrEmpty(m.DamageText) && m.DamageTextTimer > 0)
        {
            using var font = new Font("Impact", 14, FontStyle.Bold);
            using var brush = new SolidBrush(Color.OrangeRed);
            float floatOffset = (30 - m.DamageTextTimer) * 1.5f;
            g.DrawString(m.DamageText, font, brush, cx, m.Y - 20 - floatOffset, new StringFormat { Alignment = StringAlignment.Center });
        }
    }
}

// 简化版 AudioManager（静默，不播放声音）
public static class AudioManager
{
    public static void Initialize() { }

    public static void PlayShootSound() { }
    public static void PlayLaserSound() { }
    public static void PlayProjectileSound(string type) { }
    public static void PlayHitSound() { }
    public static void PlayDeathSound() { }
}
