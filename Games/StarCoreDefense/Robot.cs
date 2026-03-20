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
    Worker,     // 采集/搜索型 (高机动)
    Healer,     // 治疗/辅助型
    Shooter,    // 攻击/远程型
    Guardian,   // 守卫者 (近战)
    Engineer    // 工程兵 (防御工事维护)
}

public enum BaseModule
{
    None,
    Bastion,    // 堡垒模式：极大血量 + 减速场
    Industrial  // 工业模式：极速采集 + 购买折扣
}

public enum RobotRank
{
    Normal,
    Mega,
    Ultra
}

public partial class Robot
{
    // 采集与建造
    public Mineral? TargetMineral { get; set; }
    public WallSegment? TargetWall { get; set; }
    private int _miningTimer = 0;
    private int _buildingTimer = 0;

    // 治疗技能
    public List<Robot> HealingTargets { get; } = new List<Robot>();
    private int _healCooldown = 0;

    // 兵种类型
    public RobotClass ClassType { get; set; } = RobotClass.Shooter;
    public RobotRank Rank { get; set; } = RobotRank.Normal;

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
    public int Damage { get; set; } = 100; // Added for Rank upgrade
    public int AttackInterval { get; set; } = 60; // Added for Rank upgrade

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
    private int _targetUpdateCooldown = 0;

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

