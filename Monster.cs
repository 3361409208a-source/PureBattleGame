using System;
using System.Drawing;

namespace PureBattleGame;

/// <summary>
/// 怪物实体 - 被投放后所有机器人会集火攻击
/// </summary>
public class Monster
{
    // 位置
    public float X { get; set; }
    public float Y { get; set; }

    // 速度
    public float Dx { get; set; }
    public float Dy { get; set; }
    public string Type { get; set; } = "SLIME";

    // 大小
    public int Size { get; set; } = 35; // 58 * 0.6 ≈ 35

    // 生命值
    public int HP { get; set; } = 100;
    public int MaxHP { get; set; } = 100;

    // 状态
    public bool IsActive { get; set; } = true;
    public bool IsDead { get; set; } = false;

    // 动画
    public int AnimationFrame { get; set; } = 0;
    public int AnimationCounter { get; set; } = 0;

    // 随机数生成器
    public Random Rand { get; set; } = new Random();

    // 伤害文字
    public string? DamageText { get; set; }
    public int DamageTextTimer { get; set; } = 0;

    // 受击闪烁
    public int HitFlashTimer { get; set; } = 0;

    // 移动目标点（随机游走）
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public int ChangeTargetTimer { get; set; } = 0;

    // 战斗属性
    public int AttackCooldown { get; set; } = 0;
    
    public Monster(float x, float y)
    {
        X = x;
        Y = y;
        TargetX = x;
        TargetY = y;
        
        string[] types = { "SLIME", "SPIDER", "BAT", "WORM" };
        Type = types[Rand.Next(types.Length)];
        HP = MaxHP;
    }

    /// <summary>
    /// 获取怪物中心坐标
    /// </summary>
    public (float centerX, float centerY) GetCenter()
    {
        return (X + Size / 2f, Y + Size / 2f);
    }

    /// <summary>
    /// 更新怪物状态
    /// </summary>
    public void Update(int screenWidth, int screenHeight, List<Robot> robots)
    {
        if (!IsActive || IsDead) return;

        // 动画更新
        AnimationCounter++;
        if (AnimationCounter >= 8)
        {
            AnimationCounter = 0;
            AnimationFrame = (AnimationFrame + 1) % 4;
        }

        // 受击闪烁递减
        if (HitFlashTimer > 0) HitFlashTimer--;

        // 伤害文字递减
        if (DamageTextTimer > 0) DamageTextTimer--;

        // 冷却时间递减
        if (AttackCooldown > 0) AttackCooldown--;

        // 寻找目标，优先攻击基地
        Robot? nearestTarget = null;
        float nearestDist = float.MaxValue;
        
        var baseTarget = robots.FirstOrDefault(r => r.IsActive && !r.IsDead && r.ClassType == RobotClass.Base);
        if (baseTarget != null)
        {
            nearestTarget = baseTarget;
        }
        else
        {
            foreach (var robot in robots)
            {
                if (!robot.IsActive || robot.IsDead) continue;
                
                float dxTarget = (robot.X + robot.Size/2) - (X + Size/2);
                float dyTarget = (robot.Y + robot.Size/2) - (Y + Size/2);
                float distToRobot = (float)Math.Sqrt(dxTarget * dxTarget + dyTarget * dyTarget);
                
                if (distToRobot < nearestDist)
                {
                    nearestDist = distToRobot;
                    nearestTarget = robot;
                }
            }
        }

        if (nearestTarget != null)
        {
            // 有目标时，向目标移动（速度较慢，有压迫感）
            TargetX = nearestTarget.X + nearestTarget.Size / 2;
            TargetY = nearestTarget.Y + nearestTarget.Size / 2;
            
            float dx = TargetX - (X + Size / 2);
            float dy = TargetY - (Y + Size / 2);
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            
            if (dist > 50)
            {
                Dx += (dx / dist) * 0.15f; // 怪物移动较慢
                Dy += (dy / dist) * 0.15f;
            }

            // 发动攻击
            if (AttackCooldown <= 0 && dist < 300)
            {
                int wave = BattleForm.Instance?.CurrentWave ?? 1;
                // 波次越高，攻击频率越高
                AttackCooldown = Math.Max(30, 90 - wave * 2); 
                
                // 怪物特有攻击：全方位毒液散射或指向性重击
                int attackType = Rand.Next(100);
                if (attackType < 40 + wave) // 随着波次增加，散射概率提高
                {
                    // 随着波次增加，散射的弹丸数量增加
                    int projectiles = Math.Min(16, 8 + wave / 2);
                    for (int i = 0; i < projectiles; i++)
                    {
                        float angle = i * (float)Math.PI * 2 / projectiles;
                        float projDx = (float)Math.Cos(angle) * 100;
                        float projDy = (float)Math.Sin(angle) * 100;
                        
                        var p = new Projectile(null, X + Size/2, Y + Size/2, 
                                             X + Size/2 + projDx, Y + Size/2 + projDy, "INK");
                        p.IsMonsterProjectile = true; // 标记为怪物投射物
                        BattleForm.Instance?.AddProjectile(p);
                    }
                }
                else // 指向性减速口水
                {
                    var p = new Projectile(null, X + Size/2, Y + Size/2, 
                                         TargetX, TargetY, "SPIT");
                    p.IsMonsterProjectile = true;
                    BattleForm.Instance?.AddProjectile(p);
                }
            }
        }
        else
        {
            // 没有目标时随机游走
            ChangeTargetTimer--;
            if (ChangeTargetTimer <= 0)
            {
                ChangeTargetTimer = 60 + Rand.Next(60);
                int maxX = Math.Max(1, screenWidth - Size);
                int maxY = Math.Max(1, screenHeight - Size);
                TargetX = Rand.Next(maxX);
                TargetY = Rand.Next(maxY);
            }
            
            float dxWalk = TargetX - X;
            float dyWalk = TargetY - Y;
            float distWalk = (float)Math.Sqrt(dxWalk * dxWalk + dyWalk * dyWalk);

            if (distWalk > 10)
            {
                Dx += (dxWalk / distWalk) * 0.3f;
                Dy += (dyWalk / distWalk) * 0.3f;
            }
        }

        // 摩擦力
        Dx *= 0.95f;
        Dy *= 0.95f;

        // 应用速度
        X += Dx;
        Y += Dy;

        // 边界限制
        if (X < 0) { X = 0; Dx = -Dx * 0.8f; }
        if (X > screenWidth - Size) { X = screenWidth - Size; Dx = -Dx * 0.8f; }
        if (Y < 0) { Y = 0; Dy = -Dy * 0.8f; }
        if (Y > screenHeight - Size) { Y = screenHeight - Size; Dy = -Dy * 0.8f; }
    }

    public void OnHit(Projectile proj)
    {
        if (proj.Owner is Robot bot)
        {
            int damage = bot.GetProjectileDamage(proj.Type);
            TakeDamage(damage);
        }
        else
        {
            // Fallback
            TakeDamage(10);
        }
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        HP -= damage;
        HitFlashTimer = 10;
        BattleForm.Instance?.AddFloatingText(X + Size / 2, Y - 10, $"-{damage}", Color.OrangeRed);

        if (HP <= 0)
        {
            HP = 0;
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        IsActive = false;
        
        // 怪物死亡掉落金币 - 提高奖励
        if (BattleForm.Instance != null)
        {
            // 大Boss掉落更多，小怪也增加奖励
            int goldReward = Size > 50 ? 800 : 50;
            BattleForm.Instance.Gold += goldReward;
        }
    }
}