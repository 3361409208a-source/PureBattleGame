using System;
using System.Drawing;

namespace PureBattleGame.Games.CockroachPet;

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

    // 大小
    public int Size { get; set; } = 96;

    // 生命值
    public int HP { get; set; } = 5000;
    public int MaxHP { get; set; } = 5000;

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

    public Monster(float x, float y)
    {
        X = x;
        Y = y;
        TargetX = x;
        TargetY = y;
        HP = MaxHP;
    }

    public void Update(int screenWidth, int screenHeight)
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

        // 随机游走逻辑
        ChangeTargetTimer--;
        if (ChangeTargetTimer <= 0)
        {
            ChangeTargetTimer = 60 + Rand.Next(60);
            TargetX = Rand.Next(screenWidth - Size);
            TargetY = Rand.Next(screenHeight - Size);
        }

        // 向目标移动
        float dx = TargetX - X;
        float dy = TargetY - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist > 10)
        {
            Dx += (dx / dist) * 0.3f;
            Dy += (dy / dist) * 0.3f;
        }

        // 摩擦力
        Dx *= 0.95f;
        Dy *= 0.95f;

        // 应用速度
        X += Dx;
        Y += Dy;

        // 边界限制
        if (X < 0) { X = 0; Dx = -Dx; }
        if (X > screenWidth - Size) { X = screenWidth - Size; Dx = -Dx; }
        if (Y < 0) { Y = 0; Dy = -Dy; }
        if (Y > screenHeight - Size) { Y = screenHeight - Size; Dy = -Dy; }

        // 死亡检测
        if (HP <= 0 && !IsDead)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        HP -= damage;
        HitFlashTimer = 10;
        DamageText = $"-{damage}";
        DamageTextTimer = 30;

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
    }

    /// <summary>
    /// 获取怪物中心坐标
    /// </summary>
    public (float centerX, float centerY) GetCenter()
    {
        return (X + Size / 2f, Y + Size / 2f);
    }
}
