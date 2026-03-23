using System;
using System.Collections.Generic;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

/// <summary>
/// 投射物 - 子弹、火箭、等离子等
/// </summary>
public class Projectile
{
    public float X { get; set; }
    public float Y { get; set; }
    public float OriginX { get; set; } // 新增：记录发射原点
    public float OriginY { get; set; } // 新增：记录发射原点
    public float Dx { get; set; }
    public float Dy { get; set; }
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public Robot? Owner { get; set; }
    public Robot? TrackingTarget { get; set; } // 锁定机器人目标
    public Monster? TrackingMonster { get; set; } // 锁定怪物目标 (新增)
    public Color ProjectileColor { get; set; }
    public bool IsActive { get; set; } = true;
    public string Type { get; set; } = "BULLET"; // BULLET, ROCKET, PLASMA, CANNON, LIGHTNING, SPIT, INK
    public int LifeTime { get; set; }
    public int Damage { get; set; } // 新增伤害属性
    public List<PointF> Trail { get; set; } = new List<PointF>();

    public bool IsCluster { get; set; } = false;
    public float ExplosionRadius { get; set; } = 0;
    public float Size { get; set; } = 0; // 0 means default size based on type
    public bool IsMonsterProjectile { get; set; } = false; // 新增：是否由怪物发射
    public int ChainCount { get; set; } = 0; // 连锁次数
    public int PenetrationCount { get; set; } = 0; // 剩余穿透次数
    public HashSet<int> HitEntityIds { get; } = new HashSet<int>(); // 已命中的实体ID，防止重复命中,防止无限套娃

    // 无参构造函数（用于对象池）
    public Projectile()
    {
    }

    public Projectile(Robot? owner, float x, float y, float tx, float ty, string type = "BULLET", Robot? target = null)
    {
        Owner = owner;
        X = x;
        Y = y;
        OriginX = x; 
        OriginY = y;
        TargetX = tx;
        TargetY = ty;
        Type = type;
        TrackingTarget = target;
        ProjectileColor = owner?.PrimaryColor ?? Color.Purple; // 怪物投射物默认紫色

        float dx = tx - x;
        float dy = ty - y;
        float dist = Math.Max(1, (float)Math.Sqrt(dx * dx + dy * dy));

        float speed = GetBaseSpeed(type);
        LifeTime = GetInitialLifeTime(type);

        Dx = (dx / dist) * speed;
        Dy = (dy / dist) * speed;

        // 特殊属性初始化
        if (type == "METEOR") 
        {
            ExplosionRadius = 100;
            Size = 20;
        }
        else if (type == "ROCKET")
        {
            PenetrationCount = 5 + (owner?.Level ?? 0) / 2; // 【穿透】
            Size = 10;
        }
        else if (type == "BLACK_HOLE")
        {
            ExplosionRadius = 250;
            LifeTime = 300;
            speed = 4.0f;
            Dx = (dx / dist) * speed;
            Dy = (dy / dist) * speed;
            Size = 15;
        }
        else if (type == "LIGHTNING")
        {
            ChainCount = 3 + (owner?.Level ?? 0) / 3; // 【闪电链】
        }
    }

    private float GetBaseSpeed(string type)
    {
        return type switch
        {
            "ROCKET" => 40.0f, // 【急速火箭】
            "CANNON" => 12.0f,
            "PLASMA" => 25.0f,
            "LIGHTNING" => 45.0f, // 【闪电链急速】
            "SPIT" => 15.0f,
            "INK" => 14.0f,
            "METEOR" => 18.0f,
            "BLACK_HOLE" => 4.0f,
            "DEATH_RAY" => 40.0f,
            _ => 25.0f
        };
    }

    /// <summary>
    /// 更新投射物状态
    /// </summary>
    public void Update()
    {
        // 记录轨迹
        if (Trail.Count > 10) Trail.RemoveAt(0);
        Trail.Add(new PointF(X, Y));

        // 追踪逻辑 (机器人或怪物)
        float lifePercent = LifeTime / (float)GetInitialLifeTime(Type);
        bool hasTarget = (TrackingTarget != null && TrackingTarget.IsActive && !TrackingTarget.IsDead) ||
                         (TrackingMonster != null && TrackingMonster.IsActive && !TrackingMonster.IsDead);

        if (hasTarget && lifePercent > 0.2f)
        {
            float targetX, targetY;
            if (TrackingTarget != null)
            {
                targetX = TrackingTarget.X + TrackingTarget.Size / 2;
                targetY = TrackingTarget.Y + TrackingTarget.Size / 2;
            }
            else
            {
                targetX = TrackingMonster!.X + TrackingMonster.Size / 2;
                targetY = TrackingMonster.Y + TrackingMonster.Size / 2;
            }

            float curDx = targetX - X;
            float curDy = targetY - Y;
            float dist = Math.Max(1, (float)Math.Sqrt(curDx * curDx + curDy * curDy));

            float targetDx = (curDx / dist) * GetBaseSpeed(Type);
            float targetDy = (curDy / dist) * GetBaseSpeed(Type);

            float lerp = Type switch
            {
                "ROCKET" => 0.12f,
                "PLASMA" => 0.20f,
                "LIGHTNING" => 0.25f,
                "BULLET" => 0.05f,
                _ => 0.08f
            };
            Dx = Dx * (1 - lerp) + targetDx * lerp;
            Dy = Dy * (1 - lerp) + targetDy * lerp;
        }
        else
        {
            TrackingTarget = null;
            TrackingMonster = null;
        }

        // 移动
        X += Dx;
        Y += Dy;

        // 炮弹重力下坠
        if (Type == "CANNON") Dy += 0.15f;

        // 唾液/墨水随机波动
        if (Type == "SPIT" || Type == "INK")
        {
            X += (float)(new Random().NextDouble() - 0.5) * 2;
            Y += (float)(new Random().NextDouble() - 0.5) * 2;
        }

        LifeTime--;
        if (LifeTime <= 0) IsActive = false;
    }

    /// <summary>
    /// 检测与目标圆形边界的碰撞
    /// </summary>
    public bool CheckCollision(float targetX, float targetY, float targetSize)
    {
        float dx = X - (targetX + targetSize / 2);
        float dy = Y - (targetY + targetSize / 2);
        float distSq = dx * dx + dy * dy;

        // 【等离子碰撞优化】收紧碰撞判定，使其看起来确实接触到了核心
        float projectileHitSize = (Size > 0 ? Size : 8);
        if (Type == "PLASMA") projectileHitSize *= 0.6f; 
        
        float radius = (targetSize * 0.42f) + (projectileHitSize / 2f);
        return distSq < radius * radius;
    }

    private int GetInitialLifeTime(string type)
    {
        // 【进一步调优】寿命不仅要短(性能)，还要能跑完全程(逻辑)。以 1200 像素射程计算
        return type switch
        {
            "LIGHTNING" => 45,     // 速度 45, 需要足够击中 1200 远的目标
            "CANNON" => 100,       
            "BLACK_HOLE" => 300,   
            "METEOR" => 150,       
            "DEATH_RAY" => 45,     // 保证贯穿射程
            "SPIT" => 80,         
            "INK" => 80,           
            "ROCKET" => 120,       
            "PLASMA" => 65,        
            _ => 90               // 普通子弹寿命设为 90 帧，足以覆盖 1200 像素
        };
    }
}