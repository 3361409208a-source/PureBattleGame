using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PureBattleGame.Games.CodeSurvivor;

/// <summary>
/// 伪装IDE主窗口 - 看起来像VS Code，实际上是横版过关游戏
/// </summary>
public partial class CodeEditorForm : Form
{
    private GameWorld _world = null!;
    private List<string> _terminalHistory = new();
    private int _historyIndex = -1;

    // UI组件
    private TreeView _fileTree = null!;
    private RichTextBox _codeEditor = null!;
    private Panel _terminalPanel = null!;
    private RichTextBox _terminalOutput = null!;
    private TextBox _terminalInput = null!;
    private Label _statusBar = null!;
    private Label _fileTab = null!;

    // 游戏循环
    private System.Windows.Forms.Timer _gameTimer = null!;
    private HashSet<Keys> _pressedKeys = new();

    // 游戏状态
    private bool _gameOver = false;
    private bool _levelComplete = false;
    private int _frameCount = 0;

    public CodeEditorForm()
    {
        InitializeComponent();
        InitializeGame();
    }

    private void InitializeComponent()
    {
        this.Text = "Visual Studio Code - project/src/level1.js";
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.FromArgb(212, 212, 212);
        this.Opacity = 0.95;
        this.MinimumSize = new Size(800, 600);

        // 主布局面板
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        this.Controls.Add(mainPanel);

        // === 顶部菜单栏（伪装） ===
        var menuBar = CreateMenuBar();
        mainPanel.Controls.Add(menuBar);

        // === 左侧文件树 ===
        _fileTree = CreateFileTree();
        _fileTree.Location = new Point(0, 25);
        _fileTree.Size = new Size(220, this.ClientSize.Height - 150);
        mainPanel.Controls.Add(_fileTree);

        // 活动栏图标（伪装VS Code左侧）
        var activityBar = CreateActivityBar();
        activityBar.Location = new Point(0, 25);
        activityBar.Size = new Size(48, this.ClientSize.Height - 150);
        _fileTree.Location = new Point(48, 25);
        _fileTree.Width = 200;
        mainPanel.Controls.Add(activityBar);

        // === 编辑器区域 ===
        var editorContainer = CreateEditorContainer();
        editorContainer.Location = new Point(248, 25);
        editorContainer.Size = new Size(this.ClientSize.Width - 248, this.ClientSize.Height - 200);
        mainPanel.Controls.Add(editorContainer);

        // === 底部终端 ===
        _terminalPanel = CreateTerminalPanel();
        _terminalPanel.Location = new Point(248, this.ClientSize.Height - 175);
        _terminalPanel.Size = new Size(this.ClientSize.Width - 248, 150);
        mainPanel.Controls.Add(_terminalPanel);

        // === 底部状态栏 ===
        _statusBar = CreateStatusBar();
        _statusBar.Location = new Point(0, this.ClientSize.Height - 25);
        _statusBar.Size = new Size(this.ClientSize.Width, 25);
        mainPanel.Controls.Add(_statusBar);

        // 窗体大小改变时调整
        this.Resize += (s, e) => {
            _fileTree.Height = this.ClientSize.Height - 150;
            activityBar.Height = this.ClientSize.Height - 150;
            editorContainer.Size = new Size(this.ClientSize.Width - 248, this.ClientSize.Height - 200);
            _terminalPanel.Location = new Point(248, this.ClientSize.Height - 175);
            _terminalPanel.Width = this.ClientSize.Width - 248;
            _statusBar.Location = new Point(0, this.ClientSize.Height - 25);
            _statusBar.Width = this.ClientSize.Width;

            // 更新游戏屏幕尺寸
            if (_world != null)
            {
                _world.ScreenWidth = this.ClientSize.Width - 280;
                _world.ScreenHeight = this.ClientSize.Height - 250;
            }
        };

        // 键盘处理
        this.KeyPreview = true;
        this.KeyDown += CodeEditorForm_KeyDown;
        this.KeyUp += CodeEditorForm_KeyUp;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Alt 快捷键
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
                    this.Opacity = (this.Tag is double op) ? op : 0.95;
                    this.ShowInTaskbar = true;
                }
                return true;
            }
            // Alt + Q: 返回主页
            else if (baseKey == Keys.Q)
            {
                ReturnToHome();
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private Panel CreateMenuBar()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 25,
            BackColor = Color.FromArgb(51, 51, 51)
        };

        string[] menus = { "文件", "编辑", "选择", "查看", "转到", "运行", "终端", "帮助" };
        int x = 10;
        foreach (var menu in menus)
        {
            var lbl = new Label
            {
                Text = menu,
                ForeColor = Color.FromArgb(204, 204, 204),
                Font = new Font("Microsoft YaHei", 8),
                AutoSize = true,
                Location = new Point(x, 5),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(lbl);
            x += 45;
        }

        return panel;
    }

    private Panel CreateActivityBar()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(51, 51, 51)
        };

        string[] icons = { "📁", "🔍", "🌿", "🐛", "📦" };
        for (int i = 0; i < icons.Length; i++)
        {
            var btn = new Label
            {
                Text = icons[i],
                Font = new Font("Segoe UI", 14),
                ForeColor = i == 0 ? Color.White : Color.FromArgb(128, 128, 128),
                Size = new Size(48, 48),
                Location = new Point(0, 10 + i * 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(btn);
        }

        return panel;
    }

    private TreeView CreateFileTree()
    {
        var tree = new TreeView
        {
            BackColor = Color.FromArgb(37, 37, 38),
            ForeColor = Color.FromArgb(212, 212, 212),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 10),
            ShowLines = false,
            ShowPlusMinus = false,
            FullRowSelect = true,
            HideSelection = false
        };

        tree.AfterSelect += (s, e) => {
            if (e.Node?.Tag is Skill skill)
            {
                ShowSkillInfo(skill);
            }
        };

        return tree;
    }

    private Panel CreateEditorContainer()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(30, 30, 30)
        };

        // 文件标签栏
        var tabPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 35,
            BackColor = Color.FromArgb(45, 45, 45)
        };

        _fileTab = new Label
        {
            Text = "  📝 level1.js",
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Font = new Font("Microsoft YaHei", 9),
            AutoSize = false,
            Size = new Size(120, 35),
            Location = new Point(0, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };
        tabPanel.Controls.Add(_fileTab);
        panel.Controls.Add(tabPanel);

        // 代码编辑器
        _codeEditor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(212, 212, 212),
            Font = new Font("Consolas", 11),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            WordWrap = false,
            SelectionTabs = new int[] { 40 }
        };

        // 启用双缓冲减少闪烁
        typeof(RichTextBox).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, _codeEditor, new object[] { true });

        panel.Controls.Add(_codeEditor);
        _codeEditor.BringToFront();

        return panel;
    }

    private Panel CreateTerminalPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(30, 30, 30),
            BorderStyle = BorderStyle.FixedSingle
        };

        // 终端标签
        var tabLabel = new Label
        {
            Text = "TERMINAL",
            ForeColor = Color.White,
            BackColor = Color.FromArgb(45, 45, 45),
            Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
            AutoSize = false,
            Size = new Size(80, 25),
            Location = new Point(0, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };
        panel.Controls.Add(tabLabel);

        // 终端输出区域
        _terminalOutput = new RichTextBox
        {
            Location = new Point(0, 25),
            Size = new Size(panel.Width, 100),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(212, 212, 212),
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        panel.Controls.Add(_terminalOutput);

        // 终端输入
        _terminalInput = new TextBox
        {
            Location = new Point(0, 125),
            Size = new Size(panel.Width, 25),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.None
        };
        _terminalInput.KeyDown += TerminalInput_KeyDown;
        panel.Controls.Add(_terminalInput);

        // 调整大小时更新
        panel.Resize += (s, e) => {
            _terminalOutput.Width = panel.Width;
            _terminalInput.Width = panel.Width;
            _terminalInput.Top = panel.Height - 25;
            _terminalOutput.Height = panel.Height - 50;
        };

        return panel;
    }

    private Label CreateStatusBar()
    {
        var lbl = new Label
        {
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 9),
            TextAlign = ContentAlignment.MiddleLeft
        };
        return lbl;
    }

    private void InitializeGame()
    {
        _world = new GameWorld();
        _world.ScreenWidth = this.ClientSize.Width - 280;
        _world.ScreenHeight = this.ClientSize.Height - 250;

        // 创建游戏循环计时器
        _gameTimer = new System.Windows.Forms.Timer();
        _gameTimer.Interval = 16; // ~60 FPS
        _gameTimer.Tick += GameLoop;
        _gameTimer.Start();

        UpdateFileTree();
        RenderGame();
        PrintTerminal("Welcome to CodeSurvivor - Platformer Mode!");
        PrintTerminal("Controls: ← → Move | SPACE Jump | Alt+↑↓ Opacity | Alt+Space BossKey | Alt+Q Home");
        PrintTerminal("Jump on enemies to defeat them! Reach the flag 🚩 to complete the level.");
        PrintTerminal("");
        UpdateStatusBar();
    }

    private void GameLoop(object? sender, EventArgs e)
    {
        if (_gameOver) return;

        _frameCount++;

        // 处理输入
        UpdateInput();

        // 更新物理
        _world.UpdatePhysics();

        // 检查关卡完成
        if (_world.IsLevelComplete() && !_levelComplete)
        {
            _levelComplete = true;
            PrintTerminal("");
            PrintTerminal("🎉 Level Complete!");
            PrintTerminal($"💰 Coins collected: {_world.Player.Coins}");
            PrintTerminal($"⭐ Score: {_world.Player.Score}");
            PrintTerminal("Loading next level...");

            // 延迟进入下一关
            var delayTimer = new System.Windows.Forms.Timer();
            delayTimer.Interval = 2000;
            delayTimer.Tick += (s, evt) => {
                delayTimer.Stop();
                _world.NextLevel();
                _levelComplete = false;
                _fileTab.Text = $"  📝 level{_world.CurrentLevel}.js";
                this.Text = $"Visual Studio Code - project/src/level{_world.CurrentLevel}.js";
                PrintTerminal($"");
                PrintTerminal($"📂 Level {_world.CurrentLevel} Loaded!");
            };
            delayTimer.Start();
        }

        // 检查玩家死亡
        if (_world.Player.HP <= 0 && !_gameOver)
        {
            _gameOver = true;
            PrintTerminal("");
            PrintTerminal("╔═══════════════════════════════════════╗");
            PrintTerminal("║            GAME OVER                  ║");
            PrintTerminal($"║    Level: {_world.CurrentLevel} | Score: {_world.Player.Score}         ║");
            PrintTerminal("╚═══════════════════════════════════════╝");
            PrintTerminal("");
            PrintTerminal("Type 'restart' to try again");
        }

        // 渲染 - 每3帧渲染一次，减少闪烁
        if (_frameCount % 3 == 0)
        {
            RenderGame();
        }
        UpdateStatusBar();
    }

    private void UpdateInput()
    {
        // 左右移动
        if (_pressedKeys.Contains(Keys.Left))
        {
            _world.Player.VelX = -_world.Player.Speed;
        }
        else if (_pressedKeys.Contains(Keys.Right))
        {
            _world.Player.VelX = _world.Player.Speed;
        }
        else
        {
            _world.Player.VelX *= 0.8f; // 摩擦力
            if (Math.Abs(_world.Player.VelX) < 0.1f) _world.Player.VelX = 0;
        }

        // 跳跃
        if (_pressedKeys.Contains(Keys.Space))
        {
            if (_world.Player.IsGrounded)
            {
                _world.Player.Jump();
            }
        }
    }

    private void UpdateFileTree()
    {
        _fileTree.Nodes.Clear();

        var root = _fileTree.Nodes.Add("🗂️ project");

        var srcNode = root.Nodes.Add("📁 src");
        foreach (var skill in _world.Player.Skills)
        {
            var node = srcNode.Nodes.Add($"📄 {skill.Id}.js");
            node.Tag = skill;
            node.ToolTipText = skill.Description;
        }

        var levelNode = root.Nodes.Add("📁 levels");
        for (int i = 1; i <= 5; i++)
        {
            var node = levelNode.Nodes.Add($"📄 level{i}.js");
            if (i <= _world.CurrentLevel)
            {
                node.ForeColor = Color.FromArgb(212, 212, 212);
            }
            else
            {
                node.ForeColor = Color.Gray;
            }
        }

        var itemNode = root.Nodes.Add($"📦 inventory ({_world.Player.Coins} coins)");
        itemNode.Nodes.Add($"💰 Coins: {_world.Player.Coins}");
        itemNode.Nodes.Add($"⭐ Score: {_world.Player.Score}");

        root.ExpandAll();
    }

    private void ShowSkillInfo(Skill skill)
    {
        PrintTerminal($"");
        PrintTerminal($"// Skill: {skill.Name}");
        PrintTerminal($"// Type: {skill.Type}");
        PrintTerminal($"// Description: {skill.Description}");
        PrintTerminal($"");
    }

    private void RenderGame()
    {
        _codeEditor.Clear();

        var sb = new StringBuilder();
        float camX = _world.CameraX;

        // 文件头注释
        sb.AppendLine("/**");
        sb.AppendLine($" * Level: {_world.CurrentLevel} | FPS: {1000 / 16}");
        sb.AppendLine($" * Camera: ({camX:F0}, 0)");
        sb.AppendLine($" * Player: ({_world.Player.X:F0}, {_world.Player.Y:F0})");
        sb.AppendLine(" */");
        sb.AppendLine();

        // 代码样式声明
        sb.AppendLine("const level = {");
        sb.AppendLine($"    width: {_world.LevelWidth},");
        sb.AppendLine($"    camera: {{ x: {camX:F0} }},");
        sb.AppendLine();

        // 渲染平台
        sb.AppendLine("    // Platforms");
        sb.AppendLine("    platforms: [");
        foreach (var platform in _world.Platforms)
        {
            if (_world.IsOnScreen(platform.X, platform.Width))
            {
                int screenX = (int)(platform.X - camX);
                string type = platform.Type switch
                {
                    PlatformType.Moving => "moving",
                    PlatformType.Breaking => "breaking",
                    PlatformType.Spring => "spring",
                    _ => "static"
                };
                sb.AppendLine($"        {{ x: {screenX}, y: {(int)platform.Y}, w: {(int)platform.Width}, type: '{type}' }}, " +
                             $"// at ({platform.X:F0}, {platform.Y:F0})");
            }
        }
        sb.AppendLine("    ],");
        sb.AppendLine();

        // 渲染收集品
        sb.AppendLine("    // Collectibles");
        sb.AppendLine("    collectibles: [");
        foreach (var item in _world.Collectibles.Where(c => !c.IsCollected))
        {
            if (_world.IsOnScreen(item.X, item.Width))
            {
                int screenX = (int)(item.X - camX);
                string type = item.Type switch
                {
                    CollectibleType.Coin => "coin",
                    CollectibleType.PowerUp => "powerup",
                    CollectibleType.Flag => "flag",
                    _ => "unknown"
                };
                sb.AppendLine($"        {{ x: {screenX}, y: {(int)item.Y}, type: '{type}', icon: '{item.DisplayChar}' }}, " +
                             $"// at ({item.X:F0}, {item.Y:F0})");
            }
        }
        sb.AppendLine("    ],");
        sb.AppendLine();

        // 渲染敌人
        sb.AppendLine("    // Enemies");
        sb.AppendLine("    enemies: [");
        foreach (var enemy in _world.Enemies.Where(e => !e.IsDead))
        {
            if (_world.IsOnScreen(enemy.X, enemy.Width))
            {
                int screenX = (int)(enemy.X - camX);
                string direction = enemy.MovingRight ? "right" : "left";
                sb.AppendLine($"        {{ x: {screenX}, y: {(int)enemy.Y}, type: '{enemy.Name}', " +
                             $"hp: {enemy.HP}/{enemy.MaxHP}, dir: '{direction}', icon: '{enemy.DisplayChar}' }}, " +
                             $"// at ({enemy.X:F0}, {enemy.Y:F0})");
            }
        }
        sb.AppendLine("    ],");
        sb.AppendLine();

        // 渲染玩家
        sb.AppendLine("    // Player");
        int playerScreenX = (int)(_world.Player.X - camX);
        string playerFacing = _world.Player.FacingRight ? "right" : "left";
        string playerState = _world.Player.IsGrounded ? "grounded" : "air";
        sb.AppendLine($"    player: {{");
        sb.AppendLine($"        x: {playerScreenX},");
        sb.AppendLine($"        y: {(int)_world.Player.Y},");
        sb.AppendLine($"        facing: '{playerFacing}',");
        sb.AppendLine($"        state: '{playerState}',");
        sb.AppendLine($"        hp: {_world.Player.HP}/{_world.Player.MaxHP},");
        sb.AppendLine($"        coins: {_world.Player.Coins},");
        sb.AppendLine($"        vel: {{ x: {_world.Player.VelX:F1}, y: {_world.Player.VelY:F1} }},");
        sb.AppendLine($"        icon: '👨‍💻'");
        sb.AppendLine($"    }},");
        sb.AppendLine();

        // 渲染地图可视化（ASCII艺术风格）
        sb.AppendLine("    // Map visualization (camera view)");
        sb.AppendLine("    view: [");

        // 简化渲染：用字符表示游戏画面
        int viewWidth = 50;
        int viewHeight = 12;

        for (int row = 0; row < viewHeight; row++)
        {
            sb.Append("        \"");
            float worldY = _world.GroundY - (viewHeight - row) * 40;

            for (int col = 0; col < viewWidth; col++)
            {
                float worldX = camX + col * 20;
                char display = ' ';

                // 检查玩家
                if (Math.Abs(worldX - _world.Player.X) < 15 && Math.Abs(worldY - _world.Player.Y) < 20)
                {
                    display = _world.Player.FacingRight ? '>' : '<';
                    if (_world.Player.InvincibleTime > 0 && _frameCount % 4 < 2)
                        display = ' '; // 闪烁效果
                }
                // 检查敌人
                else
                {
                    var enemy = _world.Enemies.FirstOrDefault(e =>
                        !e.IsDead &&
                        Math.Abs(worldX - e.X) < 14 &&
                        Math.Abs(worldY - e.Y) < 14);
                    if (enemy != null)
                    {
                        display = enemy.DisplayChar[0];
                    }
                    // 检查平台
                    else
                    {
                        var platform = _world.Platforms.FirstOrDefault(p =>
                            worldX >= p.X && worldX < p.X + p.Width &&
                            Math.Abs(worldY - p.Y) < 10);
                        if (platform != null)
                        {
                            display = '=';
                        }
                        // 检查收集品
                        else
                        {
                            var item = _world.Collectibles.FirstOrDefault(c =>
                                !c.IsCollected &&
                                Math.Abs(worldX - c.X) < 10 &&
                                Math.Abs(worldY - c.Y) < 10);
                            if (item != null)
                            {
                                display = item.DisplayChar[0];
                            }
                        }
                    }
                }

                sb.Append(display);
            }
            sb.AppendLine("\",");
        }
        sb.AppendLine("    ],");
        sb.AppendLine("};");

        // 游戏提示
        sb.AppendLine();
        sb.AppendLine("// Tips:");
        sb.AppendLine("// - Use Arrow Keys to move left/right");
        sb.AppendLine("// - Press SPACE to jump");
        sb.AppendLine("// - Jump on enemies to defeat them");
        sb.AppendLine($"// - Reach the flag to complete the level");

        _codeEditor.Text = sb.ToString();
        ApplySyntaxHighlighting();
    }

    private void ApplySyntaxHighlighting()
    {
        // 简单语法高亮
        string text = _codeEditor.Text;

        // 高亮注释
        for (int i = 0; i < text.Length - 1; i++)
        {
            if (text[i] == '/' && text[i + 1] == '/')
            {
                int end = text.IndexOf('\n', i);
                if (end == -1) end = text.Length;
                _codeEditor.Select(i, end - i);
                _codeEditor.SelectionColor = Color.FromArgb(106, 153, 85);
            }
        }

        // 高亮关键字
        string[] keywords = { "const", "var", "let", "function", "class", "if", "else" };
        foreach (var keyword in keywords)
        {
            int index = 0;
            while ((index = text.IndexOf(keyword, index)) != -1)
            {
                _codeEditor.Select(index, keyword.Length);
                _codeEditor.SelectionColor = Color.FromArgb(86, 156, 214);
                index += keyword.Length;
            }
        }

        // 高亮字符串
        int strIndex = 0;
        while (strIndex < text.Length)
        {
            int quoteStart = text.IndexOf('"', strIndex);
            if (quoteStart == -1) break;
            int quoteEnd = text.IndexOf('"', quoteStart + 1);
            if (quoteEnd == -1) break;
            _codeEditor.Select(quoteStart, quoteEnd - quoteStart + 1);
            _codeEditor.SelectionColor = Color.FromArgb(206, 145, 120);
            strIndex = quoteEnd + 1;
        }

        // 滚动到玩家位置
        _codeEditor.SelectionStart = _codeEditor.Text.Length;
        _codeEditor.ScrollToCaret();
    }

    private void TerminalInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            var cmd = _terminalInput.Text.Trim();
            if (!string.IsNullOrEmpty(cmd))
            {
                _terminalHistory.Add(cmd);
                _historyIndex = _terminalHistory.Count;
                PrintTerminal($"> {cmd}");
                ExecuteCommand(cmd);
                _terminalInput.Clear();
            }
        }
        else if (e.KeyCode == Keys.Up)
        {
            e.SuppressKeyPress = true;
            if (_historyIndex > 0)
            {
                _historyIndex--;
                _terminalInput.Text = _terminalHistory[_historyIndex];
                _terminalInput.SelectionStart = _terminalInput.Text.Length;
            }
        }
        else if (e.KeyCode == Keys.Down)
        {
            e.SuppressKeyPress = true;
            if (_historyIndex < _terminalHistory.Count - 1)
            {
                _historyIndex++;
                _terminalInput.Text = _terminalHistory[_historyIndex];
                _terminalInput.SelectionStart = _terminalInput.Text.Length;
            }
            else
            {
                _historyIndex = _terminalHistory.Count;
                _terminalInput.Clear();
            }
        }
    }

    private void ExecuteCommand(string cmd)
    {
        var parts = cmd.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        switch (parts[0])
        {
            case "help":
            case "?":
                ShowHelp();
                break;
            case "restart":
                RestartGame();
                break;
            case "status":
            case "st":
                ShowStatus();
                break;
            case "next":
            case "level":
                if (_levelComplete)
                    _world.NextLevel();
                else
                    PrintTerminal("Complete the current level first!");
                break;
            case "clear":
            case "cls":
                _terminalOutput.Clear();
                break;
            case "git":
                FakeGitCommand(parts);
                break;
            case "npm":
                FakeNpmCommand(parts);
                break;
            case "ls":
            case "dir":
                PrintTerminal("level1.js  level2.js  player.js  utils.js");
                break;
            case "cat":
            case "type":
                if (parts.Length > 1) FakeCatCommand(parts[1]);
                else PrintTerminal("Usage: cat [filename]");
                break;
            default:
                PrintTerminal($"Unknown command: '{parts[0]}'. Type 'help' for available commands.");
                break;
        }

        UpdateFileTree();
        UpdateStatusBar();
    }

    private void ShowHelp()
    {
        PrintTerminal("");
        PrintTerminal("=== Available Commands ===");
        PrintTerminal("");
        PrintTerminal("GAME:");
        PrintTerminal("  restart      - Restart the game");
        PrintTerminal("  status       - Show player status");
        PrintTerminal("");
        PrintTerminal("CONTROLS:");
        PrintTerminal("  ← →          - Move left/right");
        PrintTerminal("  SPACE        - Jump");
        PrintTerminal("  Alt+↑↓       - Adjust opacity");
        PrintTerminal("  Alt+Space    - Boss key (hide)");
        PrintTerminal("  Alt+Q        - Return to home");
        PrintTerminal("");
        PrintTerminal("MISC:");
        PrintTerminal("  git [cmd]    - Pretend to use git");
        PrintTerminal("  npm [cmd]    - Pretend to use npm");
        PrintTerminal("  clear        - Clear terminal");
        PrintTerminal("  help         - Show this help");
        PrintTerminal("");
    }

    private void RestartGame()
    {
        _gameOver = false;
        _levelComplete = false;
        _world = new GameWorld();
        _world.ScreenWidth = this.ClientSize.Width - 280;
        _world.ScreenHeight = this.ClientSize.Height - 250;
        PrintTerminal("Game restarted!");
        UpdateFileTree();
        RenderGame();
    }

    private void ShowStatus()
    {
        PrintTerminal("");
        PrintTerminal("=== Player Status ===");
        PrintTerminal($"Level: {_world.CurrentLevel}");
        PrintTerminal($"HP: {_world.Player.HP}/{_world.Player.MaxHP}");
        PrintTerminal($"Coins: {_world.Player.Coins}");
        PrintTerminal($"Score: {_world.Player.Score}");
        PrintTerminal($"Position: ({_world.Player.X:F1}, {_world.Player.Y:F1})");
        PrintTerminal($"Velocity: ({_world.Player.VelX:F1}, {_world.Player.VelY:F1})");
        PrintTerminal($"State: {(_world.Player.IsGrounded ? "grounded" : "air")}");
        PrintTerminal("");
    }

    private void FakeGitCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            PrintTerminal("Usage: git [commit|push|status|log]");
            return;
        }

        switch (parts[1])
        {
            case "status":
                PrintTerminal($"On level {_world.CurrentLevel}");
                PrintTerminal($"Changes: {_world.Enemies.Count} enemies remaining");
                PrintTerminal($"Staged: {_world.Collectibles.Count(c => c.IsCollected)} collectibles found");
                break;
            case "commit":
                PrintTerminal($"[{_world.CurrentLevel}] feat: jumped over {_world.Player.Score / 10} bugs");
                break;
            case "push":
                PrintTerminal("Enumerating objects... Done.");
                PrintTerminal("remote: This is a game, not a real git repo! 😄");
                break;
            case "log":
                PrintTerminal($"commit {Guid.NewGuid().ToString()[..7]}");
                PrintTerminal($"Author: Player <player@codesurvivor.game>");
                PrintTerminal($"Date: {DateTime.Now}");
                PrintTerminal("");
                PrintTerminal($"    Platformer adventure on level {_world.CurrentLevel}");
                break;
            default:
                PrintTerminal($"git: '{parts[1]}' is not a git command.");
                break;
        }
    }

    private void FakeNpmCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            PrintTerminal("Usage: npm [install|run|build]");
            return;
        }

        switch (parts[1])
        {
            case "install":
                PrintTerminal("📦 Installing dependencies...");
                PrintTerminal("added 1 package: extra-life");
                _world.Player.Heal(30);
                break;
            case "run":
                PrintTerminal("> codesurvivor@1.0.0 start");
                PrintTerminal("> The game is already running!");
                break;
            case "build":
                PrintTerminal("> Building project...");
                PrintTerminal("> ✓ Build complete!");
                _world.Player.Score += 25;
                PrintTerminal("> ⭐ Score boosted!");
                break;
            default:
                PrintTerminal($"npm: Unknown command '{parts[1]}'");
                break;
        }
    }

    private void FakeCatCommand(string filename)
    {
        switch (filename.ToLower())
        {
            case "player.js":
                PrintTerminal("class Player {");
                PrintTerminal("  constructor() {");
                PrintTerminal($"    this.hp = {_world.Player.HP};");
                PrintTerminal($"    this.x = {_world.Player.X:F0};");
                PrintTerminal($"    this.y = {_world.Player.Y:F0};");
                PrintTerminal("  }");
                PrintTerminal("  jump() { this.vy = -12; }");
                PrintTerminal("}");
                break;
            case "level1.js":
                PrintTerminal("const level = {");
                PrintTerminal($"  width: {_world.LevelWidth},");
                PrintTerminal($"  platforms: {_world.Platforms.Count},");
                PrintTerminal($"  enemies: {_world.Enemies.Count}");
                PrintTerminal("};");
                break;
            default:
                PrintTerminal($"cat: {filename}: No such file or directory");
                break;
        }
    }

    private void CodeEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Add(e.KeyCode);

        // 确保焦点在窗体上而不是输入框上
        if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Space)
        {
            this.Focus();
        }
    }

    private void CodeEditorForm_KeyUp(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Remove(e.KeyCode);
    }

    private void PrintTerminal(string text)
    {
        _terminalOutput.AppendText(text + Environment.NewLine);
        _terminalOutput.ScrollToCaret();
    }

    private void UpdateStatusBar()
    {
        string status = $"  Ln {(int)_world.Player.Y}, Col {(int)_world.Player.X}  |  UTF-8  |  JavaScript  |  " +
                       $"🎮 Level: {_world.CurrentLevel}  |  " +
                       $"❤️ {_world.Player.HP}/{_world.Player.MaxHP}  |  " +
                       $"💰 {_world.Player.Coins}  |  " +
                       $"⭐ {_world.Player.Score}";
        _statusBar.Text = status;
    }

    private void ReturnToHome()
    {
        _gameTimer?.Stop();
        if (Core.MoyuLauncher.Instance != null)
        {
            Core.MoyuLauncher.Instance.Show();
            this.Hide();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _gameTimer?.Stop();
        base.OnFormClosing(e);
    }
}
