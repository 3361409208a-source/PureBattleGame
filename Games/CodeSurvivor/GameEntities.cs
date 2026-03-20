using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PureBattleGame.Games.CodeSurvivor;

/// <summary>
/// 平台类型
/// </summary>
public enum PlatformType
{
    Static,     // 静止平台
    Moving,     // 移动平台
    Breaking,   // 破碎平台（踩上去后消失）
    Spring      // 弹簧平台（弹跳）
}

/// <summary>
/// 平台 - 横版过关中的平台
/// </summary>
public class Platform
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; } = 20;
    public PlatformType Type { get; set; } = PlatformType.Static;

    // 移动平台参数
    public float StartX { get; set; }
    public float EndX { get; set; }
    public float MoveSpeed { get; set; } = 1.5f;
    public bool MovingRight { get; set; } = true;

    // 破碎平台参数
    public bool IsBreaking { get; set; } = false;
    public int BreakTimer { get; set; } = 0;

    public Platform(float x, float y, float width, PlatformType type = PlatformType.Static)
    {
        X = x;
        Y = y;
        Width = width;
        Type = type;
        StartX = x;
    }

    public void Update()
    {
        if (Type == PlatformType.Moving)
        {
            if (MovingRight)
            {
                X += MoveSpeed;
                if (X >= EndX) MovingRight = false;
            }
            else
            {
                X -= MoveSpeed;
                if (X <= StartX) MovingRight = true;
            }
        }

        if (IsBreaking)
        {
            BreakTimer--;
        }
    }

    public RectangleF GetBounds()
    {
        return new RectangleF(X, Y, Width, Height);
    }
}

/// <summary>
/// 收集品类型
/// </summary>
public enum CollectibleType
{
    Coin,       // 金币
    PowerUp,    // 能力提升
    Flag        // 关卡终点旗帜
}

/// <summary>
/// 收集品 - 金币、道具、旗帜
/// </summary>
public class Collectible
{
    public float X { get; set; }
    public float Y { get; set; }
    public CollectibleType Type { get; set; }
    public string DisplayChar { get; set; } = "🪙";
    public bool IsCollected { get; set; } = false;
    public int Width { get; set; } = 24;
    public int Height { get; set; } = 24;
    public int Value { get; set; } = 10; // 金币价值或分数

    public Collectible(float x, float y, CollectibleType type)
    {
        X = x;
        Y = y;
        Type = type;
        DisplayChar = type switch
        {
            CollectibleType.Coin => "🪙",
            CollectibleType.PowerUp => "⚡",
            CollectibleType.Flag => "🚩",
            _ => "?"
        };
        Value = type switch
        {
            CollectibleType.Coin => 10,
            CollectibleType.PowerUp => 50,
            CollectibleType.Flag => 100,
            _ => 0
        };
    }

    public RectangleF GetBounds()
    {
        return new RectangleF(X, Y, Width, Height);
    }
}

/// <summary>
/// 玩家实体 - 在代码世界中冒险的程序员
/// </summary>
public class Player
{
    // 浮点坐标位置
    public float X { get; set; } = 100;
    public float Y { get; set; } = 300;

    // 物理属性
    public float VelX { get; set; } = 0;
    public float VelY { get; set; } = 0;
    public float Speed { get; set; } = 5;
    public float JumpPower { get; set; } = 13;
    public float Gravity { get; set; } = 0.6f;
    public bool IsGrounded { get; set; } = false;
    public bool FacingRight { get; set; } = true;

    // 尺寸（用于碰撞检测）
    public int Width { get; set; } = 28;
    public int Height { get; set; } = 32;

    // 游戏属性
    public int MaxHP { get; set; } = 100;
    public int HP { get; set; } = 100;
    public int Level { get; set; } = 1;
    public int Score { get; set; } = 0;
    public int Coins { get; set; } = 0;

    // 无敌时间（受伤后）
    public int InvincibleTime { get; set; } = 0;

    // 已解锁的技能（伪装成已解锁的文件）
    public List<Skill> Skills { get; set; } = new();

    public Player()
    {
        Skills.Add(new Skill("jump", "跳跃", "basic", 0, "按空格键跳跃"));
        Skills.Add(new Skill("move", "移动", "basic", 0, "方向键移动"));
    }

    public void Update()
    {
        // 应用重力
        VelY += Gravity;

        // 应用速度
        X += VelX;
        Y += VelY;

        // 减少无敌时间
        if (InvincibleTime > 0)
            InvincibleTime--;

        // 更新朝向
        if (VelX > 0) FacingRight = true;
        else if (VelX < 0) FacingRight = false;
    }

