using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PureBattleGame.Games.CodeSurvivor;

/// <summary>
/// 玩家实体 - 在代码世界中冒险的程序员
/// </summary>
public class Player
{
    public int X { get; set; } = 5;
    public int Y { get; set; } = 3;
    public int MaxHP { get; set; } = 100;
    public int HP { get; set; } = 100;
    public int MaxMP { get; set; } = 50;
    public int MP { get; set; } = 50;
    public int Level { get; set; } = 1;
    public int EXP { get; set; } = 0;
    public int EXPToNext { get; set; } = 100;
    public int Attack { get; set; } = 15;
    public int Defense { get; set; } = 5;
    public int Gold { get; set; } = 0;

    // 已解锁的技能（伪装成已解锁的文件）
    public List<Skill> Skills { get; set; } = new();

    // 装备
    public Item? Weapon { get; set; }
    public Item? Armor { get; set; }
    public Item? Accessory { get; set; }

    // 背包
    public List<Item> Inventory { get; set; } = new();

    public Player()
    {
        // 初始技能
        Skills.Add(new Skill("attack", "攻击", "basic", 0, "普通攻击目标"));
        Skills.Add(new Skill("heal", "恢复", "magic", 10, "恢复 30 HP"));
    }

    public void TakeDamage(int damage)
    {
        int actual = Math.Max(1, damage - Defense);
        HP = Math.Max(0, HP - actual);
    }

    public void Heal(int amount)
    {
        HP = Math.Min(MaxHP, HP + amount);
    }

    public void GainEXP(int amount)
    {
        EXP += amount;
        while (EXP >= EXPToNext)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        EXP -= EXPToNext;
        Level++;
        EXPToNext = (int)(EXPToNext * 1.5);
        MaxHP += 20;
        HP = MaxHP;
        MaxMP += 10;
        MP = MaxMP;
        Attack += 5;
        Defense += 2;
    }

    public bool CanCast(Skill skill) => MP >= skill.MPCost;

    public void Cast(Skill skill)
    {
        MP -= skill.MPCost;
    }

    public void RegenMP()
    {
        MP = Math.Min(MaxMP, MP + 2);
    }
}

/// <summary>
/// 技能 - 伪装成代码文件
/// </summary>
public class Skill
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // basic, magic, ultimate
    public int MPCost { get; set; }
    public string Description { get; set; }
    public int Power { get; set; }
    public bool IsUnlocked { get; set; } = true;
    public int Level { get; set; } = 1;

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
/// 装备/道具
/// </summary>
public class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // weapon, armor, accessory, consumable
    public string Rarity { get; set; } // common, rare, epic, legendary
    public string Description { get; set; }
    public int AttackBonus { get; set; }
    public int DefenseBonus { get; set; }
    public int HPBonus { get; set; }
    public int MPBonus { get; set; }

    public Item(string id, string name, string type, string rarity, string desc)
    {
        Id = id;
        Name = name;
        Type = type;
        Rarity = rarity;
        Description = desc;
    }
}

/// <summary>
/// 敌人
/// </summary>
public class Enemy
{
    public int X { get; set; }
    public int Y { get; set; }
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

    // AI行为
    public int AggroRange { get; set; } = 5;
    public bool IsAggro { get; set; } = false;

    public Enemy(int x, int y, string type, int level)
    {
        X = x;
        Y = y;
        Type = type;
        Level = level;
        InitializeStats();
    }

    public int Level { get; set; }

    private void InitializeStats()
    {
        var multi = 1 + (Level - 1) * 0.2;
        switch (Type)
        {
            case "bug":
                Name = "Bug";
                DisplayChar = "🐛";
                MaxHP = (int)(30 * multi);
                Attack = (int)(8 * multi);
                Defense = (int)(2 * multi);
                EXP = 15;
                Gold = 5;
                break;
            case "slime":
                Name = "Slime";
                DisplayChar = "💧";
                MaxHP = (int)(50 * multi);
                Attack = (int)(10 * multi);
                Defense = (int)(3 * multi);
                EXP = 25;
                Gold = 10;
                break;
            case "goblin":
                Name = "Goblin";
                DisplayChar = "👺";
                MaxHP = (int)(70 * multi);
                Attack = (int)(15 * multi);
                Defense = (int)(5 * multi);
                EXP = 40;
                Gold = 20;
                break;
            case "skeleton":
                Name = "Skeleton";
                DisplayChar = "💀";
                MaxHP = (int)(60 * multi);
                Attack = (int)(18 * multi);
                Defense = (int)(4 * multi);
                EXP = 35;
                Gold = 15;
                break;
            case "dragon":
                Name = "Dragon";
                DisplayChar = "🐉";
                MaxHP = (int)(200 * multi);
                Attack = (int)(30 * multi);
                Defense = (int)(10 * multi);
                EXP = 200;
                Gold = 100;
                break;
            default:
                Name = "Unknown";
                DisplayChar = "❓";
                MaxHP = 10;
                Attack = 5;
                Defense = 0;
                EXP = 1;
                Gold = 1;
                break;
        }
        HP = MaxHP;
    }

