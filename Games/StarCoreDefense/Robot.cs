using System;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

/// <summary>
/// 机器人战斗实体 - 纯战斗版本
/// 功能：移动、攻击怪物、攻击其他机器人、格斗
/// </summary>
public enum RobotClass
{
    Base,       // 基地
    Worker,     // 采集/生产型
    Healer,     // 治疗/辅助型 (原防御者)
    Shooter,    // 攻击/远程型
    Guardian    // 守卫者 (近战/击退型)
}

public partial class Robot
{
    // 采集
    public Mineral? TargetMineral { get; set; }
    private int _miningTimer = 0;

    // 治疗技能
    public List<Robot> HealingTargets { get; } = new List<Robot>();
    private int _healCooldown = 0;

    // 兵种类型
    public RobotClass ClassType { get; set; } = RobotClass.Shooter;

    // 基本信息
    public int Id { get; set; }
    public string Name { get; set; } = "";

    // 位置与移动
    public float X { get; set; }
    public float Y { get; set; }
    public float Dx { get; set; }
    public float Dy { get; set; }
    public bool FacingRight { get; set; } = true;

    // 状态
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsDead { get; set; } = false;
    public bool IsMoving { get; set; } = true;
    public int UltimateCooldown { get; set; } = 0; // 大招冷却

    // 外观
    public int Size { get; set; } = 23; // 38 * 0.6 ≈ 23
    public Color PrimaryColor { get; set; }
    public Color SecondaryColor { get; set; }
    public Color EyeColor { get; set; }
    public float SpeedMultiplier { get; set; } = 1.0f;
    public int Opacity { get; set; } = 255; // 透明度 0-255

    // 生命值
    public int HP { get; set; } = 1000;
    public int MaxHP { get; set; } = 1000;

    // 战斗属性
    public bool IsWeaponMaster { get; set; } = false;
    public int ShootCooldown { get; set; } = 0;
    public string CurrentAttackType { get; set; } = "LASER";

    // 攻击状态
    public bool IsFiringLaser { get; set; } = false;
    public float LaserTargetX { get; set; }
    public float LaserTargetY { get; set; }
    public Robot? TargetRobot { get; set; }

    // 追逐
    public Robot? ChasingTarget { get; set; }
    public Monster? MonsterTarget { get; set; }
    public int ChaseTimer { get; set; } = 0;

    // 陀螺格斗
    public Robot? DuelTarget { get; set; }
    public int DuelTimer { get; set; } = 0;

    // 状态效果
    public int StunTimer { get; set; } = 0;
    public int SlowTimer { get; set; } = 0;
    public string SpecialState { get; set; } = "NORMAL";
    public int SpecialStateTimer { get; set; } = 0;

    // 反馈
    public string? LastDamageText { get; set; }
    public int DamageTextTimer { get; set; } = 0;
    public int DamageFeedbackTimer { get; set; } = 0;

    // 动画
    public int AnimationFrame { get; set; } = 0;
    public int AnimationCounter { get; set; } = 0;
    public float[] TentacleOffsets { get; set; } = new float[8];

    // 延迟攻击
    private int _delayedAttackTimer = 0;

    // 随机
    public Random Rand { get; set; } = new Random();

    // 触摸/漂浮效果
    public float ShakingOffset { get; set; } = 0;
    public float RotationAngle { get; set; } = 0;

    public Robot(int id, string name, float x, float y, RobotClass classType = RobotClass.Shooter)
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;
        ClassType = classType;

        // 根据兵种设置不同属性
        switch (ClassType)
        {
            case RobotClass.Base:
                PrimaryColor = Color.FromArgb(100, 150, 255); // 蓝色
                SecondaryColor = Color.FromArgb(50, 100, 200);
                EyeColor = Color.Yellow;
                Size = 40;
                SpeedMultiplier = 0.0f; // 基地不移动
                MaxHP = 3000;
                HP = MaxHP;
                break;
            case RobotClass.Worker:
                PrimaryColor = Color.FromArgb(255, 200, 77); // 金黄色
                SecondaryColor = Color.FromArgb(255, 170, 51);
                EyeColor = Color.White;
                Size = 18;
                SpeedMultiplier = 1.5f;
                MaxHP = 500;
                HP = MaxHP;
                break;
            case RobotClass.Healer:
                PrimaryColor = Color.FromArgb(77, 255, 120); // 绿色
                SecondaryColor = Color.FromArgb(51, 200, 100);
                EyeColor = Color.White;
                Size = 25;
                SpeedMultiplier = 1.2f;
                MaxHP = 1500;
                HP = MaxHP;
                break;
            case RobotClass.Shooter:
                PrimaryColor = Color.FromArgb(255, 107, 107); // 红色
                SecondaryColor = Color.FromArgb(255, 77, 77);
                EyeColor = Color.Cyan;
                Size = 23;
                SpeedMultiplier = 1.0f;
                MaxHP = 1000;
                HP = MaxHP;
                break;
            case RobotClass.Guardian:
                PrimaryColor = Color.FromArgb(128, 128, 128); // 铁灰色
                SecondaryColor = Color.FromArgb(80, 80, 80);
                EyeColor = Color.Orange;
                Size = 30; // 体型较大
                SpeedMultiplier = 1.1f;
                MaxHP = 2000; // 血量较厚
                HP = MaxHP;
                break;
        }