    public void Jump()
    {
        if (IsGrounded)
        {
            VelY = -JumpPower;
            IsGrounded = false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (InvincibleTime > 0) return;

        HP = Math.Max(0, HP - damage);
        InvincibleTime = 60; // 1秒无敌（60帧）
    }

    public void Heal(int amount)
    {
        HP = Math.Min(MaxHP, HP + amount);
    }

    public RectangleF GetBounds()
    {
        return new RectangleF(X, Y, Width, Height);
    }

    public void CollectCoin(int value)
    {
        Coins += value;
        Score += value;
    }
}

/// <summary>
/// 技能 - 伪装成代码文件
/// </summary>
public class Skill
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int MPCost { get; set; }
    public string Description { get; set; }
    public int Power { get; set; }
    public bool IsUnlocked { get; set; } = true;

    public Skill(string id, string name, string type, int mpCost, string desc, int power = 0)
    {
        Id = id;
        Name = name;
        Type = type;
        MPCost = mpCost;
        Description = desc;
        Power = power;
    }
}

/// <summary>
/// 敌人 - 横版过关中的巡逻敌人
/// </summary>
public class Enemy
{
    public float X { get; set; }
    public float Y { get; set; }
    public float VelX { get; set; } = 1.5f;
    public float PatrolStart { get; set; }
    public float PatrolEnd { get; set; }
    public bool MovingRight { get; set; } = true;

    public string Type { get; set; }
    public string Name { get; set; } = "Unknown";
    public string DisplayChar { get; set; } = "?";
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int EXP { get; set; }
    public int Gold { get; set; }
    public bool IsDead => HP <= 0;

    // 尺寸
    public int Width { get; set; } = 28;
    public int Height { get; set; } = 28;

    public Enemy(float x, float y, string type, int level)
    {
        X = x;
        Y = y;
        Type = type;
        PatrolStart = x - 60;
        PatrolEnd = x + 60;
        InitializeStats(level);
    }

    private void InitializeStats(int level)
    {
        var multi = 1 + (level - 1) * 0.15;
        switch (Type)
        {
            case "bug":
                Name = "Bug";
                DisplayChar = "🐛";
                MaxHP = (int)(25 * multi);
                Attack = (int)(8 * multi);
                Defense = (int)(1 * multi);
                EXP = 15;
                Gold = 5;
                VelX = 1.0f;
                break;
            case "slime":
                Name = "Slime";
                DisplayChar = "💧";
                MaxHP = (int)(40 * multi);
                Attack = (int)(10 * multi);
                Defense = (int)(2 * multi);
                EXP = 20;
                Gold = 8;
                VelX = 0.8f;
                break;
            case "goblin":
                Name = "Goblin";
                DisplayChar = "👺";
                MaxHP = (int)(50 * multi);
                Attack = (int)(12 * multi);
                Defense = (int)(3 * multi);
                EXP = 30;
                Gold = 15;
                VelX = 1.5f;
                break;
            case "skeleton":
                Name = "Skeleton";
                DisplayChar = "💀";
                MaxHP = (int)(35 * multi);
                Attack = (int)(15 * multi);
                Defense = (int)(1 * multi);
                EXP = 25;
                Gold = 12;
                VelX = 1.8f;
                break;
            case "dragon":
                Name = "Dragon";
                DisplayChar = "🐉";
                MaxHP = (int)(150 * multi);
                Attack = (int)(25 * multi);
                Defense = (int)(8 * multi);
                EXP = 150;
                Gold = 80;
                VelX = 1.2f;
                break;
            default:
                Name = "Unknown";
                DisplayChar = "❓";
                MaxHP = 10;
                Attack = 5;
                Defense = 0;
                EXP = 1;
                Gold = 1;
                VelX = 1.0f;
                break;
        }
        HP = MaxHP;
    }