    public void TakeDamage(int damage)
    {
        int actual = Math.Max(1, damage - Defense);
        HP = Math.Max(0, HP - actual);
    }

    public void UpdateAI(Player player, int worldWidth, int worldHeight)
    {
        double dist = Math.Sqrt(Math.Pow(X - player.X, 2) + Math.Pow(Y - player.Y, 2));

        if (dist <= AggroRange)
        {
            IsAggro = true;
        }

        if (!IsAggro) return;

        // 向玩家移动
        int dx = 0, dy = 0;
        if (X < player.X) dx = 1;
        else if (X > player.X) dx = -1;
        if (Y < player.Y) dy = 1;
        else if (Y > player.Y) dy = -1;

        // 随机选择方向（避免斜向移动问题）
        if (dx != 0 && dy != 0)
        {
            if (new Random().Next(2) == 0) dy = 0;
            else dx = 0;
        }

        int newX = X + dx;
        int newY = Y + dy;

        if (newX >= 0 && newX < worldWidth && newY >= 0 && newY < worldHeight)
        {
            X = newX;
            Y = newY;
        }
    }
}

/// <summary>
/// 游戏世界
/// </summary>
public class GameWorld
{
    public int Width { get; set; } = 40;
    public int Height { get; set; } = 20;
    public int CurrentFloor { get; set; } = 1;
    public Player Player { get; set; } = new();
    public List<Enemy> Enemies { get; set; } = new();
    public List<Item> ItemsOnGround { get; set; } = new();
    public Random Rand { get; set; } = new();

    // 地形 0=空地 1=墙 2=门 3=宝箱
    public int[,] Map { get; set; } = null!;

    public GameWorld()
    {
        GenerateFloor();
    }

    public void GenerateFloor()
    {
        Map = new int[Width, Height];

        // 生成边界墙
        for (int x = 0; x < Width; x++)
        {
            Map[x, 0] = 1;
            Map[x, Height - 1] = 1;
        }
        for (int y = 0; y < Height; y++)
        {
            Map[0, y] = 1;
            Map[Width - 1, y] = 1;
        }

        // 随机生成内部墙
        int wallCount = 20 + CurrentFloor * 3;
        for (int i = 0; i < wallCount; i++)
        {
            int wx = Rand.Next(2, Width - 2);
            int wy = Rand.Next(2, Height - 2);
            if ((wx != Player.X || wy != Player.Y) && Map[wx, wy] == 0)
            {
                Map[wx, wy] = 1;
            }
        }

        // 生成下一层门
        int doorX, doorY;
        do
        {
            doorX = Rand.Next(2, Width - 2);
            doorY = Rand.Next(2, Height - 2);
        } while (Map[doorX, doorY] != 0 || (doorX == Player.X && doorY == Player.Y));
        Map[doorX, doorY] = 2;

        // 生成宝箱
        int chestCount = 3 + Rand.Next(3);
        for (int i = 0; i < chestCount; i++)
        {
            int cx, cy;
            do
            {
                cx = Rand.Next(2, Width - 2);
                cy = Rand.Next(2, Height - 2);
            } while (Map[cx, cy] != 0);
            Map[cx, cy] = 3;
        }

        // 生成敌人
        Enemies.Clear();
        int enemyCount = 5 + CurrentFloor * 2 + Rand.Next(3);
        string[] types = CurrentFloor < 3 ? new[] { "bug", "slime" } :
                        CurrentFloor < 5 ? new[] { "bug", "slime", "goblin" } :
                        new[] { "slime", "goblin", "skeleton" };

        for (int i = 0; i < enemyCount; i++)
        {
            int ex, ey;
            do
            {
                ex = Rand.Next(2, Width - 2);
                ey = Rand.Next(2, Height - 2);
            } while (Map[ex, ey] != 0 || (ex == Player.X && ey == Player.Y) || Enemies.Any(e => e.X == ex && e.Y == ey));

            string type = types[Rand.Next(types.Length)];
            Enemies.Add(new Enemy(ex, ey, type, CurrentFloor));
        }

        // 重置玩家位置到入口
        Player.X = 2;
        Player.Y = 2;
    }

    public bool CanMoveTo(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        if (Map[x, y] == 1) return false; // 墙
        if (Enemies.Any(e => e.X == x && e.Y == y && !e.IsDead)) return false;
        return true;
    }

    public Enemy? GetEnemyAt(int x, int y)
    {
        return Enemies.FirstOrDefault(e => e.X == x && e.Y == y && !e.IsDead);
    }

    public void NextFloor()
    {
        CurrentFloor++;
        GenerateFloor();
    }

    public void UpdateEnemies()
    {
        foreach (var enemy in Enemies.Where(e => !e.IsDead))
        {
            enemy.UpdateAI(Player, Width, Height);
        }
    }
}
