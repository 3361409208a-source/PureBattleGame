using System;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

/// <summary>
/// 基地围墙分节 - 由建造者在该位置施工生成
/// </summary>
public class WallSegment
{
    public float Angle { get; set; } // 在环形中的角度 (0-2PI)
    public float Radius { get; set; } // 距离基地的半径
    public float Thickness { get; set; } // 墙体厚度
    public int Layer { get; set; } = 0; // 0: 内层, 1: 外层
    
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public bool IsActive => HP > 0;
    
    // 视觉反馈
    public int HitFlashTimer { get; set; } = 0;
    public int RepairEffectTimer { get; set; } = 0;
    
    // 该分节当前可能正被某个工程单位锁定，防止多单位无效重叠
    public Robot? LockingRobot { get; set; }
    
    // 该分节当前驻扎的战斗单位，用于实现防线平均分布
    public Robot? GarrisonRobot { get; set; }

    public WallSegment(float angle, float radius, float thickness, int maxHP)
    {
        Angle = angle;
        Radius = radius;
        Thickness = thickness;
        MaxHP = maxHP;
        HP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        if (!IsActive) return;

        // --- 核心保护：所有新开工的外层环区在未闭合供能前，处于无敌的“虚影”蓝图状态 ---
        if (Layer > 0 && BattleForm.Instance != null && !BattleForm.Instance.IsLayerComplete(Layer))
        {
            return;
        }

        HP -= damage;
        HitFlashTimer = 10;
    }

    public void Repair(int amount)
    {
        if (HP < MaxHP)
        {
            HP = Math.Min(MaxHP, HP + amount);
            RepairEffectTimer = 15;
        }
    }

    // 获取该分节在世界坐标系中的估算中心点 (用于寻路)
    public PointF GetWorldPosition(float centerX, float centerY)
    {
        float x = centerX + (float)Math.Cos(Angle) * Radius;
        float y = centerY + (float)Math.Sin(Angle) * Radius;
        return new PointF(x, y);
    }
}