    public void Update()
    {
        if (IsDead) return;

        // 巡逻移动
        if (MovingRight)
        {
            X += VelX;
            if (X >= PatrolEnd)
            {
                MovingRight = false;
            }
        }
        else
        {
            X -= VelX;
            if (X <= PatrolStart)
            {
                MovingRight = true;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        int actual = Math.Max(1, damage - Defense);
        HP = Math.Max(0, HP - actual);
    }

    public RectangleF GetBounds()
    {
        return new RectangleF(X, Y, Width, Height);
    }
}

/// <summary>
/// 游戏世界 - 横版过关关卡
/// </summary>
public class GameWorld
{
    public int LevelWidth { get; set; } = 2000;  // 关卡总宽度
    public int LevelHeight { get; set; } = 600;  // 关卡高度
    public int CurrentLevel { get; set; } = 1;
    public float CameraX { get; set; } = 0;

    public Player Player { get; set; } = new();
    public List<Platform> Platforms { get; set; } = new();
    public List<Enemy> Enemies { get; set; } = new();
    public List<Collectible> Collectibles { get; set; } = new();

    public Random Rand { get; set; } = new();

    // 屏幕尺寸（用于相机跟随）
    public int ScreenWidth { get; set; } = 800;
    public int ScreenHeight { get; set; } = 500;

    // 地面Y坐标
    public float GroundY => LevelHeight - 60;

    public GameWorld()
    {
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        Platforms.Clear();
        Enemies.Clear();
        Collectibles.Clear();

        // 重置玩家位置
        Player.X = 50;
        Player.Y = GroundY - 100;
        Player.VelX = 0;
        Player.VelY = 0;

        // 生成地面（分段，便于处理）
        float groundX = 0;
        while (groundX < LevelWidth)
        {
            float segmentWidth = Math.Min(400, LevelWidth - groundX);
            Platforms.Add(new Platform(groundX, GroundY, segmentWidth));
            groundX += segmentWidth;
        }

        // 根据关卡生成平台
        GeneratePlatforms();

        // 生成敌人
        GenerateEnemies();

        // 生成收集品
        GenerateCollectibles();

        // 添加终点旗帜
        Collectibles.Add(new Collectible(LevelWidth - 100, GroundY - 60, CollectibleType.Flag));
    }

    private void GeneratePlatforms()
    {
        // 第一关：简单平台
        float[] platformPositions = CurrentLevel switch
        {
            1 => new float[] { 200, 400, 600, 900, 1200, 1500 },
            2 => new float[] { 180, 350, 520, 750, 950, 1150, 1400, 1700 },
            3 => new float[] { 150, 300, 500, 700, 900, 1100, 1300, 1500, 1700 },
            _ => new float[] { 200, 450, 700, 1000, 1300, 1600 }
        };

        foreach (float px in platformPositions)
        {
            float y = GroundY - 80 - Rand.Next(60);
            float width = 80 + Rand.Next(60);

            // 有些平台是移动的
            PlatformType type = Rand.Next(5) == 0 ? PlatformType.Moving : PlatformType.Static;
            var platform = new Platform(px, y, width, type);

            if (type == PlatformType.Moving)
            {
                platform.EndX = px + 80;
                platform.MoveSpeed = 1.0f + Rand.Next(2);
            }

            Platforms.Add(platform);
        }

        // 高处平台（需要跳跃到达）
        for (int i = 0; i < 3 + CurrentLevel; i++)
        {
            float px = 400 + i * 400 + Rand.Next(100);
            if (px < LevelWidth - 200)
            {
                float y = GroundY - 150 - Rand.Next(50);
                Platforms.Add(new Platform(px, y, 60 + Rand.Next(40)));
            }
        }
    }

    private void GenerateEnemies()
    {
        string[] types = CurrentLevel switch
        {
            1 => new[] { "bug", "slime" },
            2 => new[] { "bug", "slime", "goblin" },
            3 => new[] { "slime", "goblin", "skeleton" },
            _ => new[] { "goblin", "skeleton" }
        };

        int enemyCount = 5 + CurrentLevel * 2;

        for (int i = 0; i < enemyCount; i++)
        {
            float ex = 300 + i * (LevelWidth - 400) / enemyCount + Rand.Next(50);
            float ey = GroundY - 30;

            // 有些敌人在平台上
            var platform = Platforms[Rand.Next(Platforms.Count)];
            if (Rand.Next(3) == 0)
            {
                ex = platform.X + platform.Width / 2;
                ey = platform.Y - 30;
            }

            string type = types[Rand.Next(types.Length)];
            var enemy = new Enemy(ex, ey, type, CurrentLevel);
            Enemies.Add(enemy);
        }
    }

    private void GenerateCollectibles()
    {
        // 在平台上放置金币
        foreach (var platform in Platforms)
        {
            if (Rand.Next(3) == 0)
            {
                Collectibles.Add(new Collectible(
                    platform.X + platform.Width / 2 - 10,
                    platform.Y - 30,
                    CollectibleType.Coin));
            }
        }

        // 随机散布一些金币
        for (int i = 0; i < 10 + CurrentLevel * 3; i++)
        {
            float cx = 200 + Rand.Next(LevelWidth - 300);
            float cy = GroundY - 80 - Rand.Next(150);
            Collectibles.Add(new Collectible(cx, cy, CollectibleType.Coin));
        }

        // 添加能力提升道具
        int powerUpCount = 2 + CurrentLevel;
        for (int i = 0; i < powerUpCount; i++)
        {
            float px = 500 + i * (LevelWidth / powerUpCount);
            float py = GroundY - 120 - Rand.Next(100);
            Collectibles.Add(new Collectible(px, py, CollectibleType.PowerUp));
        }
    }

    public void UpdatePhysics()
    {
        // 更新玩家
        Player.Update();

        // 限制玩家不超出关卡边界
        if (Player.X < 0) { Player.X = 0; Player.VelX = 0; }
        if (Player.X > LevelWidth - Player.Width) { Player.X = LevelWidth - Player.Width; Player.VelX = 0; }
        if (Player.Y > LevelHeight) // 掉落深渊
        {
            Player.HP = 0; // 死亡
        }

        // 更新平台
        foreach (var platform in Platforms)
        {
            platform.Update();
        }

        // 平台碰撞检测
        Player.IsGrounded = false;
        foreach (var platform in Platforms)
        {
            if (CheckCollision(Player.GetBounds(), platform.GetBounds()))
            {
                // 从上方落到平台上
                if (Player.VelY > 0 && Player.Y + Player.Height - Player.VelY <= platform.Y + 5)
                {
                    Player.Y = platform.Y - Player.Height;
                    Player.VelY = 0;
                    Player.IsGrounded = true;

                    // 跟随移动平台
                    if (platform.Type == PlatformType.Moving)
                    {
                        Player.X += platform.MovingRight ? platform.MoveSpeed : -platform.MoveSpeed;
                    }

                    // 弹簧平台
                    if (platform.Type == PlatformType.Spring)
                    {
                        Player.VelY = -Player.JumpPower * 1.5f;
                        Player.IsGrounded = false;
                    }

                    // 破碎平台
                    if (platform.Type == PlatformType.Breaking && !platform.IsBreaking)
                    {
                        platform.IsBreaking = true;
                        platform.BreakTimer = 60; // 1秒后消失
                    }
                }
            }
        }

        // 移除已破碎的平台
        Platforms.RemoveAll(p => p.IsBreaking && p.BreakTimer <= 0);

        // 更新敌人
        foreach (var enemy in Enemies)
        {
            enemy.Update();

            // 敌人与玩家碰撞
            if (!enemy.IsDead && CheckCollision(Player.GetBounds(), enemy.GetBounds()))
            {
                // 踩到敌人头上（消灭敌人）
                if (Player.VelY > 0 && Player.Y + Player.Height - Player.VelY <= enemy.Y + 10)
                {
                    enemy.TakeDamage(100); // 一击必杀
                    Player.VelY = -Player.JumpPower * 0.7f; // 小跳跃
                    Player.Score += enemy.EXP;
                    Player.Coins += enemy.Gold;
                }
                else
                {
                    // 被敌人碰到
                    Player.TakeDamage(enemy.Attack);
                    // 击退
                    Player.VelX = Player.X < enemy.X ? -5 : 5;
                    Player.VelY = -5;
                }
            }
        }

        // 清理死亡的敌人
        Enemies.RemoveAll(e => e.IsDead);

        // 更新收集品
        foreach (var collectible in Collectibles)
        {
            if (!collectible.IsCollected && CheckCollision(Player.GetBounds(), collectible.GetBounds()))
            {
                collectible.IsCollected = true;

                switch (collectible.Type)
                {
                    case CollectibleType.Coin:
                        Player.CollectCoin(collectible.Value);
                        break;
                    case CollectibleType.PowerUp:
                        Player.Heal(20);
                        Player.Score += 50;
                        break;
                    case CollectibleType.Flag:
                        // 到达终点，由外部处理
                        break;
                }
            }
        }

        // 清理已收集的物品
        Collectibles.RemoveAll(c => c.IsCollected && c.Type != CollectibleType.Flag);

        // 更新相机位置（跟随玩家）
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        // 相机跟随玩家，但保持在关卡边界内
        float targetCameraX = Player.X - ScreenWidth / 2 + Player.Width / 2;

        // 平滑跟随
        CameraX += (targetCameraX - CameraX) * 0.1f;

        // 限制相机范围
        if (CameraX < 0) CameraX = 0;
        if (CameraX > LevelWidth - ScreenWidth) CameraX = LevelWidth - ScreenWidth;
    }

    public bool CheckCollision(RectangleF a, RectangleF b)
    {
        return a.X < b.X + b.Width &&
               a.X + a.Width > b.X &&
               a.Y < b.Y + b.Height &&
               a.Y + a.Height > b.Y;
    }

    public bool IsOnScreen(float x, float width)
    {
        return x + width >= CameraX && x <= CameraX + ScreenWidth;
    }

    public void NextLevel()
    {
        CurrentLevel++;
        LevelWidth = 2000 + CurrentLevel * 200;
        GenerateLevel();
    }

    public void RestartLevel()
    {
        Player.HP = Player.MaxHP;
        Player.X = 50;
        Player.Y = GroundY - 100;
        Player.VelX = 0;
        Player.VelY = 0;
        Player.Coins = Math.Max(0, Player.Coins - 50); // 惩罚
    }

    public bool IsLevelComplete()
    {
        return Collectibles.Any(c => c.Type == CollectibleType.Flag && c.IsCollected);
    }
}
