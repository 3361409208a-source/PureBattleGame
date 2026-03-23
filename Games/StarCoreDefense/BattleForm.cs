using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Linq; // Added for LINQ operations
using System.Collections.Concurrent; // Added for ConcurrentDictionary in SpatialGrid

using PureBattleGame.Core;

namespace PureBattleGame.Games.StarCoreDefense;



public partial class BattleForm : Form
{
    public static BattleForm? Instance { get; private set; }

    // 游戏状态
    private List<Robot> _robots = new List<Robot>();
    private List<Monster> _monsters = new List<Monster>();
    private List<Projectile> _projectiles = new List<Projectile>();
    private readonly object _projectileLock = new object(); // 并发锁
    private Dictionary<Color, Brush> _brushCache = new Dictionary<Color, Brush>(); // 画笔缓存
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
    private int _waveTimer = 120; // 【割草改动】开局2秒后直接开干，不磨叽
    private int _monstersToSpawnInWave = 0;
    private int _spawnInterval = 0;
    private int _baseFireworkTimer = 0; // 5秒周期性火球计时器
    private int _waveStartTimer = 0;    // 波次开始后的帧数
    private bool _hasFiredWaveMeteor = false; // 每波首发标记

    // 全局升级系统
    public float GlobalDamageMultiplier { get; set; } = 1.0f;
    public float GlobalHealthMultiplier { get; set; } = 1.0f;

    // 兵种升级等级
    public int _baseLevel = 1;
    public BaseModule _currentBaseModule = BaseModule.None;
    private int _baseOverloadCooldown = 0;
    public int _workerLevel = 1;
    public int _healerLevel = 1;
    public int _shooterLevel = 1;
    public int _guardianLevel = 1;
    public int _engineerLevel = 1;

    // 机器人价格递增
    private int _workerCost = 50;
    private int _defenderCost = 150;
    private int _shooterCost = 100;
    private int _guardianCost = 200;
    private int _engineerCost = 150;

    // 渲染
    private Bitmap? _backBuffer;
    private Graphics? _bufferGraphics;
    private Panel? _settingsPanel;
    private bool _isLayer1Activated = false; // 外层防线是否已激活（全满后激活，之后只要有血就生效）
    private int _currentBuildingLayer = 1; // 当前正在建设的城墙层数（1开始，Layer 0是基础层）

    // 粒子系统
    private List<Particle> _particles = new List<Particle>();
    private List<Mineral> _minerals = new List<Mineral>();
    public List<WallSegment> _walls = new List<WallSegment>();
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

