using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

using PureBattleGame.Core;

namespace PureBattleGame.Games.StarCoreDefense;



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
    private float _worldViewFactor = 1.0f; 
    private float _panX = 0;
    private float _panY = 0;
    private float _totalMapRange = 600; // 初始地图实际跨度 (半径) - 降低初始值更紧凑
    private bool _isDragging = false;
    private bool _isDraggingMinimap = false;
    private bool _isSpaceDown = false;
    private Point _lastDragPoint;
    private ToolTip _upgradeToolTip = new ToolTip { InitialDelay = 200, AutoPopDelay = 10000 };
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
        this.FormBorderStyle = FormBorderStyle.Sizable; // 改为可调整大小
        this.Opacity = 0.1; 
        this.MinimumSize = new Size(600, 450);

        // 键盘快捷键
        this.KeyPreview = true;
        this.KeyDown += BattleForm_KeyDown;

        // 鼠标事件
        this.MouseDown += BattleForm_MouseDown;
        this.MouseMove += BattleForm_MouseMove;
        this.MouseUp += BattleForm_MouseUp;
        this.MouseWheel += BattleForm_MouseWheel;
        this.KeyUp += BattleForm_KeyUp;

        SetupZoomButtons();
        InitializeBrowser();
    }

    private void SetupZoomButtons()
    {
        // 缩放按钮可以用 GDI+ 直接画在界面上，也可以用 Button。
        // 为了风格统一，我们在 Render 中绘制。
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
        // 每帧重置怪物被攻击计数
        foreach (var m in _monsters) m.AttackerCount = 0;

        foreach (var robot in _robots)
        {
            if (robot.IsActive && !robot.IsDead)
            {
                robot.Update(this.ClientSize.Width, this.ClientSize.Height, _robots, _monsters);
                // 统计攻击计数
                if (robot.MonsterTarget != null && robot.MonsterTarget.IsActive)
                    robot.MonsterTarget.AttackerCount++;
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
            if (_minerals.Count < 20) // 适当增加资源密度
            {
                // 改为在大地图物理范围内生成，而不是屏幕内 (彻底修复 ClientSize 可能为负导致的崩溃)
                var baseB = GetBaseRobot();
                float bX = baseB?.X + baseB?.Size / 2 ?? 0;
                float bY = baseB?.Y + baseB?.Size / 2 ?? 0;
                
                // 在当前地图半径内随机分布
                float mx = bX + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 1.5f;
                float my = bY + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 1.5f;

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

                            if (p.Type == "LIGHTNING")
                            {
                                monster.ParalyzeTimer = 60; // 1秒麻痹
                                AddExplosion(monster.X + monster.Size / 2, monster.Y + monster.Size / 2, Color.White, 3, "SPARK");
                                
                                // 连锁闪电逻辑：寻找 250 距离内的下一个目标
                                if (p.ChainCount < 3)
                                {
                                    Monster? nextTarget = null;
                                    float minDist = 250;
                                    foreach(var m in _monsters)
                                    {
                                        if (m == monster || !m.IsActive || m.IsDead) continue;
                                        float dx = m.X - monster.X;
                                        float dy = m.Y - monster.Y;
                                        float d = (float)Math.Sqrt(dx*dx + dy*dy);
                                        if (d < minDist)
                                        {
                                            minDist = d;
                                            nextTarget = m;
                                        }
                                    }

                                    if (nextTarget != null)
                                    {
                                        var nextP = new Projectile(p.Owner, monster.X + monster.Size/2, monster.Y + monster.Size/2, 
                                                                  nextTarget.X + nextTarget.Size/2, nextTarget.Y + nextTarget.Size/2, "LIGHTNING");
                                        nextP.ChainCount = p.ChainCount + 1;
                                        nextP.TrackingMonster = nextTarget; // 锁定追踪新目标
                                        _projectiles.Add(nextP);
                                    }
                                }
                            }

                            // 通用命中特效
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
        var baseBot = GetBaseRobot();
        float baseX = baseBot?.X + baseBot?.Size / 2 ?? this.ClientSize.Width / 2f;
        float baseY = baseBot?.Y + baseBot?.Size / 2 ?? this.ClientSize.Height / 2f;

        float scale = 1.0f / _worldViewFactor;

        // 保存状态绘制 UI
        var oldTransform = g.Transform;

        // 应用地图缩放与平移变换 (以基地为中心配合平移)
        g.TranslateTransform(this.ClientSize.Width / 2f + _panX, this.ClientSize.Height / 2f + _panY);
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(-baseX, -baseY);

        // 绘制网格背景 (扩大范围以覆盖缩放后的视野)
        DrawWorldGrid(g, baseX, baseY);

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
                if (robot == _selectedRobot) DrawSelectionRing(g, robot);
            }
        }

        foreach (var ft in _floatingTexts)
        {
            using var font = new Font("Impact", 12 * _worldViewFactor, FontStyle.Bold); 
            using var brush = new SolidBrush(Color.FromArgb((int)(ft.Life / (float)ft.MaxLife * 255), ft.TextColor));
            g.DrawString(ft.Text, font, brush, ft.X, ft.Y);
        }

        // 还原变换，绘制不随地图缩放的 UI
        g.Transform = oldTransform;
        
        DrawMinimap(g);
        DrawZoomButtons(g);
        DrawResourceUI(g);

        if (_isGameEnding)
        {
            DrawGameEndingOverlay(g);
        }

        if (_isSpawningMonster)
        {
            // 在屏幕空间显示预览或是转换为屏幕预览
            using var brush = new SolidBrush(Color.FromArgb(100, Color.Red));
            g.FillEllipse(brush, _monsterSpawnPoint.X - 25, _monsterSpawnPoint.Y - 25, 50, 50);
        }
    }

    private void DrawWorldGrid(Graphics g, float centerX, float centerY)
    {
        using var pen = new Pen(Color.FromArgb(40, 40, 60), 1);
        using var borderPen = new Pen(Color.FromArgb(80, 80, 100), 2); // 地图边缘提示
        int gridSize = 50;
        float range = _totalMapRange; 

        // 以传入的中心点(基地)为核心绘制网格
        int startX = (int)(centerX - range);
        int endX = (int)(centerX + range);
        int startY = (int)(centerY - range);
        int endY = (int)(centerY + range);

        for (int x = startX; x <= endX; x += gridSize)
            g.DrawLine(pen, x, startY, x, endY);
        for (int y = startY; y <= endY; y += gridSize)
            g.DrawLine(pen, startX, y, endX, y);
            
        // 绘制地图边界框
        g.DrawRectangle(borderPen, startX, startY, endX - startX, endY - startY);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Space && (msg.Msg == 0x0100 || msg.Msg == 0x0104)) // KeyDown
        {
            _isSpaceDown = true;
        }

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
        // 坐标变换 (屏幕 -> 世界)
        var baseBot = GetBaseRobot();
        float baseX = baseBot?.X + baseBot?.Size / 2 ?? 0;
        float baseY = baseBot?.Y + baseBot?.Size / 2 ?? 0;
        float scale = 1.0f / _worldViewFactor;
        
        float worldX = (e.X - (this.ClientSize.Width / 2f + _panX)) / scale + baseX;
        float worldY = (e.Y - (this.ClientSize.Height / 2f + _panY)) / scale + baseY;

        // 优先检查 UI 点击
        if (CheckZoomButtonClicked(e.Location)) return;
        if (CheckMinimapClicked(e.Location))
        {
            _isDraggingMinimap = true;
            return;
        }

        // 仅当按下空格键且左键按下时开启拖拽
        if (_isSpaceDown && e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _lastDragPoint = e.Location;
            this.Cursor = Cursors.SizeAll; // 视觉反馈
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (_isSpawningMonster)
            {
                // 放置怪物
                var monster = new Monster(worldX - 15, worldY - 15);
                monster.MaxHP = 500 + CurrentWave * 100;
                monster.HP = monster.MaxHP;
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
                    if (robot.IsActive && !robot.IsDead && robot.HitTest((int)worldX, (int)worldY))
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
                            if (worldX >= monster.X && worldX <= monster.X + monster.Size &&
                                worldY >= monster.Y && worldY <= monster.Y + monster.Size)
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
                _selectedRobot.LaunchRemoteAttackAtPosition(worldX, worldY);
            }
        }
    }

    private void BattleForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDraggingMinimap)
        {
            CheckMinimapClicked(e.Location);
            return;
        }

        // 只有拖拽中且空格按下时才平移相机
        if (_isDragging && _isSpaceDown)
        {
            _panX += e.X - _lastDragPoint.X;
            _panY += e.Y - _lastDragPoint.Y;
            _lastDragPoint = e.Location;
        }
        else
        {
            _isDragging = false;
            if (this.Cursor == Cursors.SizeAll) this.Cursor = Cursors.Default;
        }

        if (_isSpawningMonster)
        {
            // 在屏幕空间显示预览，转换为世界坐标并在 Render 中绘制
            _monsterSpawnPoint = e.Location;
        }
    }

    private void BattleForm_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
        _isDraggingMinimap = false;
        this.Cursor = Cursors.Default;
    }

    private void BattleForm_MouseWheel(object? sender, MouseEventArgs e)
    {
        float oldFactor = _worldViewFactor;
        
        // 动态计算最大缩小倍率：确保能看到全图 (跨度是 2 * _totalMapRange)
        float maxFactor = Math.Max(1.5f, (2.2f * _totalMapRange) / Math.Min(this.ClientSize.Width, this.ClientSize.Height));

        // 缩放逻辑 (限制在 0.2x 到动态 maxFactor)
        if (e.Delta > 0)
        {
            _worldViewFactor = Math.Max(0.2f, _worldViewFactor * 0.9f); // 放大
        }
        else
        {
            _worldViewFactor = Math.Min(maxFactor, _worldViewFactor * 1.1f); // 缩小
        }

        // 调整 _panX 和 _panY 使得世界中心保持原处
        if (oldFactor != _worldViewFactor)
        {
            _panX = _panX * (oldFactor / _worldViewFactor);
            _panY = _panY * (oldFactor / _worldViewFactor);
        }
    }

    private void BattleForm_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            _isSpaceDown = false;
        }
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
                // 获取基地参考点
                var baseB = GetBaseRobot();
                float bX = baseB?.X + baseB?.Size / 2 ?? 0;
                float bY = baseB?.Y + baseB?.Size / 2 ?? 0;

                // 核心算法：怪物从基地的真实地图物理边缘刷出
                float spawnRange = _totalMapRange + 50; 

                float spawnX = 0, spawnY = 0;
                int edge = _rand.Next(4);

                switch (edge)
                {
                    case 0: // 顶部边缘
                        spawnX = bX + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 2;
                        spawnY = bY - spawnRange;
                        break;
                    case 1: // 右侧边缘
                        spawnX = bX + spawnRange;
                        spawnY = bY + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 2;
                        break;
                    case 2: // 底部边缘
                        spawnX = bX + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 2;
                        spawnY = bY + spawnRange;
                        break;
                    case 3: // 左侧边缘
                        spawnX = bX - spawnRange;
                        spawnY = bY + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 2;
                        break;
                }

                var monster = new Monster(spawnX, spawnY, CurrentWave);
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
                    
                    // 每新增一波，地图实际大小扩大 0.5 倍
                    _totalMapRange *= 1.5f;
                    
                    // 视野也同步稍微拉远一点点，最高不超过 8.0 倍
                    _worldViewFactor = Math.Min(8.0f, _worldViewFactor + 0.1f);
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
        _totalMapRange = 600; // 重置初始地图跨度 (更紧密战场)
        _worldViewFactor = 1.5f; // 初始化为远景
        _panX = 0;
        _panY = 0;
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
        string waveText;
        Color waveColor;
        if (_monstersToSpawnInWave > 0)
        {
            int remaining = _monsters.Count(m => m.IsActive && !m.IsDead) + _monstersToSpawnInWave;
            waveText = $"第 {CurrentWave} 波  剩余 {remaining} 敌";
            waveColor = Color.OrangeRed;
        }
        else if (_waveTimer > 0)
        {
            int secLeft = (_waveTimer + 59) / 60;
            waveText = $"第 {CurrentWave} 波  下一波: {secLeft}s";
            waveColor = secLeft <= 3 ? Color.OrangeRed : Color.LightGreen;
        }
        else
        {
            waveText = $"第 {CurrentWave} 波  激战中";
            waveColor = Color.White;
        }

        using var waveBrush = new SolidBrush(waveColor);
        var size = g.MeasureString(waveText, waveFont);
        float waveX = (this.ClientSize.Width - size.Width) / 2;
        g.DrawString(waveText, waveFont, waveBrush, waveX, 6);

        // 倒计时进度条
        if (_waveTimer > 0 && _monstersToSpawnInWave <= 0)
        {
            int barW = 180;
            float barX = (this.ClientSize.Width - barW) / 2f;
            float barY = size.Height + 8;
            float pct = (float)_waveTimer / 600f;
            
            using var barBg = new SolidBrush(Color.FromArgb(60, 255, 255, 255));
            using var barFill = new SolidBrush(pct > 0.2f ? Color.LimeGreen : Color.OrangeRed);
            g.FillRectangle(barBg, barX, barY, barW, 4);
            g.FillRectangle(barFill, barX, barY, barW * pct, 4);
        }
    }

    private void DrawMinimap(Graphics g)
    {
        int mapSize = 150;
        int padding = 10;
        int x = this.ClientSize.Width - mapSize - padding;
        int y = 45;

        // 背景
        using var bgBrush = new SolidBrush(Color.FromArgb(150, 10, 10, 15));
        g.FillRectangle(bgBrush, x, y, mapSize, mapSize);
        using var borderPen = new Pen(Color.FromArgb(100, 200, 200, 255), 1);
        g.DrawRectangle(borderPen, x, y, mapSize, mapSize);

        // 小地图中心锚点
        var baseBot = GetBaseRobot();
        float bX = baseBot?.X + baseBot?.Size / 2 ?? 0;
        float bY = baseBot?.Y + baseBot?.Size / 2 ?? 0;

        // 小地图比例尺：使其完美容纳整个战区地图 (Grid 范围)
        // 跨度是 2 * _totalMapRange，我们再扩一点点边界 (1.1倍) 确保怪在最外面也能看到
        float miniRange = 2.2f * _totalMapRange; 
        float miniScale = mapSize / miniRange; 

        // 绘制资源点
        using var mineralBrush = new SolidBrush(Color.Cyan);
        foreach (var m in _minerals)
        {
            if (!m.IsActive) continue;
            float mx = x + mapSize / 2f + (m.X - bX) * miniScale;
            float my = y + mapSize / 2f + (m.Y - bY) * miniScale;
            if (mx >= x && mx <= x + mapSize && my >= y && my <= y + mapSize)
                g.FillRectangle(mineralBrush, mx - 1, my - 1, 2, 2);
        }

        // 绘制怪物
        using var monsterBrush = new SolidBrush(Color.Red);
        foreach (var mon in _monsters)
        {
            if (!mon.IsActive || mon.IsDead) continue;
            float mx = x + mapSize / 2f + (mon.X - bX) * miniScale;
            float my = y + mapSize / 2f + (mon.Y - bY) * miniScale;
            if (mx >= x && mx <= x + mapSize && my >= y && my <= y + mapSize)
                g.FillRectangle(monsterBrush, mx - 1, my - 1, 3, 3);
        }

        // 绘制机器人
        using var robotBrush = new SolidBrush(Color.Cyan);
        foreach (var b in _robots)
        {
            if (!b.IsActive || b.IsDead) continue;
            float mx = x + mapSize / 2f + (b.X - bX) * miniScale;
            float my = y + mapSize / 2f + (b.Y - bY) * miniScale;
            if (mx >= x && mx <= x + mapSize && my >= y && my <= y + mapSize)
                g.FillRectangle(robotBrush, mx - 1, my - 1, 3, 3);
        }

        // 绘制基地 (大点)
        if (baseBot != null)
        {
            g.FillRectangle(Brushes.White, x + mapSize / 2f - 3, y + mapSize / 2f - 3, 6, 6);
        }

        // 绘制当前窗口视野范围 (Viewport)
        float worldViewW = this.ClientSize.Width * _worldViewFactor;
        float worldViewH = this.ClientSize.Height * _worldViewFactor;
        // 视野矩形在世界中的中心是 (bX + _panX*factor, bY + _panY*factor)
        float viewCenterX = bX - (_panX * _worldViewFactor);
        float viewCenterY = bY - (_panY * _worldViewFactor);
        
        float vX = x + mapSize / 2f + (viewCenterX - bX - worldViewW/2f) * miniScale;
        float vY = y + mapSize / 2f + (viewCenterY - bY - worldViewH/2f) * miniScale;
        float vW = worldViewW * miniScale;
        float vH = worldViewH * miniScale;

        using var viewPen = new Pen(Color.White, 1);
        g.DrawRectangle(viewPen, vX, vY, vW, vH);
    }

    private void DrawZoomButtons(Graphics g)
    {
        int mapSize = 150;
        int padding = 10;
        int btnWidth = 30;
        int startX = this.ClientSize.Width - mapSize - padding - btnWidth - 5;
        int startY = 45;

        using var bgBrush = new SolidBrush(Color.FromArgb(150, 40, 40, 50));
        using var borderPen = new Pen(Color.FromArgb(150, 100, 100, 150), 1);
        using var font = new Font("Impact", 15);

        // [+] 按钮
        g.FillRectangle(bgBrush, startX, startY, btnWidth, btnWidth);
        g.DrawRectangle(borderPen, startX, startY, btnWidth, btnWidth);
        g.DrawString("+", font, Brushes.White, startX + 5, startY + 2);

        // [-] 按钮
        g.FillRectangle(bgBrush, startX, startY + btnWidth + 5, btnWidth, btnWidth);
        g.DrawRectangle(borderPen, startX, startY + btnWidth + 5, btnWidth, btnWidth);
        g.DrawString("-", font, Brushes.White, startX + 7, startY + btnWidth + 6);
    }

    private bool IsOverMinimap(Point p)
    {
        int mapSize = 150;
        int padding = 10;
        int x = this.ClientSize.Width - mapSize - padding;
        int y = 45;
        return p.X >= x && p.X <= x + mapSize && p.Y >= y && p.Y <= y + mapSize;
    }

    private bool IsOverZoomButtons(Point p)
    {
        int mapSize = 150;
        int padding = 10;
        int btnWidth = 30;
        int startX = this.ClientSize.Width - mapSize - padding - btnWidth - 5;
        int startY = 45;
        // [+]
        if (p.X >= startX && p.X <= startX + btnWidth && p.Y >= startY && p.Y <= startY + btnWidth) return true;
        // [-]
        if (p.X >= startX && p.X <= startX + btnWidth && p.Y >= startY + btnWidth + 5 && p.Y <= startY + 2 * btnWidth + 5) return true;
        return false;
    }

    private bool CheckZoomButtonClicked(Point p)
    {
        int mapSize = 150;
        int padding = 10;
        int btnWidth = 30;
        int startX = this.ClientSize.Width - mapSize - padding - btnWidth - 5;
        int startY = 45;

        // [+] 放大
        if (p.X >= startX && p.X <= startX + btnWidth && p.Y >= startY && p.Y <= startY + btnWidth)
        {
            float oldF = _worldViewFactor;
            _worldViewFactor = Math.Max(0.2f, _worldViewFactor * 0.8f);
            _panX = _panX * (oldF / _worldViewFactor);
            _panY = _panY * (oldF / _worldViewFactor);
            return true;
        }
        // [-] 缩小 (动态适配地图大小)
        if (p.X >= startX && p.X <= startX + btnWidth && p.Y >= startY + btnWidth + 5 && p.Y <= startY + 2 * btnWidth + 5)
        {
            float oldF = _worldViewFactor;
            float maxFactor = Math.Max(1.5f, (2.2f * _totalMapRange) / Math.Min(this.ClientSize.Width, this.ClientSize.Height));
            _worldViewFactor = Math.Min(maxFactor, _worldViewFactor * 1.2f);
            
            if (oldF != _worldViewFactor)
            {
                _panX = _panX * (oldF / _worldViewFactor);
                _panY = _panY * (oldF / _worldViewFactor);
            }
            return true;
        }
        return false;
    }

    private bool CheckMinimapClicked(Point p)
    {
        int mapSize = 150;
        int padding = 10;
        int x = this.ClientSize.Width - mapSize - padding;
        int y = 45;

        if (p.X >= x && p.X <= x + mapSize && p.Y >= y && p.Y <= y + mapSize)
        {
            // 点击小地图平移相机
            var baseBot = GetBaseRobot();
            float bX = baseBot?.X + baseBot?.Size / 2 ?? 0;
            float bY = baseBot?.Y + baseBot?.Size / 2 ?? 0;
            // 与 DrawMinimap 里的比例尺严格对齐
            float miniRange = 2.2f * _totalMapRange; 
            float miniScale = mapSize / miniRange;

            float miniRelX = p.X - (x + mapSize / 2f);
            float miniRelY = p.Y - (y + mapSize / 2f);

            _panX = -(miniRelX / miniScale) / _worldViewFactor;
            _panY = -(miniRelY / miniScale) / _worldViewFactor;
            return true;
        }
        return false;
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
        int panelWidth = 510;
        int btnWidth = 85;
        int btnHeight = 42; // 足够展示两行文字
        int spacing = 12;

        var panel = new FlickerFreePanel
        {
            Name = "ControlPanel",
            Size = new Size(panelWidth, 90),
            Location = new Point((this.ClientSize.Width - panelWidth) / 2, this.ClientSize.Height - 95),
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
                Size = new Size(btnWidth, isUpgrade ? 24 : btnHeight),
                FlatStyle = FlatStyle.Flat,
                ForeColor = isUpgrade ? Color.Cyan : Color.White,
                BackColor = isUpgrade ? Color.FromArgb(30, 30, 55) : Color.FromArgb(40, 40, 50),
                Font = new Font("Microsoft YaHei", isUpgrade ? 6.5f : 7.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0),
                Padding = new Padding(2),
                AutoEllipsis = false,
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 90);
            btn.Click += onClick;
            return btn;
        }

        int startX = 5;
        // 购买按钮 y 坐标（顶部留给升级按钮）
        int buyY = 28;
        int upgY = 4;

        // 0. 基地升级 (无购买按钮，只有一个占位或恢复血量)
        int baseUpgradeCost = 150 * _baseLevel;
        panel.Controls.Add(CreateBtn("UpgBase", $"Lv.{_baseLevel} 💎{baseUpgradeCost}", startX, upgY, (s, e) =>
        {
            int cost = 150 * _baseLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _baseLevel++;
                var b = GetBaseRobot();
                if (b != null) b.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("HealBase", $"维修 💰100", startX, buyY, (s, e) =>
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
        panel.Controls.Add(CreateBtn("UpgWorker", $"Lv.{_workerLevel} 💎{workerUpgradeCost}", workerStartX, upgY, (s, e) =>
        {
            int cost = 50 * _workerLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _workerLevel++;
                foreach (var r in _robots) if (r.ClassType == RobotClass.Worker) r.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyWorker", $"采集工 💰{_workerCost}", workerStartX, buyY, (s, e) =>
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
        panel.Controls.Add(CreateBtn("UpgHealer", $"Lv.{_healerLevel} 💎{healerUpgradeCost}", healerStartX, upgY, (s, e) =>
        {
            int cost = 80 * _healerLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _healerLevel++;
                foreach (var r in _robots) if (r.ClassType == RobotClass.Healer) r.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyHealer", $"治疗者 💰{_defenderCost}", healerStartX, buyY, (s, e) =>
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
        panel.Controls.Add(CreateBtn("UpgShooter", $"Lv.{_shooterLevel} 💎{shooterUpgradeCost}", shooterStartX, upgY, (s, e) =>
        {
            int cost = 60 * _shooterLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _shooterLevel++;
                foreach (var r in _robots) if (r.ClassType == RobotClass.Shooter) r.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyShooter", $"攻击者 💰{_shooterCost}", shooterStartX, buyY, (s, e) =>
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
        panel.Controls.Add(CreateBtn("UpgGuardian", $"Lv.{_guardianLevel} 💎{guardianUpgradeCost}", guardianStartX, upgY, (s, e) =>
        {
            int cost = 100 * _guardianLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _guardianLevel++;
                foreach (var r in _robots) if (r.ClassType == RobotClass.Guardian) r.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyGuardian", $"守卫者 💰{_guardianCost}", guardianStartX, buyY, (s, e) =>
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
        UpdateUpgradeToolTips(); 

        // 支持拖拽窗口 (除 UI 区域外)
        this.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                // 排除 UI 区域点击，优先给游戏逻辑处理
                if (!IsOverMinimap(e.Location) && !IsOverZoomButtons(e.Location))
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
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
        if (panel.Controls["UpgWorker"] is Button uW) uW.Text = $"Lv.{_workerLevel} 💎{50 * _workerLevel}";
        if (panel.Controls["BuyWorker"] is Button btnW) btnW.Text = $"采集工 💰{_workerCost}";
        if (panel.Controls["UpgHealer"] is Button uH) uH.Text = $"Lv.{_healerLevel} 💎{80 * _healerLevel}";
        if (panel.Controls["BuyHealer"] is Button btnD) btnD.Text = $"治疗者 💰{_defenderCost}";
        if (panel.Controls["UpgShooter"] is Button uS) uS.Text = $"Lv.{_shooterLevel} 💎{60 * _shooterLevel}";
        if (panel.Controls["BuyShooter"] is Button btnS) btnS.Text = $"攻击者 💰{_shooterCost}";
        if (panel.Controls["UpgGuardian"] is Button uG) uG.Text = $"Lv.{_guardianLevel} 💎{100 * _guardianLevel}";
        if (panel.Controls["BuyGuardian"] is Button btnG) btnG.Text = $"守卫者 💰{_guardianCost}";

        UpdateUpgradeToolTips();
    }

    private void UpdateUpgradeToolTips()
    {
        var panel = this.Controls["ControlPanel"] as Panel;
        if (panel == null) return;

        if (panel.Controls["UpgBase"] is Button uB)
        {
            int nextHP = 3000 + _baseLevel * 1000;
            int currentDmg = 35 + (_baseLevel - 1) * 15;
            _upgradeToolTip.SetToolTip(uB, $"【基地升级】(当前 Lv.{_baseLevel})\n" +
                $"- 最大生命: {3000 + (_baseLevel - 1) * 1000}\n" +
                $"- 防御波伤害: {currentDmg}\n" +
                $"- 下级预览: 生命 → {nextHP}, 伤害 → {currentDmg + 15}\n" +
                $"- 额外效果: 升级时瞬间补满全部生命值。");
        }

        if (panel.Controls["UpgWorker"] is Button uW)
            _upgradeToolTip.SetToolTip(uW, $"【采集工升级】(当前 Lv.{_workerLevel})\n" +
                $"- 生命上限加成: +{(_workerLevel - 1) * 20}%\n" +
                $"- 下级预览: 全体采集工生命上限再提升 20%");

        if (panel.Controls["UpgHealer"] is Button uH)
            _upgradeToolTip.SetToolTip(uH, $"【治疗者升级】(当前 Lv.{_healerLevel})\n" +
                $"- 单次治疗量: {20 + (_healerLevel - 1) * 5}\n" +
                $"- 治疗频率: 2.0 次/秒\n" +
                $"- 下级预览: 治疗量提高至 {25 + (_healerLevel - 1) * 5}");

        if (panel.Controls["UpgShooter"] is Button uS)
        {
            float fireRate = 60.0f / Math.Max(12, 40 - _shooterLevel * 3);
            int projectiles = 1 + (_shooterLevel - 1) / 3;
            _upgradeToolTip.SetToolTip(uS, $"【游侠升级】(当前 Lv.{_shooterLevel})\n" +
                $"- 射击频率: {fireRate:F1} 发/秒\n" +
                $"- 弹幕数量: {projectiles} 枚/轮\n" +
                $"- 伤害加成: +{(_shooterLevel - 1) * 25}%\n" +
                $"- 特殊解锁: {GetShooterUnlockInfo(_shooterLevel)}");
        }

        if (panel.Controls["UpgGuardian"] is Button uG)
            _upgradeToolTip.SetToolTip(uG, $"【守卫者升级】(当前 Lv.{_guardianLevel})\n" +
                $"- 冲撞伤害: {(int)(40 * (1 + (_guardianLevel - 1) * 0.25f))}\n" +
                $"- 保护半径: 150 像素\n" +
                $"- 下级预览: 冲撞伤害提升 25%");
            
        // 购买按钮提示
        if (panel.Controls["BuyWorker"] is Button bW) _upgradeToolTip.SetToolTip(bW, "【工程单位】不具备攻击力，自动采集蓝色矿石（Minerals）。");
        if (panel.Controls["BuyHealer"] is Button bH) _upgradeToolTip.SetToolTip(bH, "【后勤单位】极速回血，优先保护基地与濒危队友。");
        if (panel.Controls["BuyShooter"] is Button bS) _upgradeToolTip.SetToolTip(bS, "【输出主力】全自动武器系统，随等级提升解锁更多弹药。");
        if (panel.Controls["BuyGuardian"] is Button bG) _upgradeToolTip.SetToolTip(bG, "【重型肉盾】高伤害近战冲撞，将敌人从基地身边无情击退。");
    }

    private string GetShooterUnlockInfo(int level)
    {
        if (level < 2) return "Lv.2 解锁【火箭弹】";
        if (level < 3) return "Lv.3 解锁【高能激光】";
        if (level < 4) return "Lv.4 解锁【等离子炮】";
        if (level < 5) return "Lv.5 解锁【自动制导】";
        if (level < 6) return "Lv.6 解锁【重型加农炮】";
        if (level < 8) return "Lv.8 解锁【连锁闪电】";
        if (level < 10) return "Lv.10 解锁【末日陨石】";
        return "已解锁终极火力形态";
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
            DrawBaseAppearance(g, robot, x, y, size, centerX, centerY);
        }
        else
        {
            using var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0));
            g.FillEllipse(shadowBrush, x, y + size * 0.2f, size, size);

            Draw3DOrbiters(g, robot, centerX, centerY, size, true);

            switch (robot.ClassType)
            {
                case RobotClass.Worker: DrawWorkerAppearance(g, robot, x, y, size, centerX, centerY); break;
                case RobotClass.Shooter: DrawShooterAppearance(g, robot, x, y, size, centerX, centerY); break;
                case RobotClass.Healer: DrawHealerAppearance(g, robot, x, y, size, centerX, centerY); break;
                case RobotClass.Guardian: DrawGuardianAppearance(g, robot, x, y, size, centerX, centerY); break;
                default: DrawDefaultAppearance(g, robot, x, y, size, centerX, centerY); break;
            }

            Draw3DOrbiters(g, robot, centerX, centerY, size, false);
        }

        DrawRobotHealthBar(g, robot, x, y, size);
        if (robot.SpecialState == "SHAKING" && robot.DuelTarget != null) DrawDuelEffect(g, robot, centerX, centerY);
    }

    private void DrawRobotHealthBar(Graphics g, Robot robot, float x, float y, float size)
    {
        if (robot.IsDead) return;
        if (robot.ClassType == RobotClass.Base)
        {
            float bw = 140, bh = 12, bx = x + (size - bw) / 2, by = y - 30;
            using (var bgb = new SolidBrush(Color.FromArgb(180, 20, 20, 25))) g.FillRectangle(bgb, bx, by, bw, bh);
            float pct = (float)robot.HP / robot.MaxHP, hpp = Math.Clamp(pct, 0, 1);
            Color hpc = (pct > 0.3f) ? Color.FromArgb(0, 255, 127) : Color.FromArgb(255, 60, 60);
            using (var hpb = new SolidBrush(hpc)) g.FillRectangle(hpb, bx + 2, by + 2, (bw - 4) * hpp, bh - 4);
            using (var p = new Pen(Color.White, 2)) g.DrawRectangle(p, bx, by, bw, bh);
            using (var f = new Font("Consolas", 9, FontStyle.Bold)) {
                string txt = $"{robot.HP}/{robot.MaxHP}";
                var sz = g.MeasureString(txt, f);
                g.DrawString(txt, f, Brushes.Black, bx + (bw - sz.Width) / 2 + 1, by - 14 + 1);
                g.DrawString(txt, f, Brushes.White, bx + (bw - sz.Width) / 2, by - 14);
            }
        }
        else if (robot.HP < robot.MaxHP)
        {
            float bw = size * 0.9f, bh = 4, bx = x + (size - bw) / 2, by = y - 8;
            using var bgb = new SolidBrush(Color.FromArgb(150, 50, 50, 50)); g.FillRectangle(bgb, bx, by, bw, bh);
            using var hpb = new SolidBrush(robot.PrimaryColor); g.FillRectangle(hpb, bx, by, bw * Math.Clamp((float)robot.HP / robot.MaxHP, 0, 1), bh);
        }
    }

    private void Draw3DOrbiters(Graphics g, Robot robot, float cx, float cy, float size, bool backLayer)
    {
        int count = 6;
        float radius = size * 0.8f, tilt = 0.4f, speed = 1.0f;
        string type = "SPARK";
        switch(robot.ClassType) {
            case RobotClass.Worker: radius = size * 0.5f; speed = 3.0f; type = "DRILL"; break;
            case RobotClass.Shooter: radius = size * 0.8f; speed = 2.0f; type = "SHELL"; break;
            case RobotClass.Healer: radius = size * 1.0f; speed = 1.2f; type = "NANO"; break;
            case RobotClass.Guardian: radius = size * 1.0f; speed = 0.6f; type = "PLATE"; break;
        }
        for (int i = 0; i < count; i++) {
            float ang = (Environment.TickCount/1000f) * speed + (float)(i * Math.PI * 2 / count);
            float bx = (float)(Math.Sin(ang) * radius), by = (float)(Math.Cos(ang) * radius * tilt), z = (float)Math.Cos(ang);
            if (backLayer && z > 0) continue; if (!backLayer && z <= 0) continue;
            float ps = 4 + z * 2, alpha = 150 + z * 100;
            using var br = new SolidBrush(Color.FromArgb((int)Math.Clamp(alpha, 0, 255), robot.PrimaryColor));
            if (type == "NANO") {
                g.FillRectangle(Brushes.LimeGreen, cx + bx - 1, cy + by - 1, 3, 3);
                if (Environment.TickCount % 500 < 100) g.DrawLine(Pens.White, cx+bx-3, cy+by, cx+bx+3, cy+by);
            } else g.FillEllipse(br, cx + bx - ps/2, cy + by - ps/2, ps, ps);
        }
    }

    private void DrawBaseAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        DrawBaseCubicOrbit(g, cx, cy, size, true);
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        g.FillRectangle(darkBrush, x - 15, y + size - 5, size + 30, 15);
        g.FillRectangle(bodyBrush, x - 5, y + size - 12, size + 10, 8);
        PointF[] pts = { new PointF(x, y+size), new PointF(x+size*0.2f, y+size*0.3f), new PointF(x+size*0.8f, y+size*0.3f), new PointF(x+size, y+size) };
        g.FillPolygon(bodyBrush, pts); g.DrawPolygon(Pens.Cyan, pts);
        float hov = (float)Math.Sin(Environment.TickCount / 500.0) * 5;
        g.FillRectangle(darkBrush, x + size * 0.15f, y + hov, size * 0.7f, size * 0.4f);
        g.FillEllipse(Brushes.White, cx - 8, cy - 8, 16, 16);
        DrawBaseCubicOrbit(g, cx, cy, size, false);
    }

    private void DrawBaseCubicOrbit(Graphics g, float cx, float cy, float size, bool backLayer)
    {
        float time = Environment.TickCount / 1500f, radius = size * 1.2f;
        for (int i = 0; i < 12; i++) {
            float ang = time + (float)(i * Math.PI * 2 / 12), tilt = (float)Math.Sin(time * 0.5f) * 0.3f;
            float x = (float)(Math.Cos(ang) * radius), y = (float)(Math.Sin(ang) * radius * tilt), z = (float)Math.Sin(ang);
            if (backLayer && z > 0) continue; if (!backLayer && z <= 0) continue;
            float ps = 6 + z * 3; Color c = backLayer ? Color.DarkBlue : Color.Cyan;
            using var br = new SolidBrush(Color.FromArgb(150 + (int)(z*100), c));
            g.FillRectangle(br, cx + x - ps/2, cy + y - ps/2, ps, ps);
            g.DrawRectangle(Pens.White, cx + x - ps/2, cy + y - ps/2, ps, ps);
        }
    }
    private void DrawWorkerAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        g.FillEllipse(bodyBrush, x, y, size, size);
        g.FillEllipse(darkBrush, x + size * 0.2f, y + size * 0.2f, size * 0.6f, size * 0.6f);
        float rot = Environment.TickCount / 100.0f;
        float ax = cx + (float)Math.Cos(rot) * (size * 0.6f), ay = cy + (float)Math.Sin(rot) * (size * 0.6f);
        using var p = new Pen(robot.PrimaryColor, 3); g.DrawLine(p, cx, cy, ax, ay);
        g.FillEllipse(Brushes.Yellow, ax - 3, ay - 3, 6, 6);
        DrawEyes(g, robot, cx, cy, 3);
    }

    private void DrawShooterAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        PointF[] pts = { new PointF(cx, y), new PointF(x + size, cy), new PointF(cx, y + size), new PointF(x, cy) };
        g.FillPolygon(bodyBrush, pts); g.FillRectangle(darkBrush, cx - 2, cy - 2, 4, 4);
        float ang = (float)Math.Atan2(robot.Dy, robot.Dx);
        if (robot.MonsterTarget != null) ang = (float)Math.Atan2(robot.MonsterTarget.Y - cy, robot.MonsterTarget.X - cx);
        for (int i = -1; i <= 1; i += 2) {
            float ba = ang + i * 0.3f, bx = cx + (float)Math.Cos(ba) * (size * 0.7f), by = cy + (float)Math.Sin(ba) * (size * 0.7f);
            using var bp = new Pen(Color.Gray, 4); g.DrawLine(bp, cx, cy, bx, by);
        }
        DrawVisor(g, cx, cy, size, ang);
    }

    private void DrawHealerAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        g.FillEllipse(bodyBrush, x, y, size, size);
        float pulse = 0.5f + (float)Math.Sin(Environment.TickCount / 200.0) * 0.5f;
        using var pb = new SolidBrush(Color.FromArgb((int)(100 * pulse), Color.LimeGreen));
        g.FillEllipse(pb, x - 5, y - 5, size + 10, size + 10);
        using var cb = new SolidBrush(Color.White);
        g.FillRectangle(cb, cx - 2, cy - 8, 4, 16); g.FillRectangle(cb, cx - 8, cy - 2, 16, 4);
        for (int i = 0; i < 2; i++) {
            float rot = Environment.TickCount / 500.0f + i * (float)Math.PI;
            float sx = cx + (float)Math.Cos(rot) * (size * 0.8f), sy = cy + (float)Math.Sin(rot) * (size * 0.8f);
            g.FillEllipse(Brushes.LimeGreen, sx - 3, sy - 3, 6, 6);
        }
    }

    private void DrawGuardianAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        PointF[] hex = new PointF[6];
        for (int i = 0; i < 6; i++) {
            float a = i * (float)Math.PI / 3;
            hex[i] = new PointF(cx + (float)Math.Cos(a) * (size / 2f), cy + (float)Math.Sin(a) * (size / 2f));
        }
        g.FillPolygon(bodyBrush, hex); using var bp = new Pen(darkBrush, 2); g.DrawPolygon(bp, hex);
        g.FillRectangle(darkBrush, x - 4, cy - 4, 8, 8); g.FillRectangle(darkBrush, x + size - 4, cy - 4, 8, 8);
        float ang = (float)Math.Atan2(robot.Dy, robot.Dx);
        float sa = (float)(ang * 180 / Math.PI - 45);
        using var sp = new Pen(Color.FromArgb(150, robot.SecondaryColor), 5);
        g.DrawArc(sp, x - 10, y - 10, size + 20, size + 20, sa, 90);
        DrawEyes(g, robot, cx, cy, 2);
    }

    private void DrawDefaultAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor); g.FillEllipse(bodyBrush, x, y, size, size);
        DrawEyes(g, robot, cx, cy, 4);
    }

    private void DrawVisor(Graphics g, float cx, float cy, float size, float angle)
    {
        float vx = cx + (float)Math.Cos(angle) * (size * 0.2f), vy = cy + (float)Math.Sin(angle) * (size * 0.2f);
        using var vb = new SolidBrush(Color.FromArgb(200, Color.Red));
        g.FillRectangle(Brushes.Black, vx - 8, vy - 2, 16, 4); g.FillRectangle(vb, vx - 6, vy - 1, 12, 2);
    }

    private void DrawEyes(Graphics g, Robot robot, float cx, float cy, float ps)
    {
        float ey = cy - 2, lx = cx - ps * 1.5f, rx = cx + ps * 0.5f;
        using var eb = new SolidBrush(robot.EyeColor);
        g.FillEllipse(Brushes.White, lx, ey, ps, ps * 0.8f); g.FillEllipse(Brushes.White, rx, ey, ps, ps * 0.8f);
        g.FillEllipse(eb, lx + ps * 0.2f, ey + ps * 0.2f, ps * 0.6f, ps * 0.6f);
        g.FillEllipse(eb, rx + ps * 0.2f, ey + ps * 0.2f, ps * 0.6f, ps * 0.6f);
    }

    private void DrawEyes(Graphics g, Robot robot, float cx, float cy) => DrawEyes(g, robot, cx, cy, 5);

    private void DrawAntennas(Graphics g, Robot robot, float cx, float cy) { }
    private void DrawTentacles(Graphics g, Robot robot, float cx, float cy) { }

    private void DrawLaserAttack(Graphics g, Robot robot, float cx, float cy)
    {
        if (!robot.IsFiringLaser) return;
        float tcx = robot.LaserTargetX, tcy = robot.LaserTargetY;
        using var lp = new Pen(robot.PrimaryColor, 4); using var cp = new Pen(Color.White, 2);
        g.DrawLine(lp, cx, cy, tcx, tcy); g.DrawLine(cp, cx, cy, tcx, tcy);
        using var sg = new SolidBrush(Color.FromArgb(100, 255, 100, 100)); g.FillEllipse(sg, cx - 8, cy - 8, 16, 16);
        using var hg = new SolidBrush(Color.FromArgb(150, 255, 200, 200)); g.FillEllipse(hg, tcx - 6, tcy - 6, 12, 12);
    }

    private void DrawDuelEffect(Graphics g, Robot robot, float cx, float cy)
    {
        if (robot.DuelTarget == null) return;
        float tcx = robot.DuelTarget.X + robot.DuelTarget.Size / 2, tcy = robot.DuelTarget.Y + robot.DuelTarget.Size / 2;
        using var dp = new Pen(Color.FromArgb(180, 255, 100, 100), 3); dp.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
        g.DrawLine(dp, cx, cy, tcx, tcy);
        float mx = (cx + tcx) / 2, my = (cy + tcy) / 2;
        using var gb = new SolidBrush(Color.FromArgb(120, 255, 200, 100)); g.FillEllipse(gb, mx - 10, my - 10, 20, 20);
    }
}
