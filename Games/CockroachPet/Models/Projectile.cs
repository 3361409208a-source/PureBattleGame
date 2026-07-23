using System.Collections.Generic;
using System.Drawing;

namespace PureBattleGame.Games.CockroachPet;

public class Projectile
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Dx { get; set; }
    public float Dy { get; set; }
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public Robot Owner { get; set; }
    public Color ProjectileColor { get; set; }
    public bool IsActive { get; set; } = true;
    public string Type { get; set; } // BULLET, ROCKET, PLASMA, CANNON, LIGHTNING, SPIT, INK
    public int LifeTime { get; set; }
    public Robot? TrackingTarget { get; set; } // 锁定目标：实时修正轨迹
    public List<PointF> Trail { get; set; } = new List<PointF>();

    public Projectile(Robot owner, float x, float y, float tx, float ty, string type = "BULLET", Robot? target = null)
    {
        Owner = owner;
        X = x;
        Y = y;
        TargetX = tx;
        TargetY = ty;
        Type = type;
        TrackingTarget = target;
        ProjectileColor = owner.PrimaryColor;

        float dx = tx - x;
        float dy = ty - y;
        float dist = (float)Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        
        float speed = GetBaseSpeed(type);

        LifeTime = type switch {
            "LIGHTNING" => 30,
            "CANNON" => 180,
            _ => 150
        };

        Dx = (dx / dist) * speed;
        Dy = (dy / dist) * speed;
    }

    private float GetBaseSpeed(string type)
    {
        return type switch {
            "ROCKET" => 12.0f,
            "CANNON" => 8.0f,
            "PLASMA" => 20.0f,
            "LIGHTNING" => 35.0f,
            "SPIT" => 15.0f,
            "INK" => 14.0f,
            "PULSE" => 18.0f,
            "BLASTER" => 30.0f,
            "BOOMERANG" => 16.0f,
            "SHURIKEN" => 22.0f,
            "GRENADE" => 10.0f,
            "FIREBALL" => 25.0f,
            "ICE_SHARD" => 28.0f,
            _ => 25.0f
        };
    }

    public void Update()
    {
        float lifePercent = LifeTime / (float)GetInitialLifeTime(Type);
        
        // Boomerang 弧线返回
        if (Type == "BOOMERANG" && lifePercent < 0.5f && Owner != null)
        {
            float backDx = (Owner.X + Owner.Size/2) - X;
            float backDy = (Owner.Y + Owner.Size/2) - Y;
            float dist = Math.Max(1, (float)Math.Sqrt(backDx * backDx + backDy * backDy));
            float returnSpeed = GetBaseSpeed(Type) * 1.5f; // 返回时加速
            Dx = Dx * 0.9f + (backDx / dist) * returnSpeed * 0.1f;
            Dy = Dy * 0.9f + (backDy / dist) * returnSpeed * 0.1f;
        }
        // 普通追踪锁定逻辑
        else if (TrackingTarget != null && TrackingTarget.IsActive && !TrackingTarget.IsDead && lifePercent > 0.2f)
        {
            float tx = TrackingTarget.X + TrackingTarget.Size / 2;
            float ty = TrackingTarget.Y + TrackingTarget.Size / 2;
            float curDx = tx - X;
            float curDy = ty - Y;
            float dist = (float)Math.Max(1, Math.Sqrt(curDx * curDx + curDy * curDy));

            float targetDx = (curDx / dist) * GetBaseSpeed(Type);
            float targetDy = (curDy / dist) * GetBaseSpeed(Type);

            float lerp = Type switch {
                "ROCKET" => 0.12f,
                "PLASMA" => 0.20f,
                "LIGHTNING" => 0.25f,
                "BULLET" => 0.05f,
                "PULSE" => 0.30f,
                "FIREBALL" => 0.08f,
                "ICE_SHARD" => 0.05f,
                _ => 0.08f
            };
            Dx = Dx * (1 - lerp) + targetDx * lerp;
            Dy = Dy * (1 - lerp) + targetDy * lerp;
        }
        else
        {
            TrackingTarget = null;
        }

        X += Dx;
        Y += Dy;

        // 抛物线/重力下坠
        if (Type == "CANNON") Dy += 0.15f;
        if (Type == "GRENADE") Dy += 0.25f;

        // 唾液/墨水 随机波动
        if (Type == "SPIT" || Type == "INK" || Type == "PULSE")
        {
            X += (float)(new Random().NextDouble() - 0.5) * 2;
            Y += (float)(new Random().NextDouble() - 0.5) * 2;
        }

        LifeTime--;
        if (LifeTime <= 0) IsActive = false;
    }

    private int GetInitialLifeTime(string type)
    {
        return type switch {
            "LIGHTNING" => 30,
            "BLASTER" => 40,
            "CANNON" => 180,
            "GRENADE" => 120,
            "BOOMERANG" => 160,
            _ => 150
        };
    }
}