        // 随机初始方向，如果不是基地
        if (ClassType != RobotClass.Base)
        {
            double angle = Rand.NextDouble() * Math.PI * 2;
            float speed = 1.5f + (float)Rand.NextDouble() * 1.5f;
            Dx = (float)Math.Cos(angle) * speed;
            Dy = (float)Math.Sin(angle) * speed;
        }
        else
        {
            Dx = 0;
            Dy = 0;
        }
    }

    /// <summary>
    /// 根据当前兵种应用属性
    /// </summary>
    public void ApplyClassProperties()
    {
        // 基础属性
        switch (ClassType)
        {
            case RobotClass.Base:
                PrimaryColor = Color.FromArgb(100, 150, 255);
                SecondaryColor = Color.FromArgb(50, 100, 200);
                EyeColor = Color.Yellow;
                Size = 40;
                SpeedMultiplier = 0.0f;
                MaxHP = 3000;
                HP = MaxHP;
                break;
            case RobotClass.Worker:
                PrimaryColor = Color.FromArgb(255, 217, 102); // 黄色
                SecondaryColor = Color.FromArgb(255, 195, 0);
                EyeColor = Color.Black;
                Size = 18;
                SpeedMultiplier = 1.2f;
                MaxHP = (int)(600 * (1 + 0.2f * (BattleForm.Instance?._workerLevel - 1 ?? 0))); // 根据等级提升血量
                HP = MaxHP;
                break;
            case RobotClass.Healer:
                PrimaryColor = Color.FromArgb(116, 185, 255); // 蓝色
                SecondaryColor = Color.FromArgb(9, 132, 227);
                EyeColor = Color.White;
                Size = 25;
                SpeedMultiplier = 0.9f;
                MaxHP = (int)(1200 * (1 + 0.2f * (BattleForm.Instance?._healerLevel - 1 ?? 0)));
                HP = MaxHP;
                break;
            case RobotClass.Shooter:
                PrimaryColor = Color.FromArgb(255, 107, 107); // 红色
                SecondaryColor = Color.FromArgb(255, 77, 77);
                EyeColor = Color.Cyan;
                Size = 23;
                SpeedMultiplier = 1.0f;
                MaxHP = (int)(1000 * (1 + 0.2f * (BattleForm.Instance?._shooterLevel - 1 ?? 0)));
                HP = MaxHP;
                break;
            case RobotClass.Guardian:
                PrimaryColor = Color.FromArgb(128, 128, 128); // 铁灰色
                SecondaryColor = Color.FromArgb(80, 80, 80);
                EyeColor = Color.Orange;
                Size = 30; // 体型较大
                SpeedMultiplier = 1.1f;
                MaxHP = (int)(2000 * (1 + 0.2f * (BattleForm.Instance?._guardianLevel - 1 ?? 0))); // 血量较厚
                HP = MaxHP;
                break;
        }

        // 应用全局生命加成
        if (BattleForm.Instance != null)
        {
            MaxHP = (int)(MaxHP * BattleForm.Instance.GlobalHealthMultiplier);
            HP = MaxHP;
        }

        // 随机初始方向，如果不是基地
        if (ClassType != RobotClass.Base)
        {
            double angle = Rand.NextDouble() * Math.PI * 2;
            float speed = 1.5f + (float)Rand.NextDouble() * 1.5f;
            Dx = (float)Math.Cos(angle) * speed;
            Dy = (float)Math.Sin(angle) * speed;
        }
        else
        {
            Dx = 0;
            Dy = 0;
        }

        // 初始速度限制
        float currentSpeed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
        if (currentSpeed > 5.0f)
        {
            Dx = (Dx / currentSpeed) * 5.0f;
            Dy = (Dy / currentSpeed) * 5.0f;
        }
    }

    /// <summary>
    /// 游戏循环更新 - 全自动战斗
    /// </summary>
    public void Update(int screenWidth, int screenHeight, List<Robot> allRobots, List<Monster> allMonsters)
    {
        if (!IsActive || IsDead) return;

        if (ShootCooldown > 0) ShootCooldown--;
        if (UltimateCooldown > 0) UltimateCooldown--;

        // 状态效果递减
        if (StunTimer > 0) StunTimer--;
        if (SlowTimer > 0) SlowTimer--;
        if (DamageTextTimer > 0) DamageTextTimer--;
        if (DamageFeedbackTimer > 0) DamageFeedbackTimer--;

        // 1. 检查目标有效性
        CheckTargetsValidity();

        // 2. 自动寻找目标
        if (ChasingTarget == null && DuelTarget == null && MonsterTarget == null && TargetMineral == null)
        {
            FindAndAssignTarget(allRobots, allMonsters);
        }

        // 3. 执行战斗与生产逻辑
        if (ClassType == RobotClass.Base)
        {
            // 基地不执行任何主动逻辑
        }
        else if (ClassType == RobotClass.Worker)
        {
            UpdateWorkerLogic();
        }
        else if (ClassType == RobotClass.Healer)
        {
            UpdateHealerLogic(allRobots);
        }
        else if (ClassType == RobotClass.Guardian)
        {
            UpdateGuardianLogic(allMonsters);
        }
        else if (MonsterTarget != null)
        {
            UpdateMonsterAttack();
        }
        else if (DuelTarget != null)
        {
            UpdateDuelLogic();
        }
        else if (ChasingTarget != null)
        {
            UpdateChasingRobotLogic();
        }
        else
        {
            UpdateRandomMovement();
        }

        // 4. 辅助逻辑
        UpdateDelayedAttack();
        UpdateLaserTargeting();

        // 5. 应用移动
        ApplyMovement(screenWidth, screenHeight);

        // 6. 死亡检查
        if (HP <= 0 && !IsDead)
        {
            HandleDeath();
        }

        // 7. 更新动画
        UpdateAnimations();
    }

    private void CheckTargetsValidity()
    {
        if (MonsterTarget != null && (!MonsterTarget.IsActive || MonsterTarget.IsDead))
            MonsterTarget = null;

        if (ChasingTarget != null && (!ChasingTarget.IsActive || ChasingTarget.IsDead))
        {
            ChasingTarget = null;
            ChaseTimer = 0;
        }

        if (DuelTarget != null && (!DuelTarget.IsActive || DuelTarget.IsDead))
        {
            DuelTarget = null;
            DuelTimer = 0;
            RotationAngle = 0;
            SpecialState = "NORMAL";
        }
    }

    private void UpdateLaserTargeting()
    {
        if (IsFiringLaser && TargetRobot != null && TargetRobot.IsActive && !TargetRobot.IsDead)
        {
            LaserTargetX = TargetRobot.X + TargetRobot.Size / 2;
            LaserTargetY = TargetRobot.Y + TargetRobot.Size / 2;
        }
        else if (IsFiringLaser && TargetRobot != null) // Target dead/inactive
        {
            IsFiringLaser = false;
            TargetRobot = null;
        }
    }

    /// <summary>
    /// 自动寻找并分配目标
    /// </summary>
    private void FindAndAssignTarget(List<Robot> allRobots, List<Monster> allMonsters)
    {
        // 采集型机器人优先寻找矿物
        if (ClassType == RobotClass.Worker)
        {
            var minerals = BattleForm.Instance?.GetMinerals();
            if (minerals != null)
            {
                Mineral? nearestMineral = null;
                float minDist = float.MaxValue;
                foreach (var m in minerals)
                {
                    if (!m.IsActive) continue;
                    float dxM = m.X - X;
                    float dyM = m.Y - Y;
                    float distM = dxM * dxM + dyM * dyM;
                    if (distM < minDist)
                    {
                        minDist = distM;
                        nearestMineral = m;
                    }
                }
                if (nearestMineral != null)
                {
                    TargetMineral = nearestMineral;
                    return;
                }
            }
        }

        // 优先攻击距离基地最近的怪物
        Monster? targetMonster = null;
        float minBaseDist = float.MaxValue;

        var baseRobot = BattleForm.Instance?.GetBaseRobot();

        if (baseRobot != null)
        {
            foreach (var monster in allMonsters)
            {
                if (monster.IsActive && !monster.IsDead)
                {
                    float dx = monster.X - baseRobot.X;
                    float dy = monster.Y - baseRobot.Y;
                    float dist = dx * dx + dy * dy;
                    if (dist < minBaseDist)
                    {
                        minBaseDist = dist;
                        targetMonster = monster;
                    }
                }
            }
        }
        else
        {
            // 如果没有基地，则退回到寻找距离自己最近的怪物
            float nearestDist = float.MaxValue;
            foreach (var monster in allMonsters)
            {
                if (monster.IsActive && !monster.IsDead)
                {
                    float dx = monster.X - X;
                    float dy = monster.Y - Y;
                    float dist = dx * dx + dy * dy;
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        targetMonster = monster;
                    }
                }
            }
        }

        if (targetMonster != null)
        {
            MonsterTarget = targetMonster;
            return;
        }

        // 如果没有怪物，机器人现在**不再互殴**，而是原地待命或随机巡逻
        MonsterTarget = null;
        ChasingTarget = null;
        DuelTarget = null;
        TargetMineral = null;
    }

    private void UpdateWorkerLogic()
    {
        if (TargetMineral != null && !TargetMineral.IsActive)
        {
            TargetMineral = null;
        }

        if (TargetMineral == null)
        {
            UpdateRandomMovement();
            return;
        }

        // 靠近矿物
        float dx = TargetMineral.X - X;
        float dy = TargetMineral.Y - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist > 10)
        {
            float maxSpeed = 3.0f * SpeedMultiplier;
            Dx = Dx * 0.9f + (dx / dist) * maxSpeed * 0.1f;
            Dy = Dy * 0.9f + (dy / dist) * maxSpeed * 0.1f;
        }
        else
        {
            // 开始采集
            Dx *= 0.5f;
            Dy *= 0.5f;
            _miningTimer++;
            if (_miningTimer >= 60) // 1秒开采一个
            {
                _miningTimer = 0;
                TargetMineral.IsActive = false;
                if (BattleForm.Instance != null)
                {
                    BattleForm.Instance.Minerals += TargetMineral.Value;
                    BattleForm.Instance.RemoveMineral(TargetMineral);
                    BattleForm.Instance.AddExplosion(X, Y, Color.Cyan, 5, "SPARK");
                }
                TargetMineral = null;
            }
        }
    }

    private void UpdateChasingRobotLogic()
    {
        // 机器人不再互殴，清空追逐目标
        ChasingTarget = null;
        ChaseTimer = 0;
    }

    private void UpdateHealerLogic(List<Robot> allRobots)
    {
        HealingTargets.Clear();
        _healCooldown--;

        // 寻找需要治疗的队友（距离300以内，血量不满的，最多3个），优先治疗基地
        var targets = allRobots.Where(r => r != this && r.IsActive && !r.IsDead && r.HP < r.MaxHP)
                               .OrderBy(r => r.ClassType == RobotClass.Base ? 0 : 1) // 优先基地
                               .ThenBy(r => (r.X - X) * (r.X - X) + (r.Y - Y) * (r.Y - Y)) // 按距离排序
                               .Take(3)
                               .ToList();

        foreach (var target in targets)
        {
            float dx = target.X - X;
            float dy = target.Y - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist < 300)
            {
                HealingTargets.Add(target);
                if (_healCooldown <= 0)
                {
                    int healAmount = 20 + ((BattleForm.Instance?._healerLevel ?? 1) - 1) * 5; // 根据等级增加治疗量
                    target.HP = Math.Min(target.MaxHP, target.HP + healAmount);
                    BattleForm.Instance?.AddExplosion(target.X + target.Size / 2, target.Y + target.Size / 2, Color.LimeGreen, 2, "SPARK");
                    BattleForm.Instance?.AddFloatingText(target.X + target.Size / 2, target.Y - 10, $"+{healAmount}", Color.LimeGreen);
                }
            }
        }

        if (_healCooldown <= 0) _healCooldown = 30; // 0.5秒回一次血

        // 移动逻辑：如果周围有怪物，逃跑；否则走向血量最少或者最近的队友
        if (MonsterTarget != null)
        {
            var (monsterX, monsterY) = MonsterTarget.GetCenter();
            float mDx = monsterX - X;
            float mDy = monsterY - Y;
            float mDist = (float)Math.Sqrt(mDx * mDx + mDy * mDy);

            if (mDist < 200)
            {
                // 逃离怪物
                float maxSpeed = 3.0f * SpeedMultiplier;
                Dx -= (mDx / mDist) * 0.5f;
                Dy -= (mDy / mDist) * 0.5f;
            }
            else
            {
                FollowTeammates(targets);
            }
        }
        else
        {
            FollowTeammates(targets);
        }

        // 限制最大速度
        float currentSpeed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
        float limitSpeed = 3.0f * SpeedMultiplier;
        if (currentSpeed > limitSpeed)
        {
            Dx = (Dx / currentSpeed) * limitSpeed;
            Dy = (Dy / currentSpeed) * limitSpeed;
        }
    }

    private void UpdateGuardianLogic(List<Monster> allMonsters)
    {
        // 寻找基地
        Robot? baseTarget = null;
        if (BattleForm.Instance != null)
        {
            baseTarget = BattleForm.Instance.GetBaseRobot();
        }

        if (baseTarget == null || !baseTarget.IsActive || baseTarget.IsDead)
        {
            UpdateRandomMovement();
            return;
        }

        // 寻找距离基地最近的怪物（保护半径 150）
        float protectionRadius = 150.0f;
        Monster? targetMonster = null;
        float minBaseDist = float.MaxValue;

        foreach (var m in allMonsters)
        {
            if (!m.IsActive || m.IsDead) continue;
            float dxBase = (m.X + m.Size / 2) - (baseTarget.X + baseTarget.Size / 2);
            float dyBase = (m.Y + m.Size / 2) - (baseTarget.Y + baseTarget.Size / 2);
            float distToBase = (float)Math.Sqrt(dxBase * dxBase + dyBase * dyBase);

            if (distToBase < protectionRadius && distToBase < minBaseDist)
            {
                minBaseDist = distToBase;
                targetMonster = m;
            }
        }

        float maxSpeed = 4.0f * SpeedMultiplier;

        if (targetMonster != null)
        {
            // 有怪物在保护范围内，冲向该怪物
            var (monsterX, monsterY) = targetMonster.GetCenter();
            float dx = monsterX - (X + Size / 2);
            float dy = monsterY - (Y + Size / 2);
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist < 1f) dist = 1f;

            Dx = Dx * 0.9f + (dx / dist) * maxSpeed * 0.3f;
            Dy = Dy * 0.9f + (dy / dist) * maxSpeed * 0.3f;

            // 如果距离足够近，触发撞击技能
            if (dist < Size / 2 + targetMonster.Size / 2 + 15) // 攻击距离稍大于碰撞距离
            {
                if (ShootCooldown <= 0)
                {
                    // 施放撞击技能
                    ShootCooldown = 60; // 1秒冷却
                    SpecialState = "SHAKING";
                    SpecialStateTimer = 20;

                    // 造成伤害
                    int levelBonus = BattleForm.Instance?._guardianLevel - 1 ?? 0;
                    int damage = (int)(40 * (1 + levelBonus * 0.2f));
                    targetMonster.TakeDamage(damage);

                    // 施加击退效果 (将怪物往基地反方向推开)
                    float pushForce = 20.0f; // 击退力度
                    float pushDx = (targetMonster.X + targetMonster.Size / 2) - (baseTarget.X + baseTarget.Size / 2);
                    float pushDy = (targetMonster.Y + targetMonster.Size / 2) - (baseTarget.Y + baseTarget.Size / 2);
                    float pushDist = (float)Math.Sqrt(pushDx * pushDx + pushDy * pushDy);
                    if (pushDist > 0)
                    {
                        targetMonster.Dx += (pushDx / pushDist) * pushForce;
                        targetMonster.Dy += (pushDy / pushDist) * pushForce;
                    }

                    // 视觉反馈
                    BattleForm.Instance?.AddExplosion(monsterX, monsterY, Color.White, 8, "SPARK");
                }
            }
        }
        else
        {
            // 保护范围内没有怪物，在基地周围巡逻
            float dx = (baseTarget.X + baseTarget.Size / 2) - (X + Size / 2);
            float dy = (baseTarget.Y + baseTarget.Size / 2) - (Y + Size / 2);
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist > 80) // 离基地太远，靠近基地
            {
                Dx = Dx * 0.9f + (dx / dist) * maxSpeed * 0.1f;
                Dy = Dy * 0.9f + (dy / dist) * maxSpeed * 0.1f;
            }
            else // 在基地附近随机游走
            {
                if (Rand.Next(100) < 5)
                {
                    double angle = Rand.NextDouble() * Math.PI * 2;
                    float speed = (float)Rand.NextDouble() * maxSpeed * 0.5f;
                    Dx = (float)Math.Cos(angle) * speed;
                    Dy = (float)Math.Sin(angle) * speed;
                }
            }
        }

        // 限制最大速度
        float currentSpeed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
        if (currentSpeed > maxSpeed)
        {
            Dx = (Dx / currentSpeed) * maxSpeed;
            Dy = (Dy / currentSpeed) * maxSpeed;
        }
    }

    private void FollowTeammates(List<Robot> targets)
    {
        if (targets.Any())
        {
            var target = targets.First();
            float dx = target.X - X;
            float dy = target.Y - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist > 100) // 保持100的距离
            {
                float maxSpeed = 3.0f * SpeedMultiplier;
                Dx = Dx * 0.9f + (dx / dist) * maxSpeed * 0.1f;
                Dy = Dy * 0.9f + (dy / dist) * maxSpeed * 0.1f;
            }
            else
            {
                Dx *= 0.9f;
                Dy *= 0.9f;
            }
        }
        else
        {
            UpdateRandomMovement();
        }
    }

    private void UpdateMonsterAttack()
    {
        if (MonsterTarget == null || !MonsterTarget.IsActive || MonsterTarget.IsDead)
        {
            MonsterTarget = null;
            return;
        }

        var (monsterX, monsterY) = MonsterTarget.GetCenter();
        float dx = monsterX - (X + Size / 2);
        float dy = monsterY - (Y + Size / 2);
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist < 1f) dist = 1f;

        float maxSpeed = 3.0f * SpeedMultiplier;

        // 根据兵种决定走位和攻击距离
        if (ClassType == RobotClass.Healer)
        {
            // 治疗型不主动攻击怪物，此逻辑在 UpdateHealerLogic 中处理
            return;
        }
        else if (ClassType == RobotClass.Shooter)
        {
            // 输出型：保持距离 (120-200) 环绕射击
            float idealDistance = 120 + (Id * 37 % 80);

            if (dist > idealDistance + 20)
            {
                // 距离太远，靠近
                float targetDx = (dx / dist) * maxSpeed;
                float targetDy = (dy / dist) * maxSpeed;
                Dx = Dx * 0.9f + targetDx * 0.1f;
                Dy = Dy * 0.9f + targetDy * 0.1f;
            }
            else if (dist < idealDistance - 20)
            {
                // 距离太近，后退
                Dx -= (dx / dist) * 0.3f;
                Dy -= (dy / dist) * 0.3f;
            }
            else
            {
                // 在理想攻击范围内，停止移动（大幅增加摩擦力），全力攻击
                Dx *= 0.8f;
                Dy *= 0.8f;
            }

            // 发射攻击 (只要在一定范围内就可以攻击)
            if (ShootCooldown == 0 && dist <= idealDistance + 40 && Rand.Next(100) < 60)
            {
                LaunchAttackAtMonster(MonsterTarget);
            }
        }
        else if (ClassType == RobotClass.Worker)
        {
            // 采集型：远离怪物（逃跑逻辑）
            if (dist < 150)
            {
                Dx -= (dx / dist) * 0.5f;
                Dy -= (dy / dist) * 0.5f;
            }
            else
            {
                UpdateRandomMovement();
            }
        }

        // 限制最大速度
        float currentSpeed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
        if (currentSpeed > maxSpeed)
        {
            Dx = (Dx / currentSpeed) * maxSpeed;
            Dy = (Dy / currentSpeed) * maxSpeed;
        }
    }

    private void LaunchAttackAtMonster(Monster monster)
    {
        if (monster == null || !monster.IsActive || monster.IsDead || IsDead) return;

        SpecialState = "ANGRY";
        SpecialStateTimer = 100;

        var (targetX, targetY) = monster.GetCenter();
        float centerX = X + Size / 2;
        float centerY = Y + Size / 2;

        // 尝试释放大招 (提高到 25% 概率)
        if (UltimateCooldown == 0 && Rand.Next(100) < 25)
        {
            PerformUltimateAttack(centerX, centerY, targetX, targetY, monster);
            ShootCooldown = 30; // 降低大招后的硬直时间，让它能更快接下一个动作
            return;
        }

        int shooterLevel = BattleForm.Instance?._shooterLevel ?? 1;
        
        // 降低普通攻击冷却，等级越高，冷却越短
        ShootCooldown = Math.Max(15, 40 - shooterLevel * 3);

        // 引入更多攻击方式：30%概率使用激光 (等级3以上解锁)
        if (shooterLevel >= 3 && Rand.Next(100) < 30)
        {
            IsFiringLaser = true;
            LaserTargetX = targetX;
            LaserTargetY = targetY;
            _delayedAttackTimer = 25;
            return;
        }

        IsFiringLaser = false;

        // 根据射手等级决定弹药池
        List<string> ammoTypes = new List<string> { "BULLET" };
        if (shooterLevel >= 2) ammoTypes.Add("ROCKET");
        if (shooterLevel >= 4) ammoTypes.Add("PLASMA");
        if (shooterLevel >= 5) ammoTypes.Add("CANNON");
        if (shooterLevel >= 6) ammoTypes.Add("LIGHTNING");

        string type = ammoTypes[Rand.Next(ammoTypes.Count)];
        
        // 每次发射数量也随等级提升 (每2级多发一枚子弹，最多3发)
        int projectileCount = 1 + (shooterLevel - 1) / 2;
        if (projectileCount > 3) projectileCount = 3;

        for (int i = 0; i < projectileCount; i++)
        {
            // 散射角度
            float spread = (projectileCount > 1) ? (float)((i - (projectileCount - 1) / 2.0f) * 0.2f) : 0;
            
            // 简单模拟角度偏移（这只是在目标点上加上随机偏移，为了简化）
            float pTargetX = targetX + (float)((Rand.NextDouble() - 0.5) * 40);
            float pTargetY = targetY + (float)((Rand.NextDouble() - 0.5) * 40);
            
            var p = new Projectile(this, centerX, centerY, pTargetX, pTargetY, type);
            BattleForm.Instance?.AddProjectile(p);
        }
    }

    private void PerformUltimateAttack(float startX, float startY, float targetX, float targetY, object target)
    {
        UltimateCooldown = 600; // 10秒冷却
        SpecialState = "SPINNING";
        SpecialStateTimer = 180;

        string[] ultimates = { "METEOR", "BLACK_HOLE", "DEATH_RAY" };
        string ult = ultimates[Rand.Next(ultimates.Length)];

        switch (ult)
        {
            case "METEOR":
                // 陨石雨：随机发射3-5个陨石
                int count = Rand.Next(3, 6);
                for (int i = 0; i < count; i++)
                {
                    float offsetX = (float)(Rand.NextDouble() - 0.5) * 200;
                    float offsetY = (float)(Rand.NextDouble() - 0.5) * 200;
                    var p = new Projectile(this, startX, startY, targetX + offsetX, targetY + offsetY, "METEOR");
                    BattleForm.Instance?.AddProjectile(p);
                }
                break;

            case "BLACK_HOLE":
                // 黑洞：缓慢飞行的控制球
                var bh = new Projectile(this, startX, startY, targetX, targetY, "BLACK_HOLE");
                BattleForm.Instance?.AddProjectile(bh);
                break;

            case "DEATH_RAY":
                // 死光：直接连接目标的激光
                var ray = new Projectile(this, startX, startY, targetX, targetY, "DEATH_RAY");
                if (target is Robot r) ray.TrackingTarget = r;
                BattleForm.Instance?.AddProjectile(ray);
                break;
        }
    }

    /// <summary>
    /// 向指定位置发射（用于右键）
    /// </summary>
    public void LaunchRemoteAttackAtPosition(float tx, float ty)
    {
        if (IsDead) return;

        int shooterLevel = BattleForm.Instance?._shooterLevel ?? 1;
        ShootCooldown = Math.Max(20, 120 - shooterLevel * 10);
        SpecialState = "ANGRY";
        SpecialStateTimer = 100;

        float centerX = X + Size / 2;
        float centerY = Y + Size / 2;

        string type = shooterLevel >= 3 ? "ROCKET" : "BULLET";
        var p = new Projectile(this, centerX, centerY, tx, ty, type);
        BattleForm.Instance?.AddProjectile(p);
    }

    // 移除废弃的LaunchRemoteAttack

    private void UpdateDelayedAttack()
    {
        if (_delayedAttackTimer > 0)
        {
            _delayedAttackTimer--;
            if (_delayedAttackTimer == 0)
            {
                IsFiringLaser = false;

                // 处理对怪物的激光伤害
                if (MonsterTarget != null && MonsterTarget.IsActive && !MonsterTarget.IsDead)
                {
                    MonsterTarget.TakeDamage(10); // 激光对怪物造成10点伤害
                }
            }
        }
    }

    private bool UpdateDuelLogic()
    {
        // 机器人不再互殴，清空格斗目标
        DuelTarget = null;
        DuelTimer = 0;
        return false;
    }


    private void UpdateRandomMovement()
    {
        // 尝试向基地靠拢并在周围环绕
        var baseRobot = BattleForm.Instance?.GetBaseRobot();
        if (baseRobot != null && ClassType != RobotClass.Worker) // 采集工不受此限制
        {
            float dx = (baseRobot.X + baseRobot.Size / 2) - (X + Size / 2);
            float dy = (baseRobot.Y + baseRobot.Size / 2) - (Y + Size / 2);
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            // 根据不同的兵种，设定不同的理想环绕半径
            float idealRadius = 80;
            if (ClassType == RobotClass.Guardian) idealRadius = 150; // 守卫者在最外圈
            else if (ClassType == RobotClass.Shooter) idealRadius = 100 + (Id % 3) * 20; // 射手在中圈错开
            else if (ClassType == RobotClass.Healer) idealRadius = 60; // 治疗者紧贴基地

            float maxSpeed = 2.0f * SpeedMultiplier;

            if (dist > idealRadius + 20)
            {
                // 距离太远，向基地靠拢
                Dx = Dx * 0.9f + (dx / dist) * maxSpeed * 0.2f;
                Dy = Dy * 0.9f + (dy / dist) * maxSpeed * 0.2f;
            }
            else if (dist < idealRadius - 20)
            {
                // 距离太近，向外散开
                Dx -= (dx / dist) * 0.2f;
                Dy -= (dy / dist) * 0.2f;
            }
            else
            {
                // 在理想半径内，缓慢环绕或停留
                if (Rand.Next(100) < 10)
                {
                    // 产生一个切向力，使其环绕
                    float tangentDx = -dy / dist * maxSpeed * 0.5f;
                    float tangentDy = dx / dist * maxSpeed * 0.5f;
                    // 一半顺时针，一半逆时针
                    if (Id % 2 == 0)
                    {
                        tangentDx = -tangentDx;
                        tangentDy = -tangentDy;
                    }
                    Dx = Dx * 0.8f + tangentDx * 0.2f;
                    Dy = Dy * 0.8f + tangentDy * 0.2f;
                }
                else
                {
                    // 逐渐停下
                    Dx *= 0.9f;
                    Dy *= 0.9f;
                }
            }

            // 限制最大速度
            float currentSpeed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
            if (currentSpeed > maxSpeed)
            {
                Dx = (Dx / currentSpeed) * maxSpeed;
                Dy = (Dy / currentSpeed) * maxSpeed;
            }
            return;
        }

        // 如果没有基地，则退回原本的随机游走
        if (Rand.Next(100) < 5)
        {
            double angle = Rand.NextDouble() * Math.PI * 2;
            float speed = (float)Rand.NextDouble() * 2.0f;
            Dx += (float)Math.Cos(angle) * speed;
            Dy += (float)Math.Sin(angle) * speed;
        }
    }

    private void ApplyMovement(int screenWidth, int screenHeight)
    {
        if (IsDead || ClassType == RobotClass.Base) // 基地固定不动
        {
            Dx = 0; Dy = 0;
            return;
        }

        float finalSpeed = SpeedMultiplier;
        if (SlowTimer > 0) finalSpeed *= 0.4f;
        if (StunTimer > 0) finalSpeed = 0;

        // 安全检查
        if (float.IsNaN(Dx) || float.IsInfinity(Dx)) Dx = 0;
        if (float.IsNaN(Dy) || float.IsInfinity(Dy)) Dy = 0;

        // 简单的物理碰撞反弹 (基地不反弹)
        if (ClassType == RobotClass.Base)
        {
            Dx = 0;
            Dy = 0;
        }
        else
        {
            float maxVelocity = 20.0f;
            if (Dx > maxVelocity) Dx = maxVelocity;
            if (Dx < -maxVelocity) Dx = -maxVelocity;
            if (Dy > maxVelocity) Dy = maxVelocity;
            if (Dy < -maxVelocity) Dy = -maxVelocity;

            X += Dx * finalSpeed;
            Y += Dy * finalSpeed;

            Dx *= 0.98f;
            Dy *= 0.98f;

            // 边界碰撞
            if (X < 0) { X = 0; Dx = -Dx * 0.8f; }
            if (X > screenWidth - Size) { X = screenWidth - Size; Dx = -Dx * 0.8f; }
            if (Y < 0) { Y = 0; Dy = -Dy * 0.8f; }
            if (Y > screenHeight - Size) { Y = screenHeight - Size; Dy = -Dy * 0.8f; }

            FacingRight = Dx >= 0;
        }
    }

    private void HandleDeath()
    {
        IsDead = true;
        IsMoving = false;
        RotationAngle = 90f;
        Dx = 0; Dy = 0;
    }

    private void UpdateAnimations()
    {
        AnimationCounter++;
        if (AnimationCounter >= 8)
        {
            AnimationCounter = 0;
            AnimationFrame = (AnimationFrame + 1) % 4;
        }

        // 触手动画
        for (int i = 0; i < TentacleOffsets.Length; i++)
        {
            TentacleOffsets[i] += 0.2f;
        }
    }

    /// <summary>
    /// 受到攻击
    /// </summary>
    public void ApplyAttackEffect(int damage = 5)
    {
        if (IsDead) return;

        HP = Math.Max(0, HP - damage);

        if (HP <= 0)
        {
            HandleDeath();
        }

        LastDamageText = $"-{damage}";
        DamageTextTimer = 45;
        DamageFeedbackTimer = 60;

        SpecialState = "SHAKING";
        SpecialStateTimer = 60;
    }

    /// <summary>
    /// 处理投射物命中
    /// </summary>
    public void HandleProjectileHit(Projectile p)
    {
        if (!IsActive || IsDead) return;

        int baseDmg = 5;
        if (p.IsMonsterProjectile)
        {
            int wave = BattleForm.Instance?.CurrentWave ?? 1;
            if (p.Type == "INK") baseDmg = 10 + wave * 2;
            else if (p.Type == "SPIT") baseDmg = 5 + wave;
        }
        else
        {
            baseDmg = p.Owner?.GetProjectileDamage(p.Type) ?? GetProjectileDamage(p.Type);
        }

        ApplyAttackEffect(baseDmg);

        // 状态效果 (基地不吃位移效果)
        if (ClassType != RobotClass.Base)
        {
            switch (p.Type)
            {
                case "ROCKET":
                    Dx += p.Dx * 0.8f;
                    Dy += p.Dy * 0.8f;
                    SpecialState = "SPINNING";
                    SpecialStateTimer = 90;
                    break;
                case "CANNON":
                    Dx += p.Dx * 1.5f;
                    Dy += p.Dy * 1.5f;
                    SpecialState = "SHAKING";
                    SpecialStateTimer = 150;
                    break;
                case "LIGHTNING":
                    StunTimer = 90;
                    SpecialState = "SHAKING";
                    SpecialStateTimer = 90;
                    break;
                case "SPIT":
                    SlowTimer = 180;
                    break;
                case "INK":
                    // 简化：墨水只造成额外伤害
                    ApplyAttackEffect(3);
                    break;
                case "PLASMA":
                    SpecialState = "SPINNING";
                    SpecialStateTimer = 120;
                    break;
                default:
                    Dx += p.Dx * 0.2f;
                    Dy += p.Dy * 0.2f;
                    break;
            }
        }
        else
        {
            // 基地也会受额外伤害，但不产生位移
            if (p.Type == "INK") ApplyAttackEffect(3);
        }

        // 追踪攻击者 (基地不追踪)
        if (p.Owner != null && p.Owner != this && ClassType != RobotClass.Base)
        {
            ChasingTarget = p.Owner;
            ChaseTimer = 400;
        }
    }

    public int GetProjectileDamage(string type)
    {
        int baseDamage = type switch
        {
            "ROCKET" => 15,
            "PLASMA" => 20,
            "CANNON" => 25,
            "LIGHTNING" => 10,
            "SPIT" => 5,
            "INK" => 8,
            "METEOR" => 60,
            "BLACK_HOLE" => 2,
            "DEATH_RAY" => 5,
            _ => 5
        };

        // 获取对应的等级加成
        int levelBonus = 0;
        if (BattleForm.Instance != null)
        {
            levelBonus = ClassType switch
            {
                RobotClass.Shooter => BattleForm.Instance._shooterLevel - 1,
                RobotClass.Worker => BattleForm.Instance._workerLevel - 1,
                RobotClass.Healer => BattleForm.Instance._healerLevel - 1,
                RobotClass.Guardian => BattleForm.Instance._guardianLevel - 1,
                _ => 0
            };
        }

        // 基础伤害 + 等级加成(每级+20%)
        return (int)(baseDamage * (1 + levelBonus * 0.2f));
    }

    /// <summary>
    /// 设置怪物目标 - 机器人会集火攻击怪物
    /// </summary>
    public void SetMonsterTarget(Monster? monster)
    {
        MonsterTarget = monster;
        if (monster != null)
        {
            ChaseTimer = 0;
            ChasingTarget = null;
            DuelTarget = null;
            DuelTimer = 0;
        }
    }

    /// <summary>
    /// 碰撞检测
    /// </summary>
    public bool HitTest(int mx, int my)
    {
        return mx >= X && mx <= X + Size &&
               my >= Y && my <= Y + Size;
    }
}

// 简化版随机扩展
public static class RandomExtensions
{
    public static float NextFloat(this Random rand)
    {
        return (float)rand.NextDouble();
    }
}
