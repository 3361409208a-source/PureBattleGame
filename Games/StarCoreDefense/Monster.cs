using System;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

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
    public int ParalyzeTimer { get; set; } = 0; // 麻痹时间 (帧)
    public float ArmorResist { get; set; } = 0f; // 物理抗性 0~1
    public bool IsRanged { get; set; } = false;  // 远程型：保持距离
    public bool IsElite { get; set; } = false;   // 精英型：金边特效
    public int GoldReward { get; set; } = 50;    // 独立奖励
    
    public Monster(float x, float y, int wave = 1)
    {
        X = x;
        Y = y;
        TargetX = x;
        TargetY = y;
        
        // 按波次随机选择怪物类型
        int roll = Rand.Next(100);
        if (wave >= 5 && roll < 15) // 精英兵 (5波后15%概率)
        {
            Type = "ELITE";
            IsElite = true;
            Size = 50;
            GoldReward = 200;
        }
        else if (wave >= 3 && roll < 35) // 装甲兵 (3波后20%概率)
        {
            Type = "ARMORED";
            ArmorResist = 0.45f;
            Size = 45;
            GoldReward = 100;
        }
        else if (wave >= 2 && roll < 55) // 远程兵 (2波后20%概率)
        {
            Type = "RANGED";
            IsRanged = true;
            Size = 28;
            GoldReward = 80;
        }
        else // 普通怪
        {
            string[] types = { "SLIME", "SPIDER", "BAT", "WORM" };
            Type = types[Rand.Next(types.Length)];
        }

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
        
        // 麻痹逻辑
        if (ParalyzeTimer > 0)
        {
            ParalyzeTimer--;
            Dx *= 0.5f; // 麻痹时剧烈减速
            Dy *= 0.5f;
            X += Dx;
            Y += Dy;
            return; // 不执行后续追踪和攻击逻辑
        }

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
            
            // 按类型决定移动策略
            float moveSpeed = IsElite ? 0.22f : (ArmorResist > 0 ? 0.1f : 0.15f);
            float keepDist = IsRanged ? 400f : 50f; // 远程兵保持射程

            if (dist > keepDist)
            {
                Dx += (dx / dist) * moveSpeed;
                Dy += (dy / dist) * moveSpeed;
            }
            else if (IsRanged && dist < keepDist - 80) // 远程兵拉开距离
            {
                Dx -= (dx / dist) * moveSpeed * 1.5f;
                Dy -= (dy / dist) * moveSpeed * 1.5f;
            }

            // 发动攻击
            if (AttackCooldown <= 0 && dist < (IsRanged ? 500 : 300))
            {
                int wave = BattleForm.Instance?.CurrentWave ?? 1;
                AttackCooldown = Math.Max(30, 90 - wave * 2); 
                
                int attackType = Rand.Next(100);
                if (!IsRanged && attackType < 40 + wave) // 近战散射
                {
                    int projectiles = Math.Min(16, 8 + wave / 2);
                    for (int i = 0; i < projectiles; i++)
                    {
                        float angle = i * (float)Math.PI * 2 / projectiles;
                        float projDx = (float)Math.Cos(angle) * 100;
                        float projDy = (float)Math.Sin(angle) * 100;
                        
                        var p = new Projectile(null, X + Size/2, Y + Size/2, 
                                             X + Size/2 + projDx, Y + Size/2 + projDy, "INK");
                        p.IsMonsterProjectile = true;
                        BattleForm.Instance?.AddProjectile(p);
                    }
                }
                else // 精准远程/精英重击
                {
                    string projType = IsElite ? "CANNON" : "SPIT";
                    var p = new Projectile(null, X + Size/2, Y + Size/2, TargetX, TargetY, projType);
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
                // 没有目标时在大范围内游走，而不是屏幕内
            ChangeTargetTimer = 120 + Rand.Next(120);
            TargetX = X + (float)(Rand.NextDouble() - 0.5) * 1000;
            TargetY = Y + (float)(Rand.NextDouble() - 0.5) * 1000;
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
    }

    public void OnHit(Projectile proj)
    {
        int rawDamage = 10;
        if (proj.Owner is Robot bot)
        {
            rawDamage = bot.GetProjectileDamage(proj.Type);
        }
        // 装甲怪物造成伤害减免
        int finalDamage = (int)(rawDamage * (1f - ArmorResist));
        TakeDamage(Math.Max(1, finalDamage)); // 至少冒建1点上去
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