    public Robot(int id, string name, float x, float y, RobotClass classType = RobotClass.Shooter, RobotRank rank = RobotRank.Normal)
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;
        ClassType = classType;
        Rank = rank;

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
            case RobotClass.Engineer:
                PrimaryColor = Color.FromArgb(0, 102, 204); // 藏蓝色
                SecondaryColor = Color.FromArgb(0, 76, 153);
                EyeColor = Color.LightSkyBlue;
                Size = 20;
                SpeedMultiplier = 1.3f;
                MaxHP = 800;
                HP = MaxHP;
                break;
        }

        // 基地特殊属性，根据等级动态调整
        if (ClassType == RobotClass.Base)
        {
            int baseLevel = BattleForm.Instance?._baseLevel ?? 1;
            MaxHP = 3000 + (baseLevel - 1) * 5000; // 基地血量随等级大幅提升
            Size = 60 + (baseLevel - 1) * 5;
        }
        
        // --- Rank 强化升级 ---
        if (Rank == RobotRank.Mega)
        {
            Size = (int)(Size * 1.8f);
            MaxHP *= 6;
            Damage *= 4;
            AttackInterval = (int)(AttackInterval * 0.7f);
        }
        else if (Rank == RobotRank.Ultra)
        {
            Size = (int)(Size * 2.8f);
            MaxHP *= 30;
            Damage *= 15;
            AttackInterval = (int)(AttackInterval * 0.5f);
        }
        
        HP = MaxHP; // 升级或重置时满血

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
                SpeedMultiplier = 1.2f + 0.1f * (BattleForm.Instance?._workerLevel - 1 ?? 0); // 等级越高移速越快
                MaxHP = (int)(600 * (1 + 0.2f * (BattleForm.Instance?._workerLevel - 1 ?? 0))); // 根据等级提升血量
                HP = MaxHP;
                break;
            case RobotClass.Healer:
                PrimaryColor = Color.FromArgb(116, 185, 255); // 蓝色
                SecondaryColor = Color.FromArgb(9, 132, 227);
                EyeColor = Color.White;
                Size = 25;
                SpeedMultiplier = 0.9f + 0.05f * (BattleForm.Instance?._healerLevel - 1 ?? 0);
                MaxHP = (int)(1200 * (1 + 0.2f * (BattleForm.Instance?._healerLevel - 1 ?? 0)));
                HP = MaxHP;
                break;
            case RobotClass.Shooter:
                PrimaryColor = Color.FromArgb(255, 107, 107); // 红色
                SecondaryColor = Color.FromArgb(255, 77, 77);
                EyeColor = Color.Cyan;
                Size = 23;
                SpeedMultiplier = 1.0f + 0.05f * (BattleForm.Instance?._shooterLevel - 1 ?? 0);
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
            case RobotClass.Engineer:
                PrimaryColor = Color.FromArgb(0, 102, 204); // 藏蓝色
                SecondaryColor = Color.FromArgb(0, 76, 153);
                EyeColor = Color.LightSkyBlue;
                Size = 20;
                SpeedMultiplier = 1.3f;
                MaxHP = (int)(800 * (1 + 0.2f * (BattleForm.Instance?._engineerLevel - 1 ?? 0)));
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
        if (_targetUpdateCooldown > 0) _targetUpdateCooldown--; // Decrement cooldown

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

        // 2. 低血量撤退逻辑 (HP < 20% 撤退, > 50% 恢复进攻)
        bool isRetreating = HP < MaxHP * 0.2f;
        bool hasRecovered = HP >= MaxHP * 0.5f;
        
        if (isRetreating && ClassType != RobotClass.Base && ClassType != RobotClass.Guardian)
        {
            // 清除进攻目标，向基地方向逃跑
            MonsterTarget = null;
            var baseBot = BattleForm.Instance?.GetBaseRobot();
            if (baseBot != null)
            {
                float dx = (baseBot.X + baseBot.Size / 2) - (X + Size / 2);
                float dy = (baseBot.Y + baseBot.Size / 2) - (Y + Size / 2);
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist > 60)
                {
                    Dx += (dx / dist) * 2.0f;
                    Dy += (dy / dist) * 2.0f;
                }
            }
            // 不执行后续进攻，直接跳到移动
            ApplyMovement(screenWidth, screenHeight);
            if (HP <= 0 && !IsDead) HandleDeath();
            UpdateAnimations();
            return;
        }

        // 3. 检查当前目标是否还在 Aggro 范围内（每帧检查，超出则放弃追击）
        if (MonsterTarget != null && MonsterTarget.IsActive && !MonsterTarget.IsDead)
        {
            float aggroRange = ClassType switch
            {
                RobotClass.Guardian => 350f,
                RobotClass.Healer   => 250f,
                _                   => 400f,
            };
            // 还在范围内：保持目标；超出范围（多给50单位缓冲防止频繁切换）→ 放弃
            float dxCheck = MonsterTarget.X - (X + Size / 2);
            float dyCheck = MonsterTarget.Y - (Y + Size / 2);
            float distCheck = (float)Math.Sqrt(dxCheck * dxCheck + dyCheck * dyCheck);
            if (distCheck > aggroRange + 50f)
            {
                MonsterTarget = null; // 脱离范围，回巡逻
            }
        }

        // 没有目标时，寻找 Aggro 范围内的新目标
        if (ChasingTarget == null && DuelTarget == null && MonsterTarget == null && TargetMineral == null && TargetWall == null)
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
        else if (ClassType == RobotClass.Engineer)
        {
            UpdateEngineerLogic();
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
        else
        {
            UpdateRandomMovement();
        }

        // 4. 机器人间排斥力：战斗中减弱，防止振荡；空闲时全力分散
        bool isFighting = MonsterTarget != null;
        foreach (var other in allRobots)
        {
            if (other == this || !other.IsActive || other.IsDead || other.ClassType == RobotClass.Base) continue;
            float dxSep = X - other.X;
            float dySep = Y - other.Y;
            float distSq = dxSep * dxSep + dySep * dySep;
            // 战斗中只防止真正重叠（50%半径），空闲时扩展分散半径
            float safeRadius = isFighting
                ? (Size + other.Size) * 0.5f   // 战斗中只防重叠
                : (Size + other.Size) * 1.2f;  // 空闲时拉大距离分散
            if (distSq < safeRadius * safeRadius && distSq > 0.01f)
            {
                float dist = (float)Math.Sqrt(distSq);
                float forceMag = isFighting ? 0.1f : 0.4f; // 战斗时弱推力
                float force = (safeRadius - dist) / safeRadius * forceMag;
                Dx += (dxSep / dist) * force;
                Dy += (dySep / dist) * force;
            }
        }

        // 5. 辅助逻辑
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
    /// 自动寻找并分配目标（Aggro Range 版本）
    /// 只对进入检测半径的怪物发动攻击，超出范围的怪物不追
    /// </summary>
    private void FindAndAssignTarget(List<Robot> allRobots, List<Monster> allMonsters)
    {
        if (_targetUpdateCooldown > 0) return;
        _targetUpdateCooldown = 15 + new Random().Next(15); // 随机错开更新频率
        // 1. 采集型机器人：全屏超速搜索逻辑
        if (ClassType == RobotClass.Worker)
        {
            var minerals = BattleForm.Instance?.GetMinerals();
            if (minerals != null)
            {
                Mineral? bestMineral = null;
                float bestMineralScore = float.MaxValue;
                
                // 采集工总数
                int totalWorkers = allRobots.Count(r => r.ClassType == RobotClass.Worker && r.IsActive && !r.IsDead);

                foreach (var m in minerals)
                {
                    if (!m.IsActive) continue;
                    
                    // 核心抢占锁逻辑：严格 1对1，如果被别人锁了就不去，保证不扎堆
                    if (m.LockingRobot != null && m.LockingRobot != this)
                        continue;

                    float dxM = m.X - X;
                    float dyM = m.Y - Y;
                    float distSq = dxM * dxM + dyM * dyM;
                    
                    float mineralScore = distSq;
                    
                    if (mineralScore < bestMineralScore)
                    {
                        bestMineralScore = mineralScore;
                        bestMineral = m;
                    }
                }
                
                if (bestMineral != null)
                {
                    // 先释放旧锁，再锁定新目标
                    if (TargetMineral != null && TargetMineral.LockingRobot == this) TargetMineral.LockingRobot = null;
                    TargetMineral = bestMineral;
                    TargetMineral.LockingRobot = this; 
                    return;
                }
            }
            return;
        }

        // 2. 工程兵：寻找损坏的围墙
        if (ClassType == RobotClass.Engineer)
        {
            TargetWall = BattleForm.Instance?.GetWeakestWall();
            return;
        }

        // ── Aggro Range 核心逻辑 ──────────────────────────────────
        // 每种兵种有自己的仇恨半径，超出范围的怪物完全忽视
        float aggroRange = ClassType switch
        {
            RobotClass.Guardian => 350f,  // 守卫者近身防御
            RobotClass.Healer   => 250f,  // 治疗者近距支援
            _                   => 400f,  // 射手默认中远程侦测
        };

        Monster? targetMonster = null;
        float bestScore = float.MaxValue;

        foreach (var monster in allMonsters)
        {
            if (!monster.IsActive || monster.IsDead) continue;

            float dx = monster.X - (X + Size / 2);
            float dy = monster.Y - (Y + Size / 2);
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            // 超出 Aggro Range → 完全忽视，不追
            if (dist > aggroRange) continue;

            // 范围内：综合评分（距离 + 已被攻击人数惩罚）
            float attackerPenalty = monster.AttackerCount * 300f;
            float score = dist + attackerPenalty;

            if (score < bestScore)
            {
                bestScore = score;
                targetMonster = monster;
            }
        }

        if (targetMonster != null)
        {
            MonsterTarget = targetMonster;
            return;
        }

        // 范围内没有怪物 → 清除目标，回到巡逻（UpdateRandomMovement）
        MonsterTarget = null;
        ChasingTarget = null;
        DuelTarget = null;
        TargetMineral = null;
        TargetWall = null; // Clear wall target too
    }

    private void UpdateWorkerLogic()
    {
        if (TargetMineral != null && !TargetMineral.IsActive) TargetMineral = null;

        if (TargetMineral == null)
        {
            UpdateRandomMovement();
            return;
        }

        float dx = TargetMineral.X - X;
        float dy = TargetMineral.Y - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        // 采集工速度随等级极快增长：基础 6.0 + (等级 * 1.5)
        float maxSpeed = (6.0f + (BattleForm.Instance?._workerLevel ?? 1) * 1.5f) * SpeedMultiplier;

        if (dist > 15)
        {
            Dx = Dx * 0.8f + (dx / dist) * maxSpeed * 0.2f;
            Dy = Dy * 0.8f + (dy / dist) * maxSpeed * 0.2f;
            _miningTimer = 0;
        }
        else
        {
            Dx *= 0.3f;
            Dy *= 0.3f;
            _miningTimer++;
            if (_miningTimer >= 40) // 采集速度也变快了
            {
                _miningTimer = 0;
                // 这里加星矿（钻石）而不是钱
                if (BattleForm.Instance != null)
                {
                    // 采矿量随采集工等级提升（Lv.1: 15, Lv.2: 20, Lv.3: 25...）
                    int workerLv = BattleForm.Instance._workerLevel;
                    int mineralYield = 10 + workerLv * 5;
                    BattleForm.Instance.Minerals += mineralYield;
                    BattleForm.Instance.AddFloatingText(X, Y - 20, $"+{mineralYield} 💎", Color.Cyan);
                    
                    // 彻底移除该晶体，确保“采完了”
                    TargetMineral.LockingRobot = null; // 采完释放
                    TargetMineral.IsActive = false;
                    BattleForm.Instance.RemoveMineral(TargetMineral);
                }
                // 采集完后清除目标重新通过 FindAndAssignTarget 全局检索
                TargetMineral = null;
            }
        }
    }

    private void UpdateEngineerLogic()
    {
        if (TargetWall == null || !TargetWall.IsActive)
        {
            TargetWall = BattleForm.Instance?.GetWeakestWall();
        }

        if (TargetWall == null)
        {
            UpdateRandomMovement();
            return;
        }

        var wp = TargetWall.GetWorldPosition(BattleForm.Instance!.GetBaseRobot()?.X ?? 0, BattleForm.Instance!.GetBaseRobot()?.Y ?? 0);
        float dx = wp.X - X;
        float dy = wp.Y - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist > 25)
        {
            float maxSpeed = 5.0f * SpeedMultiplier;
            Dx = Dx * 0.9f + (dx / dist) * maxSpeed * 0.1f;
            Dy = Dy * 0.9f + (dy / dist) * maxSpeed * 0.1f;
            _buildingTimer = 0;
        }
        else
        {
            Dx *= 0.5f;
            Dy *= 0.5f;
            _buildingTimer++;
            TargetWall.Repair(2); // 工程兵修理效率更高
            if (_buildingTimer % 15 == 0)
            {
                BattleForm.Instance?.AddExplosion(X + Size / 2, Y + Size / 2, Color.DeepSkyBlue, 2, "SPARK");
            }
            if (TargetWall.HP >= TargetWall.MaxHP) TargetWall = null;
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

        // 寻找需要治疗的队友，优先选择血量百分比最低的（伤势最重的）
        // 当基地血量百分比低于70%时，才将基地提至最高优先级
        var baseRobot2 = allRobots.FirstOrDefault(r => r.ClassType == RobotClass.Base);
        bool baseInDanger = baseRobot2 != null && (float)baseRobot2.HP / baseRobot2.MaxHP < 0.70f;
        var targets = allRobots.Where(r => r != this && r.IsActive && !r.IsDead && r.HP < r.MaxHP && r.ClassType != RobotClass.Worker)
                               .OrderBy(r => baseInDanger && r.ClassType == RobotClass.Base ? 0 : 1) // 仅基地危机时优先基地
                               .ThenBy(r => (float)r.HP / r.MaxHP) // 按血量百分比排序，最危险的先治
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
                // 距离太远，全速靠近
                Dx = (dx / dist) * maxSpeed;
                Dy = (dy / dist) * maxSpeed;
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
        
        // 降低普通攻击冷却，等级越高，射速越快 (Lv.1: 37, Lv.4: 28, Lv.8: 16)
        ShootCooldown = Math.Max(12, 40 - shooterLevel * 3);

        // 引入更多攻击方式：激光判定及高频率射击
        if (shooterLevel >= 3 && Rand.Next(100) < 35)
        {
            IsFiringLaser = true;
            LaserTargetX = targetX;
            LaserTargetY = targetY;
            _delayedAttackTimer = 25; 
            return;
        }

        IsFiringLaser = false;

        // 根据等级选择最强大的武器类型 (100% 概率使用当前解锁的最高级武器)
        string type = "BULLET";
        if (shooterLevel >= 10) type = "METEOR";
        else if (shooterLevel >= 8) type = "LIGHTNING";
        else if (shooterLevel >= 6) type = "CANNON";
        else if (shooterLevel >= 4) type = "PLASMA";
        else if (shooterLevel >= 2) type = "ROCKET";
        
        // 连发机制：每3级多发一枚子弹，最多5发
        int projectileCount = 1 + (shooterLevel - 1) / 3;
        if (projectileCount > 5) projectileCount = 5;

        for (int i = 0; i < projectileCount; i++)
        {
            // 散射与偏移
            float pTargetX = targetX + (float)((Rand.NextDouble() - 0.5) * 60);
            float pTargetY = targetY + (float)((Rand.NextDouble() - 0.5) * 60);
            
            var p = new Projectile(this, centerX, centerY, pTargetX, pTargetY, type);
            // 追踪能力：Lv.5 以上自动追踪
            if (shooterLevel >= 5) p.TrackingMonster = monster;
            
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
                else if (target is Monster m) ray.TrackingMonster = m;
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

                // 处理对怪物的激光伤害 - 伤害随射手等级增加
                if (MonsterTarget != null && MonsterTarget.IsActive && !MonsterTarget.IsDead)
                {
                    int shooterLevel = BattleForm.Instance?._shooterLevel ?? 1;
                    int laserDmg = 15 + (shooterLevel - 1) * 8;
                    MonsterTarget.TakeDamage(laserDmg); 
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
            float idealRadius = 70;
            if (ClassType == RobotClass.Guardian) idealRadius = 120;
            else if (ClassType == RobotClass.Shooter) idealRadius = 80 + (Id % 4) * 15; // 射手在中圈错开
            else if (ClassType == RobotClass.Healer) idealRadius = 50; // 治疗者紧贴基地

            float maxSpeed = 2.5f * SpeedMultiplier;

            if (dist > idealRadius + 15)
            {
                // 距离太远，全速回归基地
                Dx = (dx / dist) * maxSpeed;
                Dy = (dy / dist) * maxSpeed;
            }
            else if (dist < idealRadius - 15)
            {
                // 距离太近，向外散开
                Dx -= (dx / dist) * 0.4f;
                Dy -= (dy / dist) * 0.4f;
            }
            else
            {
                // 在理想半径内，持续环绕（Id奇偶决定顺逆时针）
                float tangentDx = -(dy / dist) * maxSpeed * 0.6f;
                float tangentDy = (dx / dist) * maxSpeed * 0.6f;
                if (Id % 2 == 0) { tangentDx = -tangentDx; tangentDy = -tangentDy; }
                Dx = Dx * 0.85f + tangentDx * 0.15f;
                Dy = Dy * 0.85f + tangentDy * 0.15f;
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

            FacingRight = Dx >= 0;
        }
    }

    private void HandleDeath()
    {
        if (TargetMineral != null && TargetMineral.LockingRobot == this)
        {
            TargetMineral.LockingRobot = null;
        }
        TargetMineral = null;
        TargetWall = null;

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
        
        // 采集工人不具有血量概念，无视所有伤害，避免占用治疗资源
        if (ClassType == RobotClass.Worker) return;

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
                RobotClass.Engineer => BattleForm.Instance._engineerLevel - 1,
                _ => 0
            };
        }

        // 基础伤害 + 等级提升 (等级3以上每级+25%，Lv.1为100%伤害)
        float factor = 1.0f + levelBonus * 0.25f;
        
        // 针对不同类型的子弹微调其成长曲线 (如有需要可以在此扩展)
        return (int)(baseDamage * factor);
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