    // 性能诊断
    private int _fps = 0;
    private int _bgmSwitchTimer = 0; 
    private int _activeBattleTrack = 1; // 当前选中的战斗音轨 (1或3)
    private int _frameCountForFps = 0;
    private DateTime _lastMetricUpdate = DateTime.Now;
    private TimeSpan _lastTotalProcessorTime = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
    private double _cpuUsage = 0;
    private double _memUsageMB = 0;

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
        this.Opacity = SettingsManager.Current.DefaultOpacity;
        SetupGame();
        InitializeWalls();
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
    }

    private SpatialGrid _spatialGrid = new SpatialGrid(200);

    private void SetupZoomButtons()
    {
        // 缩放按钮可以用 GDI+ 直接画在界面上，也可以用 Button。
        // 为了风格统一，我们在 Render 中绘制。
    }


    private void SetupGame()
    {
        // 创建双缓冲（限制最大尺寸防止内存溢出）
        int maxSize = 4096;
        int width = Math.Min(this.ClientSize.Width, maxSize);
        int height = Math.Min(this.ClientSize.Height, maxSize);
        _backBuffer = new Bitmap(width, height);
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
        CreateSettingsUI();
    }

    private void CreateSettingsUI()
    {
        Button btnSettings = new Button
        {
            Text = "⚙️ 设置",
            Location = new Point(this.ClientSize.Width - 150, 5),
            Size = new Size(70, 25),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(50, 50, 60),
            Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnSettings.FlatAppearance.BorderSize = 1;
        btnSettings.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
        btnSettings.Click += (s, e) => { 
            if (_settingsPanel != null)
            {
                _settingsPanel.Visible = !_settingsPanel.Visible; 
                _settingsPanel.BringToFront(); 
            }
        };
        this.Controls.Add(btnSettings);
        btnSettings.BringToFront();

        _settingsPanel = new Panel
        {
            Size = new Size(140, 150),
            Location = new Point(this.ClientSize.Width - 170, 35),
            BackColor = Color.FromArgb(200, 30, 30, 35),
            Visible = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BorderStyle = BorderStyle.FixedSingle
        };

        CheckBox cbSfx = new CheckBox 
        { 
            Text = "音效已开启", 
            Checked = !AudioManager.IsMutedSFX, 
            ForeColor = Color.White, 
            Location = new Point(10, 10), 
            AutoSize = true,
            Font = new Font("Microsoft YaHei", 8)
        };
        cbSfx.CheckedChanged += (s, e) => {
            AudioManager.IsMutedSFX = !cbSfx.Checked;
            cbSfx.Text = cbSfx.Checked ? "音效已开启" : "音效已禁用";
        };

        CheckBox cbBgm = new CheckBox 
        { 
            Text = "音乐已开启", 
            Checked = !AudioManager.IsMutedBGM, 
            ForeColor = Color.White, 
            Location = new Point(10, 40), 
            AutoSize = true,
            Font = new Font("Microsoft YaHei", 8)
        };
        cbBgm.CheckedChanged += (s, e) => {
            AudioManager.IsMutedBGM = !cbBgm.Checked;
            cbBgm.Text = cbBgm.Checked ? "音乐已开启" : "音乐已禁用";
            AudioManager.UpdateBGMVolume();
        };

        Label lblVol = new Label 
        { 
            Text = "音效音量:", 
            ForeColor = Color.White, 
            AutoSize = true, 
            Location = new Point(8, 70),
            Font = new Font("Microsoft YaHei", 8)
        };

        TrackBar tbSfxVol = new TrackBar
        {
            Minimum = 0,
            Maximum = 1000,
            Value = AudioManager.SfxVolume,
            TickFrequency = 100,
            Location = new Point(5, 90),
            Size = new Size(120, 30),
            TickStyle = TickStyle.BottomRight
        };

        tbSfxVol.ValueChanged += (s, e) => {
            AudioManager.SfxVolume = tbSfxVol.Value;
            if (AudioManager.SfxVolume == 0) cbSfx.Checked = false;
        };

        _settingsPanel.Controls.Add(cbSfx);
        _settingsPanel.Controls.Add(cbBgm);
        _settingsPanel.Controls.Add(lblVol);
        _settingsPanel.Controls.Add(tbSfxVol);
        this.Controls.Add(_settingsPanel);
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
        // 性能指标计算
        _frameCountForFps++;
        var now = DateTime.Now;
        var elapsed = (now - _lastMetricUpdate).TotalSeconds;
        if (elapsed >= 1.0)
        {
            _fps = (int)(_frameCountForFps / elapsed);
            _frameCountForFps = 0;
            
            // CPU 计算
            var currentCpuTime = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            var cpuUsedMs = (currentCpuTime - _lastTotalProcessorTime).TotalMilliseconds;
            _cpuUsage = cpuUsedMs / (Environment.ProcessorCount * elapsed * 1000);
            _lastTotalProcessorTime = currentCpuTime;

            // 内存计算
            _memUsageMB = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1024.0 / 1024.0;
            
            _lastMetricUpdate = now;
        }

        if (_isGameEnding)
        {
            HandleGameEnding();
            return;
        }

        // 更新机器人
        // --- 性能优化：空间网格更新 ---
        _spatialGrid.Clear();
        foreach (var m in _monsters) {
            m.AttackerCount = 0;
            if (m.IsActive && !m.IsDead) _spatialGrid.Add(m);
        }

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
        bool l1Active = IsLayerComplete(1);
        foreach (var monster in _monsters)
        {
            if (monster.IsActive && !monster.IsDead)
            {
                monster.Update(this.ClientSize.Width, this.ClientSize.Height, _robots, l1Active);
            }
        }
        // --- 核心集成：音乐淡入淡出每帧更新 ---
        AudioManager.Update(1.0f / 60.0f); 

        // 【断点续播 & 完整播放轮换】
        bool existsThreat = _monsters.Any(m => m.IsActive && !m.IsDead);
        
        if (existsThreat)
        {
            // 检查当前战斗音乐是否播放结束，若结束则轮换下一首 (MCI Mode 会变为 stopped)
            string status = AudioManager.GetTrackStatus(_activeBattleTrack);
            if (status.Contains("stopped") || status.Contains("not ready"))
            {
                 // 自动轮换：曲终即换
                 _activeBattleTrack = (_activeBattleTrack == 1) ? 3 : 1;
            }

            _bgmSwitchTimer = 180; 
            AudioManager.PlayBGM(_activeBattleTrack); 
        }
        else
        {
            if (_bgmSwitchTimer > 0) _bgmSwitchTimer--;
            if (_bgmSwitchTimer <= 0)
            {
                AudioManager.PlayBGM(2); // 切换平时音乐，战斗音乐将在原地暂停
            }
        }

        // --- 核心逻辑增强：全周界合拢监控 ---
        if (!_isLayer1Activated)
        {
            var l1 = _walls.Where(w => w.Layer == 1).ToList();
            // 严苛判定：36 节外墙全部满血，瞬间触发合拢
            if (l1.Count > 0 && l1.All(w => w.HP >= w.MaxHP))
            {
                _isLayer1Activated = true;
                
                // 史诗级视觉反馈：全员换防加速！
                AddFloatingText(this.ClientSize.Width / 2, this.ClientSize.Height / 2 - 100, "SYSTEM: OUTER PERIMETER LOCKED! 🛡️⚡", Color.Gold);
                AddFloatingText(this.ClientSize.Width / 2, this.ClientSize.Height / 2 - 60, "INITIATING TACTICAL REDEPLOYMENT...", Color.Cyan);
                AudioManager.PlaySound("level_up");
                
                // 激活全员冲刺，持续 15 秒 (900 帧)，确保绝对同步换防
                foreach (var r in _robots) r.SpeedBoostTimer = 900; 
                
                // 给驻防点重置一次，确保机器人刷新最佳位置
                foreach (var r in _robots) r.AssignedWall = null;
            }
        }

        // 更新矿物生成逻辑 (已废弃全图散落矿石)
        _mineralSpawnTimer--;
        if (_mineralSpawnTimer <= 0)
        {
            _mineralSpawnTimer = 300 + _rand.Next(300);
            // 【割草废弃】不再在场景中生成满地乱跑的物理矿石。
            // 因为采集工（Worker）现在直接在你眼前充当“挂机印钞机”，全图矿石已经没有存在意义了。
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
        lock (_projectileLock)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var p = _projectiles[i];
                if (!p.IsActive)
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                p.Update();

                // 【核心视觉：超级陨石炸裂】
                if (p.Type == "METEOR" && p.ProjectileColor == Color.Magenta && p.LifeTime <= 1)
                {
                    // 模拟真实烟火：散开、下坠、加速
                    Color[] rainbow = { Color.Red, Color.Orange, Color.Yellow, Color.Lime, Color.Cyan, Color.Magenta, Color.White };
                    for (int ri = 0; ri < 80; ri++) 
                    {
                        float angle = (float)(_rand.NextDouble() * Math.PI * 2);
                        float speed = (float)(_rand.NextDouble() * 14 + 6);
                        Color c = rainbow[ri % rainbow.Length];
                        float sdx = (float)Math.Cos(angle) * speed;
                        float sdy = (float)Math.Sin(angle) * speed - 5; // 初始稍微抗重力一下
                        
                        var particle = new Particle(p.X, p.Y, sdx, sdy, c, _rand.Next(45, 100), (float)_rand.NextDouble() * 5 + 3, "FIREWORK_SPARK");
                        _particles.Add(particle);
                    }
                    AddExplosion(p.X, p.Y, Color.White, 1, "RING"); 
                    TriggerChainExplosion(p.X, p.Y, 700, p.Damage); 
                    AudioManager.PlaySound("hit");
                }

                // 检测与机器人碰撞
                foreach (var robot in _robots)
                {
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

                if (!p.IsActive) continue;

                // 检测与怪物碰撞 (只有机器人的投射物才对怪物造成伤害) - 使用空间网格优化
                if (!p.IsMonsterProjectile)
                {
                    foreach (var monster in _spatialGrid.GetNearby(p.X, p.Y))
                    {
                        if (monster.IsActive && !monster.IsDead && p.CheckCollision(monster.X, monster.Y, monster.Size))
                        {
                            if (p.Type == "METEOR" && p.ProjectileColor == Color.Magenta) 
                            {
                                // 蓄力彩色陨石特殊处理：撞击时引发小规模AOE但弹头不消失(穿透)
                                TriggerChainExplosion(monster.X, monster.Y, 150, p.Damage / 5);
                                continue; 
                            }

                            // 【修复：高级武器无范围伤害Bug】之前所有子弹都只造成单体伤害，即使是陨石！
                            if (p.ExplosionRadius > 0)
                            {
                                for (int mi = 0; mi < _monsters.Count; mi++)
                                {
                                    var m = _monsters[mi];
                                    if (m.IsActive && !m.IsDead)
                                    {
                                        float dx = (m.X + m.Size / 2) - p.X;
                                        float dy = (m.Y + m.Size / 2) - p.Y;
                                        if (dx * dx + dy * dy <= p.ExplosionRadius * p.ExplosionRadius)
                                            m.OnHit(p);
                                    }
                                }
                                AddExplosion(p.X, p.Y, p.ProjectileColor, 15, "SPARK");
                            }
                            else
                            {
                                monster.OnHit(p);
                                AddExplosion(p.X, p.Y, p.ProjectileColor, 5, "SPARK");
                            }
                            
                            if (p.Type == "LIGHTNING") { monster.ParalyzeTimer = 60; AddExplosion(monster.X + monster.Size / 2, monster.Y + monster.Size / 2, Color.White, 3, "SPARK"); }
                            
                            if (p.Type != "BLACK_HOLE" && p.Type != "DEATH_RAY") { p.IsActive = false; _projectiles.RemoveAt(i); }
                            break;
                        }
                    }
                }
            }
        }

        // 处理所有实体之间的物理碰撞
        HandleAllCollisions();
        HandleMonsterWallCollision();

        // 基地被动与冷却
        HandleBaseModulePassives();

        // 基地能量波技能 (每6秒)
        UpdateBaseWave();

        // 检查波次逻辑
        HandleWaveLogic();

        // 检查游戏结束条件
        CheckGameEnd();

        // 围墙状态更新
        foreach (var w in _walls) {
            if (w.HitFlashTimer > 0) w.HitFlashTimer--;
            if (w.RepairEffectTimer > 0) w.RepairEffectTimer--;
        }

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
        if (this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
        {
            // 先释放 Graphics，再释放 Bitmap（正确的顺序）
            _bufferGraphics?.Dispose();
            _bufferGraphics = null;
            _backBuffer?.Dispose();

            // 限制最大缓冲区尺寸，防止内存溢出
            int maxSize = 4096;
            int width = Math.Min(this.ClientSize.Width, maxSize);
            int height = Math.Min(this.ClientSize.Height, maxSize);

            _backBuffer = new Bitmap(width, height);
            _bufferGraphics = Graphics.FromImage(_backBuffer);
            _bufferGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            _bufferGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        }

        // 调整浏览器位置
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

        // 绘制围墙
        RenderWalls(g, baseX, baseY);

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

        // 绘制投射物 - 使用快照防止遍历冲突
        Projectile[] pArray;
        lock (_projectileLock) pArray = _projectiles.ToArray();
        foreach (var p in pArray)
        {
            if (p.IsActive) DrawProjectile(g, p);
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

        // 绘制所有活跃粒子 (移到顶层确保烟火火花不被遮挡)
        foreach (var p in _particles)
        {
            if (p.IsActive) DrawParticle(g, p);
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

        // 绘制性能诊断信息
        DrawPerformanceStats(g);
    }

    private void DrawPerformanceStats(Graphics g)
    {
        string fpsText = $"FPS: {_fps}";
        string cpuText = $"CPU: {_cpuUsage:P1}";
        string memText = $"MEM: {_memUsageMB:F1} MB";
        string fullText = $"{fpsText} | {cpuText} | {memText}";

        using var font = new Font("Consolas", 9, FontStyle.Bold);
        var size = g.MeasureString(fullText, font);
        
        using var bgBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        g.FillRectangle(bgBrush, 10, 10, size.Width + 10, size.Height + 6);
        g.DrawRectangle(Pens.Gray, 10, 10, size.Width + 10, size.Height + 6);
        g.DrawString(fullText, font, Brushes.LimeGreen, 15, 13);
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
                BrowserForm.Instance.ToggleVisibility();
                return true;
            }
            // Alt + Q: 退出当前游戏，返回启动器
            else if (baseKey == Keys.Q)
            {
                if ((keyData & Keys.Shift) == Keys.Shift)
                {
                    // Alt + Shift + Q: 终极防老板 (彻底杀死程序)
                    Environment.Exit(0);
                }
                else
                {
                    ReturnToHome();
                }
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
        // 空格键：用于配合左键拖动画面
        else if (e.KeyCode == Keys.Space)
        {
            _isSpaceDown = true;
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

        // 点击中间（主窗口区域）可移动画面：支持【中键拖拽】或【空格+左键拖拽】
        if ((e.Button == MouseButtons.Middle) || (_isSpaceDown && e.Button == MouseButtons.Left))
        {
            _isDragging = true;
            _lastDragPoint = e.Location;
            this.Cursor = Cursors.SizeAll; // 视觉反馈：显示可移动状态
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

        // 平移相机：中键或空格+左键
        if (_isDragging && (_isSpaceDown || Control.MouseButtons == MouseButtons.Middle))
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
        // 关键逻辑调整：如果没有指定坐标，则默认从基地中心出生
        if (x < 0 || y < 0)
        {
            var b = GetBaseRobot();
            if (b != null)
            {
                x = b.X + b.Size / 2;
                y = b.Y + b.Size / 2;
            }
            else
            {
                x = this.ClientSize.Width / 2;
                y = this.ClientSize.Height / 2;
            }
        }

        var robot = new Robot(_robotIdCounter++, name, x, y, classType);
        _robots.Add(robot);
        return robot;
    }

    // 供 Robot 调用的方法
    public void AddProjectile(Projectile p)
    {
        lock (_projectileLock)
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

                    // 采集工不与采集工发生碰撞，避免在矿区发生“推搡”或“打架”现象
                    if (r1.ClassType == RobotClass.Worker && r2.ClassType == RobotClass.Worker) continue;

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

                // --- 物理层面穿透：外圈开启前，怪物与采集/工程单位不发生碰撞 ---
                bool l1Active = IsLayerComplete(1);
                if (!l1Active && (r.ClassType == RobotClass.Worker || r.ClassType == RobotClass.Engineer)) continue;

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
        
        // 【割草改动】基地等级越高，脉冲冷却时间越短（甚至能做到一两秒炸一次清屏）
        int cooldown = Math.Max(100, 360 - (_baseLevel - 1) * 25);
        
        // 1. 蓄力视觉效果优化 (大约提前1秒开始蓄力)
        if (_baseWaveTimer > cooldown - 60)
        {
            float baseX = baseRobot.X + baseRobot.Size / 2f;
            float baseY = baseRobot.Y + baseRobot.Size / 2f;
            
            // 产生吸入粒子
            if (_rand.Next(2) == 0)
            {
                float angle = (float)(_rand.NextDouble() * Math.PI * 2);
                float dist = 120 - (_baseWaveTimer - (cooldown - 60)) * 1.8f;
                if (dist < 10) dist = 10;
                float px = baseX + (float)Math.Cos(angle) * dist;
                float py = baseY + (float)Math.Sin(angle) * dist;
                AddExplosion(px, py, Color.FromArgb(200, Color.Cyan), 1, "SPARK");
            }
        }

        if (_baseWaveTimer >= cooldown) // 达到触发阈值
        {
            _baseWaveTimer = 0;

            // 【割草改动】指数暴涨的伤害与核弹般的清屏半径！
            float waveRadius = 300f + (_baseLevel - 1) * 100f; // 满级几乎覆盖半张地图
            float pushForce = 50f + (_baseLevel - 1) * 15f;    // 把怪推到天涯海角
            int waveDamage = 500 + (_baseLevel - 1) * 350;     // 足够秒杀后一半波次所有的中小型单位

            float baseX = baseRobot.X + baseRobot.Size / 2f;
            float baseY = baseRobot.Y + baseRobot.Size / 2f;

            // 爆发视觉特效
            AddExplosion(baseX, baseY, Color.White, 35, "RING"); 
            AddExplosion(baseX, baseY, Color.Cyan, 50, "SPARK");
            AddExplosion(baseX, baseY, Color.Blue, 15, "SMOKE");
            
            // 额外震撼视觉：在屏幕中心跳出提示
            AddFloatingText(baseX, baseY - 50, "[BASE PULSE]", Color.Aqua);

            // 影响范围内的怪物
            foreach (var monster in _monsters)
            {
                if (!monster.IsActive || monster.IsDead) continue;

                var (mx, my) = monster.GetCenter();
                float dx = mx - baseX;
                float dy = my - baseY;
                float dist = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));

                if (dist <= waveRadius)
                {
                    monster.TakeDamage(waveDamage);
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
                // 【帧率抢救】怪物数量适度缩减，WinForms渲染太吃力
                _monstersToSpawnInWave = 20 + CurrentWave * 10; 
                _spawnInterval = 0;
                _waveStartTimer = 0;
            }
            else
            {
                _waveTimer--;
            }
        }
        else
        {
            // 波次进行中
            _waveStartTimer++;
            _baseFireworkTimer++;

            // 5秒一个周期
            if (_baseFireworkTimer > 300) _baseFireworkTimer = 0;

            // 蓄力视觉阶段
            if (_baseFireworkTimer >= 240)
            {
                var br = GetBaseRobot();
                if (br != null)
                {
                    float angle = (float)(_rand.NextDouble() * Math.PI * 2);
                    float dist = 80 - (_baseFireworkTimer - 240) * 1.25f;
                    float px = (br.X + br.Size/2) + (float)Math.Cos(angle) * dist;
                    float py = (br.Y + br.Size/2) + (float)Math.Sin(angle) * dist;
                    AddExplosion(px, py, Color.Magenta, 1, "SPARK");
                }
            }

            if (_baseFireworkTimer == 299)
            {
                FireBaseWaveMeteor();
            }
        }

        // 处理合体逻辑 (每 5 个同类普通单位合成 1 个精英)
        // 生成怪物逻辑 (支持单帧多刷)
        if (_monstersToSpawnInWave > 0)
        {
            _spawnInterval--;
            if (_spawnInterval <= 0)
            {
                // 【割草改动】每帧刷怪数量极大提升，形成真正的虫群潮水
                int burst = Math.Min(_monstersToSpawnInWave, 3 + CurrentWave);
                for (int i = 0; i < burst; i++)
                {
                    SpawnOneMonster(CurrentWave);
                }
                
                // 刷怪无间隔，源源不断涌出
                _spawnInterval = Math.Max(1, 10 - CurrentWave); 
            }

            if (_monstersToSpawnInWave <= 0)
            {
                // 这波刷完了，进入下一波准备阶段
                CurrentWave++;
                _waveTimer = 120; // 【割草改动】间隔从600(10秒)狂砍到120(2秒)，这才是真正的“尸潮无缝衔接”！
                
                // 地图扩展由 1.5倍 更改为 线性增加 150，并设置最大上限 2500
                _totalMapRange = Math.Min(2500f, _totalMapRange + 150f);
                
                // 视野也同步稍微拉远一点点，最高不超过 8.0 倍
                _worldViewFactor = Math.Min(8.0f, _worldViewFactor + 0.15f);
            }
        }
    }

    private void FireBaseWaveMeteor()
    {
        var b = GetBaseRobot();
        if (b == null) return;
        float bx = b.X + b.Size / 2f, by = b.Y + b.Size / 2f;
        
        // 寻找最近的怪物作为目标
        Monster? target = null;
        float minDist = float.MaxValue;
        foreach(var m in _monsters) {
            if(m.IsActive && !m.IsDead) {
                float dx = m.X - bx, dy = m.Y - by;
                float d = dx*dx + dy*dy;
                if(d < minDist) { minDist = d; target = m; }
            }
        }

        float tx = bx, ty = by - 600; // 默认目标：上方
        if (target != null) {
            tx = target.X + target.Size / 2;
            ty = target.Y + target.Size / 2;
        }

        AudioManager.PlaySound("overload");
        AddExplosion(bx, by, Color.Gold, 20, "RING");

        lock (_projectileLock)
        {
            // 追踪型彩色核弹 (伤害下调，射速加快)
            var p = new Projectile(b, bx, by, tx, ty, "METEOR") 
            { 
                Size = 45, 
                Damage = 3000 + CurrentWave * 800,
                LifeTime = 80,  
                ExplosionRadius = 450, 
                ProjectileColor = Color.Magenta
            };
            
            // 计算朝向目标的初始速度
            float adx = tx - bx, ady = ty - by;
            float alen = (float)Math.Sqrt(adx*adx + ady*ady);
            if(alen > 0) {
                p.Dx = (adx / alen) * 16f;
                p.Dy = (ady / alen) * 16f;
            }

            _projectiles.Add(p);
        }
    }

    private void SpawnOneMonster(int wave)
    {
        var baseB = GetBaseRobot();
        float bX = baseB?.X + baseB?.Size / 2 ?? 0;
        float bY = baseB?.Y + baseB?.Size / 2 ?? 0;

        float spawnRange = _totalMapRange + 100; // 稍微拉开一点刷怪距离 
        float spawnX = 0, spawnY = 0;
        int edge = _rand.Next(4);

        switch (edge)
        {
            case 0: spawnX = bX + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 2; spawnY = bY - spawnRange; break;
            case 1: spawnX = bX + spawnRange; spawnY = bY + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 2; break;
            case 2: spawnX = bX + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 2; spawnY = bY + spawnRange; break;
            case 3: spawnX = bX - spawnRange; spawnY = bY + (float)(_rand.NextDouble() - 0.5) * _totalMapRange * 2; break;
        }

        var monster = new Monster(spawnX, spawnY, wave);
        // 【割草改动】调整血量：不能像纸糊的一碰就全死光，增加硬度让他们更密集
        monster.MaxHP = 200 + wave * 60; 
        monster.HP = monster.MaxHP;

        if (wave % 10 == 0 && _monstersToSpawnInWave == 1) // Boss
        {
            monster.MaxHP *= 15;
            monster.HP = monster.MaxHP;
            monster.Size = 100;
            AddFloatingText(spawnX, spawnY, "LEVEL BOSS INCOMING!", Color.Red);
        }

        _monsters.Add(monster);
        _monstersToSpawnInWave--;

        // 仅通知当前没有追踪目标的机器人去追击新怪物。
        // 正在被攻击的机器人不会丢下当前目标去追新怪。
        foreach (var robot in _robots)
            if (robot.IsActive && !robot.IsDead && robot.ClassType != RobotClass.Worker && robot.MonsterTarget == null)
                robot.SetMonsterTarget(monster);
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

    public void TriggerChainExplosion(float x, float y, float radius, int damage)
    {
        // 视觉反馈
        AddExplosion(x, y, Color.Orange, 5, "SPARK");
        
        lock (_projectileLock) { // 稍微防一下多线程并发问题
            // 找出范围内的怪物，触发真伤爆炸，引发连环殉爆
            for (int i = 0; i < _monsters.Count; i++)
            {
                var m = _monsters[i];
                if (m.IsActive && !m.IsDead)
                {
                    float dx = (m.X + m.Size / 2) - x;
                    float dy = (m.Y + m.Size / 2) - y;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        m.TakeDamage(damage); 
                    }
                }
            }
        }
    }

    public void AddExplosion(float x, float y, Color color, int count = 10, string type = "SPARK")
    {
        // 【帧率保底】如果同屏粒子超过300，坚决不再生成新粒子，拯救FPS
        if (_particles.Count > 300) return;

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

        // 画笔缓存优化，避免高并发分配内存
        if (!_brushCache.TryGetValue(color, out var brush))
        {
            brush = new SolidBrush(color);
            _brushCache[color] = brush;
        }

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
        int panelWidth = 530; // 稍作加宽以容纳 6 个栏位
        int btnWidth = 72;   // 按钮变小一点
        int btnHeight = 28;  // 紧凑高度
        int spacing = 8;     // 间距调窄

        var panel = new FlickerFreePanel
        {
            Name = "ControlPanel",
            Size = new Size(panelWidth, 75), // 降低高度
            Location = new Point((this.ClientSize.Width - panelWidth) / 2, this.ClientSize.Height - 80),
            BackColor = Color.FromArgb(20, 20, 25),
            BorderStyle = BorderStyle.None,
            Anchor = AnchorStyles.Bottom
        };

        panel.Controls.Add(new Label
        {
            Name = "ResMonitor",
            Text = "💰 {Gold}  |  💎 {Minerals}",
            Location = new Point(5, 52),
            Size = new Size(panelWidth - 10, 20),
            ForeColor = Color.Gold,
            Font = new Font("Consolas", 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        });

        // 按钮样式生成器
        Button CreateBtn(string id, string text, int x, int y, EventHandler onClick, bool isUpgrade = false)
        {
            var btn = new Button
            {
                Name = id,
                Text = text,
                Location = new Point(x, y),
                Size = new Size(btnWidth, isUpgrade ? 20 : btnHeight),
                FlatStyle = FlatStyle.Flat,
                ForeColor = isUpgrade ? Color.Cyan : Color.White,
                BackColor = isUpgrade ? Color.FromArgb(30, 30, 55) : Color.FromArgb(40, 40, 50),
                Font = new Font("Microsoft YaHei", isUpgrade ? 6.0f : 6.8f, FontStyle.Bold),
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
                // 检查当前层是否已建设完成（防止快速连升）
                if (!IsLayerComplete(_currentBuildingLayer))
                {
                    AddFloatingText(this.ClientSize.Width / 2, this.ClientSize.Height / 2 - 50,
                        $"环区 {_currentBuildingLayer} 尚未完工！", Color.OrangeRed);
                    return;
                }

                Minerals -= cost;
                _baseLevel++;
                AudioManager.PlaySound("level_up");

                // Lv.10 分支：选择模块
                if (_baseLevel == 10)
                {
                    ShowBaseModuleSelection();
                }

                // 基地升级时扩建城墙（绑定到基地等级）
                int newLayer = _baseLevel; // 新层数 = 基地等级
                BuildOuterLayerBlueprint(newLayer);
                _currentBuildingLayer = newLayer;

                UpdateWallScaling(); // 更新围墙

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

        // 主动技能：超载 (Lv.5 解锁)
        var btnOverload = CreateBtn("Overload", "⚡ 超载 ⚡", startX, buyY + 45, (s, e) => TriggerBaseOverload());
        btnOverload.Height = 15;
        btnOverload.Font = new Font("Microsoft YaHei", 5.5f, FontStyle.Bold);
        btnOverload.Visible = _baseLevel >= 5;
        panel.Controls.Add(btnOverload);
        if (_baseOverloadCooldown > 0) btnOverload.Enabled = false;

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
                AudioManager.PlaySound("level_up");
                foreach (var r in _robots) if (r.ClassType == RobotClass.Worker) r.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyWorker", $"采集工 💰{_workerCost}", workerStartX, buyY, (s, e) =>
        {
            int realCost = (_currentBaseModule == BaseModule.Industrial) ? (int)(_workerCost * 0.8f) : _workerCost;
            if (Gold >= realCost)
            {
                Gold -= realCost;
                AudioManager.PlaySound("purchase");
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
                AudioManager.PlaySound("level_up");
                foreach (var r in _robots) if (r.ClassType == RobotClass.Healer) r.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyHealer", $"治疗者 💰{_defenderCost}", healerStartX, buyY, (s, e) =>
        {
            int realCost = (_currentBaseModule == BaseModule.Industrial) ? (int)(_defenderCost * 0.8f) : _defenderCost;
            if (Gold >= realCost)
            {
                Gold -= realCost;
                AudioManager.PlaySound("purchase");
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
                AudioManager.PlaySound("level_up");
                foreach (var r in _robots) if (r.ClassType == RobotClass.Shooter) r.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyShooter", $"攻击者 💰{_shooterCost}", shooterStartX, buyY, (s, e) =>
        {
            int realCost = (_currentBaseModule == BaseModule.Industrial) ? (int)(_shooterCost * 0.8f) : _shooterCost;
            if (Gold >= realCost)
            {
                Gold -= realCost;
                AudioManager.PlaySound("purchase");
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
                AudioManager.PlaySound("level_up");
                foreach (var r in _robots) if (r.ClassType == RobotClass.Guardian) r.ApplyClassProperties();
                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyGuardian", $"守卫者 💰{_guardianCost}", guardianStartX, buyY, (s, e) =>
        {
            int realCost = (_currentBaseModule == BaseModule.Industrial) ? (int)(_guardianCost * 0.8f) : _guardianCost;
            if (Gold >= realCost)
            {
                Gold -= realCost;
                AudioManager.PlaySound("purchase");
                var r = SpawnRobot("守卫者", -1, -1, RobotClass.Guardian);
                r.ApplyClassProperties();
                _guardianCost = (int)(_guardianCost * 1.35f);
                UpdateUI();
            }
        }));

        // 5. 工程兵
        int engineerStartX = guardianStartX + btnWidth + spacing;
        int engineerUpgradeCost = 120 * _engineerLevel;
        panel.Controls.Add(CreateBtn("UpgEngineer", $"Lv.{_engineerLevel} 💎{engineerUpgradeCost}", engineerStartX, upgY, (s, e) =>
        {
            int cost = 120 * _engineerLevel;
            if (Minerals >= cost)
            {
                Minerals -= cost;
                _engineerLevel++;
                AudioManager.PlaySound("level_up");
                foreach (var r in _robots) if (r.ClassType == RobotClass.Engineer) r.ApplyClassProperties();

                // 注：城墙扩建已移至基地升级

                UpdateUI();
            }
        }, true));
        panel.Controls.Add(CreateBtn("BuyEngineer", $"工程兵 💰{_engineerCost}", engineerStartX, buyY, (s, e) =>
        {
            int realCost = (_currentBaseModule == BaseModule.Industrial) ? (int)(_engineerCost * 0.8f) : _engineerCost;
            if (Gold >= realCost)
            {
                Gold -= realCost;
                AudioManager.PlaySound("purchase");
                var r = SpawnRobot("工程兵", -1, -1, RobotClass.Engineer);
                r.ApplyClassProperties();
                _engineerCost = (int)(_engineerCost * 1.4f);
                UpdateUI();
            }
        }));

        this.Controls.Add(panel);
        UpdateUI();
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
        bool isInd = _currentBaseModule == BaseModule.Industrial;
        if (panel.Controls["UpgBase"] is Button uB) uB.Text = $"Lv.{_baseLevel} 💎{150 * _baseLevel}";
        if (panel.Controls["UpgWorker"] is Button uW) uW.Text = $"Lv.{_workerLevel} 💎{50 * _workerLevel}";
        if (panel.Controls["BuyWorker"] is Button btnW) btnW.Text = $"采集工 💰{(isInd ? (int)(_workerCost * 0.8f) : _workerCost)}";
        if (panel.Controls["UpgHealer"] is Button uH) uH.Text = $"Lv.{_healerLevel} 💎{80 * _healerLevel}";
        if (panel.Controls["BuyHealer"] is Button btnD) btnD.Text = $"治疗者 💰{(isInd ? (int)(_defenderCost * 0.8f) : _defenderCost)}";
        if (panel.Controls["UpgShooter"] is Button uS) uS.Text = $"Lv.{_shooterLevel} 💎{60 * _shooterLevel}";
        if (panel.Controls["BuyShooter"] is Button btnS) btnS.Text = $"攻击者 💰{(isInd ? (int)(_shooterCost * 0.8f) : _shooterCost)}";
        if (panel.Controls["UpgGuardian"] is Button uG) uG.Text = $"Lv.{_guardianLevel} 💎{100 * _guardianLevel}";
        if (panel.Controls["BuyGuardian"] is Button btnG) btnG.Text = $"守卫者 💰{(isInd ? (int)(_guardianCost * 0.8f) : _guardianCost)}";
        if (panel.Controls["UpgEngineer"] is Button uE) uE.Text = $"Lv.{_engineerLevel} 💎{120 * _engineerLevel}";
        if (panel.Controls["BuyEngineer"] is Button btnE) btnE.Text = $"工程兵 💰{(isInd ? (int)(_engineerCost * 0.8f) : _engineerCost)}";

        if (panel.Controls["Overload"] is Button bO)
        {
            bO.Visible = _baseLevel >= 5;
            bO.Enabled = _baseOverloadCooldown <= 0;
            if (_baseOverloadCooldown > 0) bO.Text = $"{_baseOverloadCooldown / 60}s";
            else bO.Text = "⚡ 超载 ⚡";
        }

        if (panel.Controls["ResMonitor"] is Label resL)
        {
            resL.Text = $"💰 {Gold}  |  💎 {Minerals}";
        }

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
            string layerStatus = IsLayerComplete(_currentBuildingLayer)
                ? $"✅ 环区 {_currentBuildingLayer} 已完工，可升级"
                : $"⏳ 环区 {_currentBuildingLayer} 建设中，完工后可升级";
            _upgradeToolTip.SetToolTip(uB, $"【基地升级】(当前 Lv.{_baseLevel})\n" +
                $"- 最大生命: {3000 + (_baseLevel - 1) * 1000}\n" +
                $"- 防御波伤害: {currentDmg}\n" +
                $"- 下级预览: 生命 → {nextHP}, 伤害 → {currentDmg + 15}\n" +
                $"- 额外效果: 升级时瞬间补满全部生命值，并启动新的城墙环区！\n" +
                $"- {layerStatus}");
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
        {
            int currentDmg = 100 + (_guardianLevel - 1) * 60;
            float currentOrbit = (_walls.Any(w => w.Layer == 1 && w.HP > 0)) ? 420f : 140f;
            _upgradeToolTip.SetToolTip(uG, $"【守护者升级】(当前 Lv.{_guardianLevel})\n" +
                $"- 冲击伤害: {currentDmg}\n" +
                $"- 当前巡逻半径: {currentOrbit:F0} 像素\n" +
                $"- 下级预览: 伤害 +60, 速度 +15%");
        }
            
        // 购买按钮提示
        if (panel.Controls["BuyWorker"] is Button bW) _upgradeToolTip.SetToolTip(bW, "【工程单位】不具备攻击力，自动采集蓝色矿石（Minerals）。");
        if (panel.Controls["BuyHealer"] is Button bH) _upgradeToolTip.SetToolTip(bH, "【后勤单位】极速回血，优先保护基地与濒危队友。");
        if (panel.Controls["BuyShooter"] is Button bS) _upgradeToolTip.SetToolTip(bS, "【输出主力】全自动武器系统，随等级提升解锁更多弹药。");
        if (panel.Controls["BuyGuardian"] is Button bG) _upgradeToolTip.SetToolTip(bG, "【重型冲撞者】在基地周围固定轨道快速巡逻，通过高速撞击阻击并击退任何试图靠近的怪物。");
        if (panel.Controls["BuyEngineer"] is Button bE) _upgradeToolTip.SetToolTip(bE, "【建设专家】自动维修城墙，优先建设未完成的外层防线。");

        // 工程兵升级提示
        if (panel.Controls["UpgEngineer"] is Button uE)
        {
            int repairAmount = 20 + (_engineerLevel - 1) * 5;
            _upgradeToolTip.SetToolTip(uE, $"【工程兵升级】(当前 Lv.{_engineerLevel})\n" +
                $"- 单次修复量: {repairAmount}\n" +
                $"- 修复频率: 超频模式\n" +
                $"- 下级预览: 修复量提高至 {repairAmount + 5}\n" +
                $"- 注：城墙扩建现在绑定到【基地升级】");
        }
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

        if (robot.Rank == RobotRank.Ultra)
        {
            DrawUltraAppearance(g, robot, x, y, size, x + size / 2, y + size / 2);
            DrawRobotHealthBar(g, robot, x, y, size);
            return;
        }

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
                case RobotClass.Engineer: DrawEngineerAppearance(g, robot, x, y, size, centerX, centerY); break;
                default: DrawDefaultAppearance(g, robot, x, y, size, centerX, centerY); break;
            }

            // 特殊效果：工程兵修理光束
            if (robot.ClassType == RobotClass.Engineer && robot.TargetWall != null)
            {
                var br = GetBaseRobot();
                var wp = robot.TargetWall.GetWorldPosition(br?.X ?? 0, br?.Y ?? 0);
                using (var pen = new Pen(Color.FromArgb(150, Color.DeepSkyBlue), 2 + (float)Math.Sin(Environment.TickCount / 50.0) * 1))
                {
                    g.DrawLine(pen, centerX, centerY, wp.X, wp.Y);
                    // 目标落点火花
                    g.FillEllipse(Brushes.White, wp.X - 2, wp.Y - 2, 4, 4);
                }
            }

            // 特殊效果：守卫者等离子旋风
            if (robot.SpecialState == "WHIRLWIND")
            {
                using var p = new Pen(Color.FromArgb(180, Color.Orange), 3);
                float rot = (Environment.TickCount / 25f);
                for (int i = 0; i < 3; i++)
                {
                    float angle = (rot + i * (float)Math.PI * 2 / 3f) * 180 / (float)Math.PI;
                    g.DrawArc(p, centerX - 60, centerY - 60, 120, 120, angle, 90);
                }
                using var glowB = new SolidBrush(Color.FromArgb(40, Color.Yellow));
                g.FillEllipse(glowB, centerX - 60, centerY - 60, 120, 120);
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
        else if (robot.HP < robot.MaxHP && robot.ClassType != RobotClass.Worker)
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
            case RobotClass.Guardian: radius = size * 1.1f; speed = 1.0f; type = "SHIELD_PLATE"; break;
        }
        for (int i = 0; i < count; i++) {
            float ang = (Environment.TickCount/1000f) * speed + (float)(i * Math.PI * 2 / count);
            float bx = (float)(Math.Sin(ang) * radius), by = (float)(Math.Cos(ang) * radius * tilt), z = (float)Math.Cos(ang);
            if (backLayer && z > 0) continue; if (!backLayer && z <= 0) continue;
            float ps = 4 + z * 2, alpha = 150 + z * 100;
            if (type == "SHIELD_PLATE")
            {
                // 守护兵专属重型盾片
                using var sb = new SolidBrush(Color.FromArgb((int)alpha, robot.SecondaryColor));
                g.FillRectangle(sb, cx + bx - 6, cy + by - 3, 12, 6);
                using var sp = new Pen(Color.FromArgb((int)alpha, Color.White), 1);
                g.DrawRectangle(sp, cx + bx - 6, cy + by - 3, 12, 6);
            }
            else
            {
                using var br = new SolidBrush(Color.FromArgb((int)Math.Clamp(alpha, 0, 255), robot.PrimaryColor));
                if (type == "NANO") {
                    g.FillRectangle(Brushes.LimeGreen, cx + bx - 1, cy + by - 1, 3, 3);
                    if (Environment.TickCount % 500 < 100) g.DrawLine(Pens.White, cx+bx-3, cy+by, cx+bx+3, cy+by);
                } else g.FillEllipse(br, cx + bx - ps/2, cy + by - ps/2, ps, ps);
            }
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
        
        // 1. 底盘：带有机械感的圆角矩形
        g.FillEllipse(darkBrush, x, y, size, size);
        g.FillEllipse(bodyBrush, x + size * 0.1f, y + size * 0.1f, size * 0.8f, size * 0.8f);
        
        // 2. 旋转的钻头/钻头臂 (仅在移动或采集时)
        float rot = Environment.TickCount / 50.0f;
        float armLen = size * 0.6f;
        for (int i = 0; i < 2; i++)
        {
            float ang = rot + i * (float)Math.PI;
            float ax = cx + (float)Math.Cos(ang) * armLen, ay = cy + (float)Math.Sin(ang) * armLen;
            using var p = new Pen(Color.Gray, 3); g.DrawLine(p, cx, cy, ax, ay);
            g.FillEllipse(Brushes.Silver, ax - 4, ay - 4, 8, 8);
            // 尖端闪烁
            if (robot.TargetMineral != null) g.FillEllipse(Brushes.White, ax - 1, ay - 1, 3, 3);
        }

        DrawEyes(g, robot, cx, cy, 3);
    }

    private void DrawShooterAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        
        // 1. 主翼：三角形锐利外观
        PointF[] pts = { new PointF(cx, y - 2), new PointF(x + size + 2, cy), new PointF(cx, y + size + 2), new PointF(x - 2, cy) };
        g.FillPolygon(bodyBrush, pts);
        g.FillEllipse(darkBrush, cx - size / 4, cy - size / 4, size / 2, size / 2);
        
        // 2. 炮管：指向目标
        float ang = (float)Math.Atan2(robot.Dy, robot.Dx);
        if (robot.MonsterTarget != null) ang = (float)Math.Atan2(robot.MonsterTarget.Y - cy, robot.MonsterTarget.X - cx);
        
        for (int i = -1; i <= 1; i += 2) {
            float ba = ang + i * 0.35f, bx = cx + (float)Math.Cos(ba) * (size * 0.85f), by = cy + (float)Math.Sin(ba) * (size * 0.85f);
            using var bp = new Pen(Color.FromArgb(80, 80, 80), 5); 
            g.DrawLine(bp, cx, cy, bx, by);
            // 炮口光效 (如果正在攻击)
            if (robot.MonsterTarget != null) g.FillEllipse(Brushes.OrangeRed, bx - 2, by - 2, 4, 4);
        }
        
        DrawVisor(g, cx, cy, size, ang);
    }

    private void DrawHealerAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        
        // 1. 核心球体
        g.FillEllipse(bodyBrush, x, y, size, size);
        float pulse = 0.5f + (float)Math.Sin(Environment.TickCount / 150.0) * 0.5f;
        
        // 2. 治疗光晕 (更加动态)
        using (var pb = new SolidBrush(Color.FromArgb((int)(80 * pulse), Color.LimeGreen)))
        {
            float ringSize = size * (1.2f + 0.2f * pulse);
            g.FillEllipse(pb, cx - ringSize / 2, cy - ringSize / 2, ringSize, ringSize);
        }

        // 3. 内部十字标志
        using var cb = new SolidBrush(Color.White);
        g.FillRectangle(cb, cx - 1, cy - 7, 3, 14); g.FillRectangle(cb, cx - 7, cy - 1, 14, 3);
        
        // 4. 环绕无人机
        for (int i = 0; i < 2; i++) {
            float rot = Environment.TickCount / 400.0f + i * (float)Math.PI;
            float sx = cx + (float)Math.Cos(rot) * (size * 0.9f), sy = cy + (float)Math.Sin(rot) * (size * 0.9f);
            g.FillEllipse(Brushes.White, sx - 2, sy - 2, 5, 5);
            g.FillEllipse(Brushes.LimeGreen, sx - 1, sy - 1, 3, 3);
        }
    }

    private void DrawGuardianAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        
        // 1. 旋转动感背影 (反应速度感)
        float rotSpeed = (float)Math.Sqrt(robot.Dx * robot.Dx + robot.Dy * robot.Dy);
        float spinAngle = Environment.TickCount / 100f * (1 + rotSpeed * 0.5f);
        
        // 2. 重型多重装甲 (八边形结构)
        PointF[] oct = new PointF[8];
        for (int i = 0; i < 8; i++) {
            float a = i * (float)Math.PI / 4 + spinAngle;
            float r = (i % 2 == 0) ? (size / 1.7f) : (size / 2.0f); // 交错结构
            oct[i] = new PointF(cx + (float)Math.Cos(a) * r, cy + (float)Math.Sin(a) * r);
        }
        g.FillPolygon(bodyBrush, oct);
        using var bp = new Pen(darkBrush, 3); g.DrawPolygon(bp, oct);
        
        // 3. 冲击尖刺 (在移动方向的前端)
        if (rotSpeed > 0.5f) {
            float moveAngle = (float)Math.Atan2(robot.Dy, robot.Dx);
            PointF[] spike = {
                new PointF(cx + (float)Math.Cos(moveAngle) * size * 0.9f, cy + (float)Math.Sin(moveAngle) * size * 0.9f),
                new PointF(cx + (float)Math.Cos(moveAngle + 0.5f) * size * 0.5f, cy + (float)Math.Sin(moveAngle + 0.5f) * size * 0.5f),
                new PointF(cx + (float)Math.Cos(moveAngle - 0.5f) * size * 0.5f, cy + (float)Math.Sin(moveAngle - 0.5f) * size * 0.5f)
            };
            using var spikeBrush = new SolidBrush(Color.FromArgb(200, Color.OrangeRed));
            g.FillPolygon(spikeBrush, spike);
        }

        // 4. 高能核心 (呼吸灯)
        float hPulse = 0.5f + (float)Math.Sin(Environment.TickCount / 80.0) * 0.5f;
        using var coreB = new SolidBrush(Color.FromArgb((int)(150 + 105 * hPulse), Color.Orange));
        g.FillEllipse(coreB, cx - 7, cy - 7, 14, 14);
        using var coreP = new Pen(Color.White, 2); g.DrawEllipse(coreP, cx - 7, cy - 7, 14, 14);

        DrawEyes(g, robot, cx, cy, 3);
    }

    private void DrawEngineerAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var darkBrush = new SolidBrush(robot.SecondaryColor);
        
        // 1. 工字型主体
        g.FillRectangle(darkBrush, x + size * 0.1f, y + size * 0.1f, size * 0.8f, size * 0.8f);
        g.FillRectangle(bodyBrush, x + size * 0.2f, y + size * 0.2f, size * 0.6f, size * 0.6f);
        
        // 2. 修理机械臂
        float rot = Environment.TickCount / 80.0f;
        float armLen = size * 0.7f;
        float ax = cx + (float)Math.Cos(rot) * armLen, ay = cy + (float)Math.Sin(rot) * armLen;
        using (var p = new Pen(Color.FromArgb(100, 100, 100), 4)) g.DrawLine(p, cx, cy, ax, ay);
        g.FillRectangle(Brushes.DeepSkyBlue, ax - 3, ay - 3, 6, 6);
        
        // 3. 电子眼 (深蓝色)
        using var eb = new SolidBrush(Color.Cyan);
        g.FillRectangle(eb, cx - 6, cy - 3, 4, 4);
        g.FillRectangle(eb, cx + 2, cy - 3, 4, 4);
    }

    private void DrawUltraAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        // 终极形态：多重环绕、核心脉冲、半透明重影
        float time = Environment.TickCount / 500f;
        
        // 1. 底层核心光晕
        using (var haloBrush = new SolidBrush(Color.FromArgb(100, robot.PrimaryColor)))
        {
            float haloSize = size * (1.2f + 0.1f * (float)Math.Sin(time * 2));
            g.FillEllipse(haloBrush, cx - haloSize/2, cy - haloSize/2, haloSize, haloSize);
        }

        // 2. 主体：尖锐的多边形
        PointF[] points = new PointF[8];
        for (int i = 0; i < 8; i++)
        {
            float angle = i * (float)Math.PI / 4 + time * 0.5f;
            float r = (i % 2 == 0) ? size * 0.6f : size * 0.4f;
            points[i] = new PointF(cx + (float)Math.Cos(angle) * r, cy + (float)Math.Sin(angle) * r);
        }
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        g.FillPolygon(bodyBrush, points);
        using var p = new Pen(Color.White, 3);
        g.DrawPolygon(p, points);

        // 3. 三重环绕粒子
        for (int i = 0; i < 3; i++)
        {
            Draw3DOrbiters(g, robot, cx, cy, size * (1.0f + i * 0.3f), i % 2 == 0);
        }

        DrawEyes(g, robot, cx, cy, 8);
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


    private void DrawMegaAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var pb = new SolidBrush(robot.PrimaryColor); using var sb = new SolidBrush(robot.SecondaryColor);
        switch (robot.ClassType) {
            case RobotClass.Worker:
                g.FillEllipse(sb, x - 5, y - 5, size + 10, size + 10); g.FillEllipse(pb, x, y, size, size);
                for (int i = 0; i < 3; i++) {
                    float r = (Environment.TickCount / 100f) + (float)(i * Math.PI * 2 / 3);
                    float dx = cx + (float)Math.Cos(r) * (size * 0.9f), dy = cy + (float)Math.Sin(r) * (size * 0.9f);
                    using var p = new Pen(Color.Gold, 4); g.DrawLine(p, cx, cy, dx, dy);
                    g.FillEllipse(Brushes.Yellow, dx - 5, dy - 5, 10, 10);
                }
                break;
            case RobotClass.Shooter:
                PointF[] pts = { new PointF(cx, y - 10), new PointF(x + size + 10, cy), new PointF(cx, y + size + 10), new PointF(x - 10, cy) };
                g.FillPolygon(pb, pts); g.DrawPolygon(Pens.White, pts);
                float ang = (float)Math.Atan2(robot.Dy, robot.Dx);
                if (robot.MonsterTarget != null) ang = (float)Math.Atan2(robot.MonsterTarget.Y - cy, robot.MonsterTarget.X - cx);
                for (int i = 0; i < 4; i++) {
                    float ba = ang + (i - 1.5f) * 0.25f, bx = cx + (float)Math.Cos(ba) * (size * 0.9f), by = cy + (float)Math.Sin(ba) * (size * 0.7f);
                    using var bp = new Pen(Color.Silver, 5); g.DrawLine(bp, cx, cy, bx, by);
                    g.FillEllipse(Brushes.Red, bx - 3, by - 3, 6, 6);
                }
                break;
            case RobotClass.Healer:
                g.FillEllipse(pb, x, y, size, size); g.FillEllipse(sb, x + 5, y + 5, size - 10, size - 10);
                using (var lb = new SolidBrush(Color.FromArgb(150, Color.LimeGreen))) g.FillEllipse(lb, x - 10, y - 10, size + 20, size + 20);
                using (var wb = new SolidBrush(Color.White)) { g.FillRectangle(wb, cx - 4, cy - 12, 8, 24); g.FillRectangle(wb, cx - 12, cy - 4, 24, 8); }
                for (int i = 0; i < 4; i++) {
                    float rot = (Environment.TickCount / 400f) + (float)(i * Math.PI / 2);
                    float sx = cx + (float)Math.Cos(rot) * (size * 1.1f), sy = cy + (float)Math.Sin(rot) * (size * 1.1f);
                    g.FillEllipse(Brushes.Cyan, sx - 4, sy - 4, 8, 8);
                }
                break;
            case RobotClass.Guardian:
                PointF[] hex = new PointF[6]; for (int i = 0; i < 6; i++) {
                    float a = i * (float)Math.PI / 3; hex[i] = new PointF(cx + (float)Math.Cos(a) * (size * 0.7f), cy + (float)Math.Sin(a) * (size * 0.7f));
                }
                g.FillPolygon(sb, hex); using (var sp = new Pen(Color.Gold, 3)) g.DrawPolygon(sp, hex);
                float gang = (float)Math.Atan2(robot.Dy, robot.Dx);
                for (int j = -1; j <= 1; j++) {
                    float sa = (float)(gang * 180 / Math.PI - 60 + j * 30);
                    using var shp = new Pen(Color.Cyan, 6);
                    g.DrawArc(shp, x - 15, y - 15, size + 30, size + 30, sa, 40);
                }
                break;
        }
        DrawEyes(g, robot, cx, cy, 6);
    } // Fix missing brace

    private void UpdateFusion()
    {
        // 1. Normal -> Mega Fusion (5 required)
        var groups = _robots.Where(r => r.IsActive && !r.IsDead && r.Rank == RobotRank.Normal && r.ClassType != RobotClass.Base)
                           .GroupBy(r => r.ClassType);
        foreach (var group in groups)
        {
            if (group.Count() >= 5)
            {
                var list = group.Take(5).ToList();
                float ax = list.Average(r => r.X), ay = list.Average(r => r.Y);
                RobotClass ct = list[0].ClassType;
                foreach (var r in list) { r.IsActive = false; r.IsDead = true; AddExplosion(r.X, r.Y, Color.White, 3); }
                var mega = new Robot(++_robotIdCounter, "MEGA " + ct, ax, ay, ct, RobotRank.Mega);
                mega.ApplyClassProperties();
                _robots.Add(mega);
                AddFloatingText(ax, ay, "MEGA FUSION!", Color.Gold);
                AudioManager.PlaySound("fusion");
            }
        }

        // 2. Mega -> Ultra Fusion (3 required)
        var megaGroups = _robots.Where(r => r.IsActive && !r.IsDead && r.Rank == RobotRank.Mega)
                               .GroupBy(r => r.ClassType);
        foreach (var group in megaGroups)
        {
            if (group.Count() >= 3)
            {
                var list = group.Take(3).ToList();
                float ax = list.Average(r => r.X), ay = list.Average(r => r.Y);
                RobotClass ct = list[0].ClassType;
                foreach (var r in list) { r.IsActive = false; r.IsDead = true; AddExplosion(r.X, r.Y, Color.Gold, 8); }
                var ultra = new Robot(++_robotIdCounter, "ULTRA " + ct, ax, ay, ct, RobotRank.Ultra);
                ultra.ApplyClassProperties();
                _robots.Add(ultra);
                AddFloatingText(ax, ay, "ULTRA EVOLUTION!", Color.DeepSkyBlue);
                AudioManager.PlaySound("ultra_fusion");
                AddExplosion(ax, ay, Color.Cyan, 20, "RING");
            }
        }
    }

    private void ShowBaseModuleSelection()
    {
        var result = MessageBox.Show(
            "基地达到 Lv.10！建议选择进化方向：\n\n" +
            "【是】堡垒模式 (Bastion)：极大血量 + 减速力场\n" +
            "【否】工业模式 (Industrial)：极致效率 + 购买 8 折",
            "高级进化选择",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (result == DialogResult.Yes)
        {
            _currentBaseModule = BaseModule.Bastion;
            var b = GetBaseRobot();
            if (b != null) { b.MaxHP += 50000; b.HP += 50000; }
            AddFloatingText(this.ClientSize.Width / 2, this.ClientSize.Height / 2, "BASTION MODE!", Color.DeepSkyBlue);
        }
        else
        {
            _currentBaseModule = BaseModule.Industrial;
            AddFloatingText(this.ClientSize.Width / 2, this.ClientSize.Height / 2, "INDUSTRIAL MODE!", Color.LimeGreen);
        }
    }

    private void TriggerBaseOverload()
    {
        if (_baseOverloadCooldown > 0) return;
        var b = GetBaseRobot();
        if (b == null) return;

        _baseOverloadCooldown = 600; 
        float bx = b.X + b.Size / 2, by = b.Y + b.Size / 2;

        AddExplosion(bx, by, Color.Cyan, 30, "RING");
        AudioManager.PlaySound("overload");

        // 超载半径随基地等级动态增长（基础 600，每升一级+80）
        float overloadRadius = 600f + (_baseLevel - 1) * 80f;
        foreach (var m in _monsters)
        {
            if (!m.IsActive || m.IsDead) continue;
            float dx = m.X - bx, dy = m.Y - by;
            float distSq = dx * dx + dy * dy;
            if (distSq < overloadRadius * overloadRadius)
            {
                float dist = (float)Math.Sqrt(distSq);
                float force = (overloadRadius - dist) / overloadRadius * 50f;
                m.X += (dx / dist) * force;
                m.Y += (dy / dist) * force;
                m.TakeDamage(2000 + _baseLevel * 200);
                AddExplosion(m.X, m.Y, Color.DeepSkyBlue, 3, "SPARK");
            }
        }
    }

    private void HandleBaseModulePassives()
    {
        if (_baseOverloadCooldown > 0) _baseOverloadCooldown--;

        // 动态更新墙体属性 (半径与厚度随等级成长)
        UpdateWallScaling();

        if (_currentBaseModule == BaseModule.Bastion)
        {
            var b = GetBaseRobot();
            if (b != null)
            {
                foreach (var m in _spatialGrid.GetNearby(b.X, b.Y))
                {
                    float dx = m.X - b.X, dy = m.Y - b.Y;
                    if (dx * dx + dy * dy < 350 * 350) m.SlowTimer = 10;
                }
            }
        }
    }

    private void InitializeWalls()
    {
        _walls.Clear();
        _isLayer1Activated = false; // 重置激活状态
        
        // Layer 0: 内层防线 (基础 24 节) - 初始满血!
        int innerSegments = 24; 
        for (int i = 0; i < innerSegments; i++)
        {
            float angle = (float)(i * Math.PI * 2 / innerSegments);
            var wall = new WallSegment(angle, 150, 10, 1000) { Layer = 0, HP = 1000 }; 
            _walls.Add(wall);
        }

        // Layer 1: 更广阔的外围防线 (36 节) - 初始 0 HP (蓝图)
        int outerSegments = 36;
        for (int i = 0; i < outerSegments; i++)
        {
            float angle = (float)(i * Math.PI * 2 / outerSegments);
            var wall = new WallSegment(angle, 450, 15, 3000) { Layer = 1, HP = 0 };
            _walls.Add(wall);
        }
    }

    public bool IsLayerComplete(int layer)
    {
        if (layer == 0) return true;
        if (layer == 1) return _isLayer1Activated;
        // 其它层：该层的所有墙块都必须有 HP（HP > 0）
        var layerWalls = _walls.Where(w => w.Layer == layer).ToList();
        if (layerWalls.Count == 0) return false; // 该层尚未创建
        return layerWalls.All(w => w.HP > 0);
    }

    public bool IsUnderThreat()
    {
        return _monsters.Any(m => m.IsActive && !m.IsDead);
    }

    public float GetNearestMonsterDist(float x, float y)
    {
        float minDist = float.MaxValue;
        foreach (var m in _monsters)
        {
            if (m.IsActive && !m.IsDead)
            {
                float dx = m.X - x, dy = m.Y - y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) minDist = dist;
            }
        }
        return minDist;
    }

    private void UpdateWallScaling()
    {
        // 动态计算每一层应有的半径，确保不重叠
        // 基础半径 150，每层间距 300，且随基地等级略微扩张
        foreach (var wall in _walls)
        {
            float targetRadius = (150 + wall.Layer * 300) + _baseLevel * 10;
            float targetThickness = (8 + wall.Layer * 5) + _baseLevel * 2;
            int targetMaxHP = (1000 + wall.Layer * 4000) + _baseLevel * 1000;

            wall.Radius = wall.Radius * 0.95f + targetRadius * 0.05f;
            wall.Thickness = targetThickness;
            wall.MaxHP = targetMaxHP;
        }
    }

    public bool HasWallGaps()
    {
        // 只有内层缺口才视为紧急撤回信号
        return _walls.Any(w => w.Layer == 0 && w.HP <= 0);
    }

    public int MaxActiveLayer()
    {
        return _walls.Where(w => w.HP > 0).Max(w => (int?)w.Layer) ?? 0;
    }

    // 基地升级时触发城墙扩建的蓝图生成函数
    private void BuildOuterLayerBlueprint(int layerIndex)
    {
        if (_walls.Any(w => w.Layer == layerIndex)) return;

        // 每多一层，圈块增加12，半径增加300，血量指数级增强
        int segments = 24 + layerIndex * 12;
        float radius = 150 + layerIndex * 300;
        int maxHp = 1000 + layerIndex * 2000;
        float thickness = 10 + layerIndex * 5;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)(i * Math.PI * 2 / segments);
            var wall = new WallSegment(angle, radius, thickness, maxHp) { Layer = layerIndex, HP = 0 };
            _walls.Add(wall);
        }

        AddFloatingText(this.ClientSize.Width / 2, this.ClientSize.Height / 2, $"外环扩张方案: 环区 {layerIndex} 已启动!", Color.Orange);
        AudioManager.PlaySound("build");
    }

    public WallSegment? GetWeakestWall(Robot caller)
    {
        // 自动按层级从内向外建造/修理
        var best = _walls.Where(w => (w.LockingRobot == null || w.LockingRobot == caller) && w.HP < w.MaxHP)
                         .OrderBy(w => w.Layer * 1000000 + (w.HP <= 0 ? -10000 : w.HP)) 
                         .FirstOrDefault();

        if (best != null) best.LockingRobot = caller;
        return best;
    }

    public WallSegment? GetGarrisonWall(Robot caller, Monster threat)
    {
        // 自动寻找当前已开工（HP>0）的最外层大本营层级
        int maxActiveLayer = _walls.Where(w => w.HP > 0).Max(w => (int?)w.Layer) ?? 0;
        
        var br = GetBaseRobot();
        
        var best = _walls.Where(w => w.IsActive && 
                                    (w.Layer == maxActiveLayer) && 
                                    (w.GarrisonRobot == null || w.GarrisonRobot == caller))
                         .OrderBy(w => {
                             var wp = w.GetWorldPosition(br?.X ?? 0, br?.Y ?? 0);
                             return Math.Pow(wp.X - threat.X, 2) + Math.Pow(wp.Y - threat.Y, 2);
                         })
                         .FirstOrDefault();

        if (best != null) best.GarrisonRobot = caller;
        return best;
    }

    public void ReleaseGarrison(Robot caller)
    {
        foreach (var w in _walls)
        {
            if (w.GarrisonRobot == caller) w.GarrisonRobot = null;
        }
    }

    private void RenderWalls(Graphics g, float bx, float by)
    {
        foreach (var wall in _walls)
        {
            int layerCount = _walls.Count(w => w.Layer == wall.Layer);
            float sweepAngle = 360f / (layerCount > 0 ? layerCount : 24);
            bool isLayerComplete = _walls.Where(w => w.Layer == wall.Layer).All(w => w.HP > 0);

            float wx = bx + (float)Math.Cos(wall.Angle) * wall.Radius;
            float wy = by + (float)Math.Sin(wall.Angle) * wall.Radius;
            float startAngle = (float)(wall.Angle * 180 / Math.PI) - sweepAngle / 2;

            // 基础颜色判定
            float hpPercent = (float)wall.HP / wall.MaxHP;
            Color baseColor;
            
            if (wall.HP <= 0)
            {
                // 等待修筑的淡色细线模板
                baseColor = wall.Layer > 0 ? Color.FromArgb(40, 255, 200, 0) : Color.FromArgb(40, 0, 255, 255);
            }
            else if (wall.Layer > 0 && !isLayerComplete)
            {
                // 仍在全层建设期时
                baseColor = Color.FromArgb(100, 255, 180, 0);
            }
            else
            {
                // 已建成、激活防御状态的渐变颜色 (按血量)
                baseColor = Color.FromArgb(200, (int)(255 * (1 - hpPercent)), (int)(255 * hpPercent), 
                                              wall.Layer > 0 ? (hpPercent > 0.8f ? 255 : 100) : 100);
            }

            Color wallColor = wall.HitFlashTimer > 0 ? Color.White : baseColor;

            using (var pen = new Pen(Color.FromArgb(100, wallColor), 2))
            using (var brush = new SolidBrush(wallColor))
            {
                float drawSweep = (wall.Layer > 0 && !isLayerComplete && wall.HP > 0) ? sweepAngle * hpPercent : sweepAngle;
                
                if (Math.Abs(drawSweep) > 0.1f)
                {
                    g.DrawArc(pen, bx - wall.Radius, by - wall.Radius, wall.Radius * 2, wall.Radius * 2, startAngle, drawSweep);
                }

                if (wall.HP > 0)
                {
                    g.TranslateTransform(wx, wy);
                    g.RotateTransform((float)(wall.Angle * 180 / Math.PI));
                    g.FillRectangle(brush, -wall.Thickness / 2, -wall.Thickness / 2, wall.Thickness, wall.Thickness * 2);
                    
                    g.ResetTransform();
                    g.TranslateTransform(this.ClientSize.Width / 2f + _panX, this.ClientSize.Height / 2f + _panY);
                    g.ScaleTransform(1.0f / _worldViewFactor, 1.0f / _worldViewFactor);
                    g.TranslateTransform(-bx, -by);
                }
            }
        }
    }

    private void HandleMonsterWallCollision()
    {
        var b = GetBaseRobot();
        if (b == null) return;
        float bx = b.X + b.Size / 2, by = b.Y + b.Size / 2;

        foreach (var m in _monsters)
        {
            if (!m.IsActive || m.IsDead) continue;
            
            // --- 真正的圆心物理定位 ---
            float mx = m.X + m.Size / 2f, my = m.Y + m.Size / 2f;
            float dx = mx - bx, dy = my - by;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            foreach (var wall in _walls)
            {
                bool isLayerComplete = _walls.Where(w => w.Layer == wall.Layer).All(w => w.HP > 0);
                
                // 层级过滤：外圈未开机视为虚空
                if (wall.Layer > 0 && !isLayerComplete) continue;
                if (wall.HP <= 0) continue;
                
                float angle = (float)Math.Atan2(dy, dx);
                if (angle < 0) angle += (float)(Math.PI * 2);

                float angleDiff = (float)Math.Abs(angle - wall.Angle);
                if (angleDiff > Math.PI) angleDiff = (float)(Math.PI * 2 - angleDiff);

                int layerCount = _walls.Count(w => w.Layer == wall.Layer);
                float halfSweep = (float)Math.PI / (layerCount > 0 ? layerCount : 24);
                if (angleDiff < halfSweep + 0.05f) 

                {
                    // 检测带 + 击退逻辑锁定：务必保持 += 符号以实现向外反弹！
                // --- 核心修复：防止分母为零导致怪物坐标变为 NaN ---
                // 当怪物极度接近基地圆心（dist < 0.1f）时，不执行基于向向量的推力计算
                if (dist > 0.1f && Math.Abs(dist - wall.Radius) < (wall.Thickness + m.Size / 2f) + 5f)
                {
                    // 只有在怪物位于墙体外侧（dist > wall.Radius）时才向外推
                    if (dist > wall.Radius - 5f) 
                    {
                        m.X += (dx / dist) * 15f; 
                        m.Y += (dy / dist) * 15f;
                        
                        int wallDmg = (10 + CurrentWave) * (m.IsElite ? 5 : 1); 
                        wall.TakeDamage(wallDmg);
                    }
                }
                }
            }
        }
    }

    public List<Monster> GetAllMonsters() => _monsters;
}

/// <summary>
/// 空间网格系统 - 优化大规模实体的查询性能 (O(N))
/// </summary>
public class SpatialGrid
{
    private readonly int _cellSize;
    private readonly ConcurrentDictionary<(int, int), List<Monster>> _cells = new();

    public SpatialGrid(int cellSize = 200) { _cellSize = cellSize; }

    private (int, int) GetCell(float x, float y) => ((int)Math.Floor(x / _cellSize), (int)Math.Floor(y / _cellSize));

    public void Clear() => _cells.Clear();

    public void Add(Monster m)
    {
        var key = GetCell(m.X, m.Y);
        _cells.GetOrAdd(key, _ => new List<Monster>()).Add(m);
    }

    public IEnumerable<Monster> GetNearby(float x, float y)
    {
        var (cx, cy) = GetCell(x, y);
        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
                if (_cells.TryGetValue((cx + i, cy + j), out var list))
                    foreach (var m in list) yield return m;
    }
}
