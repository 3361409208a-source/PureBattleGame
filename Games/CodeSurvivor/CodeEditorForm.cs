using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PureBattleGame.Games.CodeSurvivor;

/// <summary>
/// 伪装IDE主窗口 - 看起来像VS Code，实际上是Roguelike游戏
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

    // 游戏状态
    private bool _gameOver = false;
    private List<string> _combatLog = new();
    private int _turnCount = 0;

    public CodeEditorForm()
    {
        InitializeComponent();
        InitializeGame();
    }

    private void InitializeComponent()
    {
        this.Text = "Visual Studio Code - project/src/player.js";
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.FromArgb(212, 212, 212);
        this.Opacity = 0.1;
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
        };

        // 键盘快捷键
        this.KeyPreview = true;
        this.KeyDown += CodeEditorForm_KeyDown;
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
                    this.Opacity = (this.Tag is double op) ? op : 0.1;
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
            Text = "  📝 player.js",
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
            Font = new Font("Consolas", 12),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            WordWrap = false,
            SelectionTabs = new int[] { 40 }
        };

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
        UpdateFileTree();
        RenderGame();
        PrintTerminal("Welcome to CodeSurvivor v1.0.0");
        PrintTerminal("Shortcuts: ↑↓←→ Move | Alt+↑↓ Opacity | Alt+Space BossKey | Alt+Q Home");
        PrintTerminal("Type 'help' for available commands");
        PrintTerminal("");
        UpdateStatusBar();
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

        var equipNode = root.Nodes.Add("📁 equipment");
        if (_world.Player.Weapon != null)
            equipNode.Nodes.Add($"⚔️ {_world.Player.Weapon.Name}");
        if (_world.Player.Armor != null)
            equipNode.Nodes.Add($"🛡️ {_world.Player.Armor.Name}");

        var itemNode = root.Nodes.Add($"📦 inventory ({_world.Player.Inventory.Count})");
        foreach (var item in _world.Player.Inventory.Take(5))
        {
            itemNode.Nodes.Add($"• {item.Name}");
        }

        root.ExpandAll();
    }

    private void ShowSkillInfo(Skill skill)
    {
        PrintTerminal($"");
        PrintTerminal($"// Skill: {skill.Name}");
        PrintTerminal($"// Type: {skill.Type}");
        PrintTerminal($"// MP Cost: {skill.MPCost}");
        PrintTerminal($"// Description: {skill.Description}");
        PrintTerminal($"");
    }

    private void RenderGame()
    {
        _codeEditor.Clear();

        var sb = new System.Text.StringBuilder();

        // 顶部状态注释
        sb.AppendLine("/**");
        sb.AppendLine($" * Floor: {_world.CurrentFloor} | Turn: {_turnCount}");
        sb.AppendLine($" * Position: ({_world.Player.X}, {_world.Player.Y})");
        sb.AppendLine($" * HP: {GetBar(_world.Player.HP, _world.Player.MaxHP)} {_world.Player.HP}/{_world.Player.MaxHP}");
        sb.AppendLine($" * MP: {GetBar(_world.Player.MP, _world.Player.MaxMP)} {_world.Player.MP}/{_world.Player.MaxMP}");
        sb.AppendLine($" * Level: {_world.Player.Level} [EXP: {_world.Player.EXP}/{_world.Player.EXPToNext}]");
        sb.AppendLine($" * ATK: {_world.Player.Attack} | DEF: {_world.Player.Defense} | 💰: {_world.Player.Gold}");
        sb.AppendLine(" */");
        sb.AppendLine();

        // 代码样式注释
        sb.AppendLine("const world = {");
        sb.AppendLine("    // Map representation");
        sb.AppendLine("    grid: [");

        // 渲染地图
        for (int y = 0; y < _world.Height; y++)
        {
            sb.Append("        \"");
            for (int x = 0; x < _world.Width; x++)
            {
                if (x == _world.Player.X && y == _world.Player.Y)
                {
                    sb.Append("👨‍💻"); // 玩家
                }
                else
                {
                    var enemy = _world.GetEnemyAt(x, y);
                    if (enemy != null && !enemy.IsDead)
                    {
                        sb.Append(enemy.DisplayChar);
                    }
                    else
                    {
                        switch (_world.Map[x, y])
                        {
                            case 0: sb.Append("  "); break; // 空地
                            case 1: sb.Append("██"); break; // 墙
                            case 2: sb.Append("🚪"); break; // 门
                            case 3: sb.Append("📦"); break; // 宝箱
                            default: sb.Append("  "); break;
                        }
                    }
                }
            }
            sb.AppendLine("\",");
        }

        sb.AppendLine("    ],");
        sb.AppendLine();

        // 敌人信息
        sb.AppendLine("    // Active enemies");
        sb.AppendLine("    enemies: [");
        foreach (var enemy in _world.Enemies.Where(e => !e.IsDead).Take(5))
        {
            double dist = Math.Sqrt(Math.Pow(enemy.X - _world.Player.X, 2) + Math.Pow(enemy.Y - _world.Player.Y, 2));
            sb.AppendLine($"        {{ name: \"{enemy.Name}\", hp: {enemy.HP}/{enemy.MaxHP}, dist: {dist:F1} }}, " +
                         $"// at ({enemy.X}, {enemy.Y})");
        }
        sb.AppendLine("    ],");
        sb.AppendLine();

        // 最近战斗日志
        sb.AppendLine("    // Recent combat log");
        sb.AppendLine("    log: [");
        foreach (var log in _combatLog.Take(5))
        {
            sb.AppendLine($"        \"{log}\",");
        }
        sb.AppendLine("    ]");
        sb.AppendLine("};");

        // 设置彩色文本
        _codeEditor.Text = sb.ToString();
        ApplySyntaxHighlighting();
    }

    private string GetBar(int current, int max)
    {
        int bars = (int)((double)current / max * 10);
        return new string('█', bars) + new string('░', 10 - bars);
    }

    private void ApplySyntaxHighlighting()
    {
        // 简单的语法高亮
        string[] keywords = { "const", "var", "let", "function", "class" };
        string[] comments = { "//", "/*", "*/" };

        // 这里简化处理，实际可以做得更复杂
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
            case "go":
            case "move":
                if (parts.Length > 1) MovePlayer(parts[1]);
                else PrintTerminal("Usage: go [up/down/left/right/north/south/east/west]");
                break;
            case "attack":
            case "atk":
                if (parts.Length > 1) AttackDirection(parts[1]);
                else AttackTarget();
                break;
            case "cast":
            case "use":
                if (parts.Length > 1) CastSkill(parts[1]);
                else PrintTerminal("Usage: cast [skillname]");
                break;
            case "status":
            case "st":
                ShowStatus();
                break;
            case "inventory":
            case "inv":
                ShowInventory();
                break;
            case "skills":
                ShowSkills();
                break;
            case "rest":
                Rest();
                break;
            case "floor":
                GoToNextFloor();
                break;
            case "map":
                ShowMiniMap();
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
                PrintTerminal("player.js  utils.js  enemy.js  items.json");
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

        // 检查游戏状态
        CheckGameState();

        // 更新界面
        RenderGame();
        UpdateFileTree();
        UpdateStatusBar();
    }

    private void ShowHelp()
    {
        PrintTerminal("");
        PrintTerminal("=== Available Commands ===");
        PrintTerminal("");
        PrintTerminal("MOVEMENT:");
        PrintTerminal("  go [dir]     - Move (up/down/left/right/n/s/e/w)");
        PrintTerminal("  rest         - Rest and recover HP/MP");
        PrintTerminal("");
        PrintTerminal("COMBAT:");
        PrintTerminal("  attack       - Attack adjacent enemy");
        PrintTerminal("  cast [skill] - Use skill (attack, heal, etc.)");
        PrintTerminal("");
        PrintTerminal("INFO:");
        PrintTerminal("  status       - Show player status");
        PrintTerminal("  inventory    - Show inventory");
        PrintTerminal("  skills       - Show learned skills");
        PrintTerminal("  map          - Show mini map");
        PrintTerminal("");
        PrintTerminal("MISC:");
        PrintTerminal("  floor        - Go to next floor (at 🚪)");
        PrintTerminal("  git [cmd]    - Pretend to use git");
        PrintTerminal("  npm [cmd]    - Pretend to use npm");
        PrintTerminal("  clear        - Clear terminal");
        PrintTerminal("  help         - Show this help");
        PrintTerminal("");
    }

    private void MovePlayer(string dir)
    {
        int dx = 0, dy = 0;
        switch (dir)
        {
            case "up":
            case "north":
            case "n": dy = -1; break;
            case "down":
            case "south":
            case "s": dy = 1; break;
            case "left":
            case "west":
            case "w": dx = -1; break;
            case "right":
            case "east":
            case "e": dx = 1; break;
        }

        int newX = _world.Player.X + dx;
        int newY = _world.Player.Y + dy;

        // 检查是否是门
        if (_world.Map[newX, newY] == 2)
        {
            PrintTerminal("You found the exit door! Type 'floor' to go to next floor.");
            return;
        }

        // 检查是否有敌人
        var enemy = _world.GetEnemyAt(newX, newY);
        if (enemy != null)
        {
            PrintTerminal($"⚔️ Blocked by {enemy.Name}! Use 'attack' to fight.");
            return;
        }

        // 检查是否是宝箱
        if (_world.Map[newX, newY] == 3)
        {
            OpenChest(newX, newY);
        }

        if (_world.CanMoveTo(newX, newY))
        {
            _world.Player.X = newX;
            _world.Player.Y = newY;
            _turnCount++;

            // 敌人回合
            _world.UpdateEnemies();

            // 检查是否被敌人攻击
            CheckEnemyAttacks();
        }
        else
        {
            PrintTerminal("💥 Can't move there! (Wall or boundary)");
        }
    }

    private void OpenChest(int x, int y)
    {
        _world.Map[x, y] = 0; // 移除宝箱
        var rand = new Random();
        int reward = rand.Next(3);

        switch (reward)
        {
            case 0:
                int gold = 20 + rand.Next(30);
                _world.Player.Gold += gold;
                PrintTerminal($"📦 You found {gold} gold!");
                break;
            case 1:
                int exp = 30 + rand.Next(20);
                _world.Player.GainEXP(exp);
                PrintTerminal($"📦 You found a skill book! +{exp} EXP");
                break;
            case 2:
                PrintTerminal("📦 You found a potion! (Auto-used)");
                _world.Player.Heal(30);
                break;
        }
    }

    private void AttackTarget()
    {
        // 寻找相邻的敌人
        var adjacent = _world.Enemies.Where(e => !e.IsDead && Math.Abs(e.X - _world.Player.X) <= 1 && Math.Abs(e.Y - _world.Player.Y) <= 1).FirstOrDefault();

        if (adjacent != null)
        {
            PerformAttack(adjacent);
        }
        else
        {
            PrintTerminal("❌ No enemy in range!");
        }
    }

    private void AttackDirection(string dir)
    {
        int dx = 0, dy = 0;
        switch (dir)
        {
            case "up":
            case "north":
            case "n": dy = -1; break;
            case "down":
            case "south":
            case "s": dy = 1; break;
            case "left":
            case "west":
            case "w": dx = -1; break;
            case "right":
            case "east":
            case "e": dx = 1; break;
        }

        int targetX = _world.Player.X + dx;
        int targetY = _world.Player.Y + dy;

        var enemy = _world.GetEnemyAt(targetX, targetY);
        if (enemy != null)
        {
            PerformAttack(enemy);
        }
        else
        {
            PrintTerminal("❌ No enemy in that direction!");
        }
    }

    private void PerformAttack(Enemy enemy)
    {
        int damage = _world.Player.Attack + new Random().Next(5);
        enemy.TakeDamage(damage);
        PrintTerminal($"⚔️ You hit {enemy.Name} for {damage} damage!");

        if (enemy.IsDead)
        {
            PrintTerminal($"💀 {enemy.Name} defeated! +{enemy.EXP} EXP, +{enemy.Gold} Gold");
            _world.Player.GainEXP(enemy.EXP);
            _world.Player.Gold += enemy.Gold;
            _combatLog.Add($"Killed {enemy.Name}");
        }

        // 敌人反击
        EnemyCounterAttack(enemy);

        // 敌人回合
        _world.UpdateEnemies();
        _turnCount++;
    }

    private void EnemyCounterAttack(Enemy enemy)
    {
        if (enemy.IsDead) return;

        int damage = enemy.Attack;
        _world.Player.TakeDamage(damage);
        PrintTerminal($"🩸 {enemy.Name} hits you for {damage} damage!");

        if (_world.Player.HP <= 0)
        {
            _gameOver = true;
        }
    }

    private void CheckEnemyAttacks()
    {
        var adjacent = _world.Enemies.Where(e => !e.IsDead && Math.Abs(e.X - _world.Player.X) <= 1 && Math.Abs(e.Y - _world.Player.Y) <= 1);
        foreach (var enemy in adjacent)
        {
            EnemyCounterAttack(enemy);
        }
    }

    private void CastSkill(string skillName)
    {
        var skill = _world.Player.Skills.FirstOrDefault(s => s.Id == skillName && s.IsUnlocked);
        if (skill == null)
        {
            PrintTerminal($"❌ Skill '{skillName}' not found!");
            return;
        }

        if (!_world.Player.CanCast(skill))
        {
            PrintTerminal($"❌ Not enough MP! (Need {skill.MPCost}, Have {_world.Player.MP})");
            return;
        }

        _world.Player.Cast(skill);

        switch (skill.Id)
        {
            case "attack":
                AttackTarget();
                break;
            case "heal":
                _world.Player.Heal(30 + _world.Player.Level * 5);
                PrintTerminal("💚 Heal cast! HP restored.");
                break;
            case "fireball":
                CastFireball();
                break;
            default:
                PrintTerminal($"✨ Cast {skill.Name}!");
                break;
        }

        _turnCount++;
        _world.UpdateEnemies();
    }

    private void CastFireball()
    {
        PrintTerminal("🔥 Fireball explodes!");
        // 伤害周围所有敌人
        var nearby = _world.Enemies.Where(e => !e.IsDead && Math.Sqrt(Math.Pow(e.X - _world.Player.X, 2) + Math.Pow(e.Y - _world.Player.Y, 2)) <= 2);
        foreach (var enemy in nearby)
        {
            int damage = (int)(_world.Player.Attack * 1.5);
            enemy.TakeDamage(damage);
            PrintTerminal($"  {enemy.Name} takes {damage} fire damage!");
        }
    }

    private void ShowStatus()
    {
        PrintTerminal("");
        PrintTerminal("=== Player Status ===");
        PrintTerminal($"Level: {_world.Player.Level}");
        PrintTerminal($"EXP: {_world.Player.EXP}/{_world.Player.EXPToNext}");
        PrintTerminal($"HP: {_world.Player.HP}/{_world.Player.MaxHP}");
        PrintTerminal($"MP: {_world.Player.MP}/{_world.Player.MaxMP}");
        PrintTerminal($"Attack: {_world.Player.Attack}");
        PrintTerminal($"Defense: {_world.Player.Defense}");
        PrintTerminal($"Gold: {_world.Player.Gold}");
        PrintTerminal($"Position: ({_world.Player.X}, {_world.Player.Y})");
        PrintTerminal($"Floor: {_world.CurrentFloor}");
        PrintTerminal("");
    }

    private void ShowInventory()
    {
        PrintTerminal("");
        PrintTerminal("=== Inventory ===");
        if (_world.Player.Inventory.Count == 0)
        {
            PrintTerminal("(Empty)");
        }
        else
        {
            foreach (var item in _world.Player.Inventory)
            {
                PrintTerminal($"  • {item.Name} ({item.Type})");
            }
        }
        PrintTerminal($"\nGold: {_world.Player.Gold}");
        PrintTerminal("");
    }

    private void ShowSkills()
    {
        PrintTerminal("");
        PrintTerminal("=== Skills ===");
        foreach (var skill in _world.Player.Skills)
        {
            PrintTerminal($"  • {skill.Id} - {skill.Name} (MP: {skill.MPCost})");
            PrintTerminal($"    {skill.Description}");
        }
        PrintTerminal("");
    }

    private void Rest()
    {
        // 检查周围是否有敌人
        var nearby = _world.Enemies.Where(e => !e.IsDead && Math.Sqrt(Math.Pow(e.X - _world.Player.X, 2) + Math.Pow(e.Y - _world.Player.Y, 2)) <= 5);
        if (nearby.Any())
        {
            PrintTerminal("❌ Can't rest! Enemies are nearby.");
            return;
        }

        _world.Player.HP = Math.Min(_world.Player.MaxHP, _world.Player.HP + 20);
        _world.Player.MP = _world.Player.MaxMP;
        PrintTerminal("💤 You rest for a while. HP and MP restored.");
        _turnCount += 5;
        _world.UpdateEnemies();
    }

    private void GoToNextFloor()
    {
        if (_world.Map[_world.Player.X, _world.Player.Y] == 2)
        {
            PrintTerminal($"🚪 Descending to floor {_world.CurrentFloor + 1}...");
            _world.NextFloor();
            PrintTerminal("✨ New area discovered!");
        }
        else
        {
            PrintTerminal("❌ You need to find the door (🚪) first!");
        }
    }

    private void ShowMiniMap()
    {
        PrintTerminal("");
        PrintTerminal("=== Map ===");
        for (int y = 0; y < _world.Height; y++)
        {
            var line = "";
            for (int x = 0; x < _world.Width; x++)
            {
                if (x == _world.Player.X && y == _world.Player.Y)
                    line += "@";
                else
                {
                    var enemy = _world.GetEnemyAt(x, y);
                    if (enemy != null)
                        line += "E";
                    else
                    {
                        switch (_world.Map[x, y])
                        {
                            case 0: line += "."; break;
                            case 1: line += "#"; break;
                            case 2: line += "D"; break;
                            case 3: line += "C"; break;
                            default: line += "?"; break;
                        }
                    }
                }
            }
            PrintTerminal(line);
        }
        PrintTerminal("");
        PrintTerminal("@ = You, E = Enemy, # = Wall, D = Door, C = Chest, . = Empty");
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
                PrintTerminal("On floor " + _world.CurrentFloor);
                PrintTerminal($"Changes not staged: {_world.Enemies.Count(e => !e.IsDead)} enemies remaining");
                break;
            case "commit":
                PrintTerminal($"[{_world.CurrentFloor}] feat: cleared floor {_world.CurrentFloor - 1}");
                PrintTerminal(" 1 file changed, 0 insertions(+), " + _world.Enemies.Count(e => e.IsDead) + " deletions(-)");
                break;
            case "push":
                PrintTerminal("Enumerating objects... Done.");
                PrintTerminal("Writing objects... Done.");
                PrintTerminal("remote: This is a game, not a real git repo! 😄");
                break;
            case "log":
                PrintTerminal($"commit {Guid.NewGuid().ToString()[..7]}");
                PrintTerminal($"Author: Player <player@codesurvivor.game>");
                PrintTerminal($"Date: {DateTime.Now}");
                PrintTerminal("");
                PrintTerminal("    Defeated enemies on floor " + _world.CurrentFloor);
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
                PrintTerminal("added 1 package: vitality-potion");
                _world.Player.Inventory.Add(new Item("potion", "Vitality Potion", "consumable", "common", "Restores 50 HP"));
                break;
            case "run":
                PrintTerminal("> codesurvivor@1.0.0 start");
                PrintTerminal("> The game is already running!");
                break;
            case "build":
                PrintTerminal("> Building project...");
                PrintTerminal("> ✓ Build complete!");
                PrintTerminal("> 💪 Attack temporarily boosted!");
                _world.Player.Attack += 5;
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
                PrintTerminal($"    this.level = {_world.Player.Level};");
                PrintTerminal("  }");
                PrintTerminal("}");
                break;
            case "enemy.js":
                PrintTerminal("class Enemy {");
                PrintTerminal("  // TODO: Implement AI");
                PrintTerminal("}");
                break;
            default:
                PrintTerminal($"cat: {filename}: No such file or directory");
                break;
        }
    }

    private void CheckGameState()
    {
        if (_gameOver)
        {
            PrintTerminal("");
            PrintTerminal("╔═══════════════════════════════════════╗");
            PrintTerminal("║            GAME OVER                  ║");
            PrintTerminal($"║    You survived {_world.CurrentFloor} floors!            ║");
            PrintTerminal("╚═══════════════════════════════════════╝");
            PrintTerminal("");
            PrintTerminal("Type 'restart' to play again (or close window)");
        }
    }

    private void PrintTerminal(string text)
    {
        _terminalOutput.AppendText(text + Environment.NewLine);
        _terminalOutput.ScrollToCaret();
    }

    private void UpdateStatusBar()
    {
        string status = $"  Ln {_world.Player.Y}, Col {_world.Player.X}  |  UTF-8  |  JavaScript  |  🎮 Floor: {_world.CurrentFloor}  |  ❤️ {_world.Player.HP}/{_world.Player.MaxHP}  |  ✨ {_world.Player.MP}/{_world.Player.MaxMP}  |  🏆 Lv.{_world.Player.Level}";
        _statusBar.Text = status;
    }

    private void CodeEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // 方向键移动（不加 Alt，因为 Alt+方向键是透明度控制）
        switch (e.KeyCode)
        {
            case Keys.Up:
                MovePlayer("up");
                RenderGame();
                UpdateStatusBar();
                e.Handled = true;
                break;
            case Keys.Down:
                MovePlayer("down");
                RenderGame();
                UpdateStatusBar();
                e.Handled = true;
                break;
            case Keys.Left:
                MovePlayer("left");
                RenderGame();
                UpdateStatusBar();
                e.Handled = true;
                break;
            case Keys.Right:
                MovePlayer("right");
                RenderGame();
                UpdateStatusBar();
                e.Handled = true;
                break;
        }
    }
    }

    private void ReturnToHome()
    {
        if (Core.MoyuLauncher.Instance != null)
        {
            Core.MoyuLauncher.Instance.Show();
            this.Hide();
        }
    }
}
