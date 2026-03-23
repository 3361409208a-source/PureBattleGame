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
    Guardian,   // 守卫者 (近战)
    Engineer,   // 工程兵 (防御工事维护)
    Gunner,     // 基础机枪手 (平衡型)
    Rocket,     // 火箭兵 (范围大伤害)
    Plasma,     // 等离子兵 (高频连发)
    Laser,      // 激光狙击 (高精打击)
    Lightning   // 闪电特攻 (追踪/特殊)
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
    
    // 治疗技能
    public List<Robot> HealingTargets { get; } = new List<Robot>();
    private int _healCooldown = 0;

    // 兵种类型
    public RobotClass ClassType { get; set; } = RobotClass.Gunner;
    public RobotRank Rank { get; set; } = RobotRank.Normal;
    public int Level { get; set; } = 1; // 兵种当前等级

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
    public int SpeedBoostTimer { get; set; } = 0; // 快速换防/冲刺计时器
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
    public bool IsFiringLightning { get; set; } = false; // 新增：持续电击状态
    public List<Monster> LightningTargets { get; } = new List<Monster>(); // 新增：当前的电击目标链
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

    // 驻扎锁定 (防卫点)
    public WallSegment? AssignedWall { get; set; }

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
    private int _frameCount = 0;
    private int _delayedAttackTimer = 0;

    // 随机
    public Random Rand { get; set; } = new Random();

    // 触摸/漂浮效果
    public float ShakingOffset { get; set; } = 0;
    public float RotationAngle { get; set; } = 0;
    public float OrbitAngle { get; set; } = 0; // 守卫者公转角度

    public Robot(int id, string name, float x, float y, RobotClass classType = RobotClass.Gunner, RobotRank rank = RobotRank.Normal)
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
            case RobotClass.Gunner:
                PrimaryColor = Color.FromArgb(255, 107, 107);
                SecondaryColor = Color.FromArgb(255, 77, 77);
                EyeColor = Color.Cyan;
                Size = 23;
                SpeedMultiplier = 1.0f;
                MaxHP = 1000;
                HP = MaxHP;
                CurrentAttackType = "BULLET";
                break;
            case RobotClass.Rocket:
                PrimaryColor = Color.FromArgb(255, 165, 0); // 橙色
                SecondaryColor = Color.FromArgb(200, 100, 0);
                EyeColor = Color.Yellow;
                Size = 26;
                SpeedMultiplier = 0.85f;
                MaxHP = 1200;
                HP = MaxHP;
                CurrentAttackType = "ROCKET";
                break;
            case RobotClass.Plasma:
                PrimaryColor = Color.FromArgb(170, 100, 255); // 紫色
                SecondaryColor = Color.FromArgb(100, 50, 200);
                EyeColor = Color.Magenta;
                Size = 21;
                SpeedMultiplier = 1.1f;
                MaxHP = 800;
                HP = MaxHP;
                CurrentAttackType = "PLASMA";
                break;
            case RobotClass.Laser:
                PrimaryColor = Color.FromArgb(0, 255, 255); // 青蓝色
                SecondaryColor = Color.FromArgb(0, 180, 200);
                EyeColor = Color.White;
                Size = 22;
                SpeedMultiplier = 0.95f;
                MaxHP = 900;
                HP = MaxHP;
                CurrentAttackType = "LASER";
                break;
            case RobotClass.Lightning:
                PrimaryColor = Color.FromArgb(255, 255, 100); // 亮黄色
                SecondaryColor = Color.FromArgb(200, 200, 0);
                EyeColor = Color.DeepSkyBlue;
                Size = 22;
                SpeedMultiplier = 1.2f;
                MaxHP = 1100;
                HP = MaxHP;
                CurrentAttackType = "LIGHTNING";
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
            case RobotClass.Gunner:
                Level = BattleForm.Instance?._shooterLevel ?? 1;
                int lvG = Level; 
                PrimaryColor = Color.FromArgb(255, 107, 107);
                Size = 23;
                MaxHP = (int)(1000 * (1 + 0.2f * (lvG - 1)));
                Damage = (110 + (lvG - 1) * 35) * 11; // 【伤害激增 1000%】
                AttackInterval = Math.Max(1, (12 - (lvG - 1) / 2) / 11); // 【射速提升 1000%】
                CurrentAttackType = "BULLET";
                HP = MaxHP;
                break;
            case RobotClass.Rocket:
                Level = BattleForm.Instance?._rocketLevel ?? 1;
                int lvR = Level;
                PrimaryColor = Color.FromArgb(255, 165, 0);
                Size = 26;
                MaxHP = (int)(1200 * (1 + 0.2f * (lvR - 1)));
                Damage = (250 + (lvR - 1) * 80) * 11; // 【伤害激增 1000%】
                AttackInterval = Math.Max(1, (60 - (lvR - 1) * 4) / 11); // 【射速提升 1000%】
                CurrentAttackType = "ROCKET";
                HP = MaxHP;
                break;
            case RobotClass.Plasma:
                Level = BattleForm.Instance?._plasmaLevel ?? 1;
                int lvP = Level;
                PrimaryColor = Color.FromArgb(170, 100, 255);
                Size = 21;
                MaxHP = (int)(800 * (1 + 0.15f * (lvP - 1)));
                Damage = (45 + (lvP - 1) * 15) * 11; // 【伤害激增 1000%】
                AttackInterval = Math.Max(1, (6 - (lvP - 1) / 3) / 11); // 【射速提升 1000%】
                CurrentAttackType = "PLASMA";
                HP = MaxHP;
                break;
            case RobotClass.Laser:
                Level = BattleForm.Instance?._laserLevel ?? 1;
                int lvL = Level;
                PrimaryColor = Color.FromArgb(0, 255, 255);
                Size = 22;
                MaxHP = (int)(900 * (1 + 0.18f * (lvL - 1)));
                Damage = (150 + (lvL - 1) * 50) * 3 * 11; // 【伤害激增 1000%】
                AttackInterval = Math.Max(1, (45 - (lvL - 1) * 2) / 11); // 【射速提升 1000%】
                CurrentAttackType = "LASER";
                HP = MaxHP;
                break;
            case RobotClass.Lightning:
                Level = BattleForm.Instance?._lightningLevel ?? 1;
                int lvLt = Level;
                PrimaryColor = Color.FromArgb(255, 255, 100);
                Size = 22;
                MaxHP = (int)(1100 * (1 + 0.22f * (lvLt - 1)));
                Damage = (120 + (lvLt - 1) * 40) * 11; // 【伤害激增 1000%】
                AttackInterval = Math.Max(1, (30 - (lvLt - 1) * 2) / 11); // 【射速提升 1000%】
                CurrentAttackType = "LIGHTNING";
                HP = MaxHP;
                break;
            case RobotClass.Guardian:
                int gLevel = BattleForm.Instance?._guardianLevel ?? 1;
                PrimaryColor = Color.FromArgb(128, 128, 128); 
                Size = 34;
                SpeedMultiplier = 1.0f + 0.15f * (gLevel - 1); 
                MaxHP = (int)(2500 * (1 + 0.25f * (gLevel - 1))); 
                Damage = (45 + (gLevel - 1) * 25) * 11; // 【伤害激增 1000%】
                HP = MaxHP;
                break;
            case RobotClass.Engineer:
                int lvE = BattleForm.Instance?._engineerLevel ?? 1;
                PrimaryColor = Color.FromArgb(0, 102, 204); 
                Size = 20;
                MaxHP = (int)(800 * (1 + 0.2f * (lvE - 1)));
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

        // 动态速度限制：支持换防冲刺
        float currentSpeed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
        float maxCap = 5.0f * (SpeedBoostTimer > 0 ? 3.0f : 1.0f);
        if (currentSpeed > maxCap)
        {
            Dx = (Dx / currentSpeed) * maxCap;
            Dy = (Dy / currentSpeed) * maxCap;
        }
    }


    /// <summary>
    /// 游戏循环更新 - 全自动战斗
    /// </summary>
    public void Update(int screenWidth, int screenHeight, List<Robot> allRobots, List<Monster> allMonsters)
    {
        _frameCount++;
        if (SpeedBoostTimer > 0) SpeedBoostTimer--;
        
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
            UpdateGuardianLogic(allRobots, allMonsters);
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
        _targetUpdateCooldown = 15 + new Random().Next(15); 
        
        // 【割草改动】采集型机器人：彻底放弃乱跑找矿的设定。现在它们是"主基地的挂机印钞机"！
        if (ClassType == RobotClass.Worker)
        {
            TargetMineral = null;
            return; // 采集工无需在此寻找任何怪/矿，不瞎跑！
        }

        // 2. 工程兵：寻找损坏的围墙
        if (ClassType == RobotClass.Engineer)
        {
            TargetWall = BattleForm.Instance?.GetWeakestWall(this);
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
        // --- 核心优化：采集兵形态进化 ---
        // 不再只在基地中心"坐牢"，而是模拟在整个防御圈内"勤劳工作"
        if (ChasingTarget == null && TargetMineral == null)
        {
            // 如果没事干，5%概率随机找个远点去"巡逻采矿"
            if (Rand.Next(100) < 5)
            {
                float angle = (float)(Rand.NextDouble() * Math.PI * 2);
                float dist = 200f + (float)Rand.NextDouble() * 800f; // 在200-1000范围内随机跑
                ChasingTarget = new Robot(-1, "patrol_point",
                    BattleForm.Instance.ClientSize.Width/2 + (float)Math.Cos(angle) * dist,
                    BattleForm.Instance.ClientSize.Height/2 + (float)Math.Sin(angle) * dist,
                    RobotClass.Worker)
                {
                    IsActive = false // 标记为虚拟目标点
                };
                ChaseTimer = 200;
            }
        }

        // 基础移动逻辑：绕基地大圆周运动 + 随机小偏移
        var baseBot = BattleForm.Instance?.GetBaseRobot();
        if (baseBot != null)
        {
            // 给每个采集工一个独特的"工位"偏移，避免聚成一坨
            float wavePhase = (float)Math.Sin(BattleForm.Instance.CurrentWave * 0.5f + Id);
            float targetRadius = 150f + (Id % 15) * 40f + wavePhase * 30f; 
            
            float dx = (baseBot.X + baseBot.Size / 2) - (X + Size / 2);
            float dy = (baseBot.Y + baseBot.Size / 2) - (Y + Size / 2);
            float dist = Math.Max(1, (float)Math.Sqrt(dx * dx + dy * dy));

            float maxSpeed = 4.0f * SpeedMultiplier;

            // 保持在宽阔的"工位"环带内
            if (dist > targetRadius + 40) { Dx += (dx / dist) * 0.4f; Dy += (dy / dist) * 0.4f; }
            else if (dist < targetRadius - 40) { Dx -= (dx / dist) * 0.4f; Dy -= (dy / dist) * 0.4f; }
            else 
            { 
                // 在环带内顺时针/逆时针穿梭，模拟扫射资源
                float tangentDx = -(dy / dist) * maxSpeed * 0.7f;
                float tangentDy =  (dx / dist) * maxSpeed * 0.7f;
                if (Id % 3 == 0) { tangentDx = -tangentDx; tangentDy = -tangentDy; } 
                
                // 加入一点随机波动，看起来像在扫描地面
                tangentDx += (float)(Rand.NextDouble() - 0.5) * 2f;
                tangentDy += (float)(Rand.NextDouble() - 0.5) * 2f;

                Dx = Dx * 0.92f + tangentDx * 0.08f;
                Dy = Dy * 0.92f + tangentDy * 0.08f;
            }
            
            // 生产逻辑（保持高速印钞）
            _miningTimer++;
            int workerLevel = BattleForm.Instance?._workerLevel ?? 1;
            int interval = Math.Max(2, 35 - workerLevel * 4); 

            if (_miningTimer >= interval)
            {
                _miningTimer = 0;
                if (BattleForm.Instance != null)
                {
                    int goldYield = 8 + workerLevel * 3; // 稍微调低基础产出
                    int mineralYield = (Rand.Next(100) < (3 + workerLevel)) ? 1 : 0; 
                    
                    BattleForm.Instance.Gold += goldYield;
                    BattleForm.Instance.Minerals += mineralYield;
                    
                    if (Rand.Next(100) < 3) // 极低概率喷个火花
                        BattleForm.Instance.AddExplosion(X + Size / 2, Y + Size / 2, Color.Gold, 1, "SPARK");
                }
            }
        }
    }

    // 【改版工程兵】：绝对安全地挂机超频修墙，甚至还能发死亡射线！
    private List<WallSegment> _activeRepairTargets = new List<WallSegment>();

    private void UpdateEngineerLogic()
    {
        // 1. 绝对安全的定步巡航（在内圈绕转）
        var baseBot = BattleForm.Instance?.GetBaseRobot();
        if (baseBot != null)
        {
            float targetRadius = 70f + (Id % 3) * 15f; 
            float bx = baseBot.X + baseBot.Size / 2;
            float by = baseBot.Y + baseBot.Size / 2;
            float dx = bx - (X + Size / 2);
            float dy = by - (Y + Size / 2);
            float dist = Math.Max(1, (float)Math.Sqrt(dx * dx + dy * dy));

            float maxSpeed = 3.5f * SpeedMultiplier;
            if (dist > targetRadius + 10) { Dx += (dx / dist) * 0.3f; Dy += (dy / dist) * 0.3f; }
            else if (dist < targetRadius - 10) { Dx -= (dx / dist) * 0.3f; Dy -= (dy / dist) * 0.3f; }
            else 
            { 
                float tangentDx = -(dy / dist) * maxSpeed * 0.5f;
                float tangentDy =  (dx / dist) * maxSpeed * 0.5f;
                Dx = Dx * 0.9f + tangentDx * 0.1f;
                Dy = Dy * 0.9f + tangentDy * 0.1f;
            }
            Dx *= 0.9f; Dy *= 0.9f;
        }

        int engineerLevel = BattleForm.Instance?._engineerLevel ?? 1;

        // 2. 智能化分散修复 (由于是"赛博工程"，多工程兵会自动分工)
        if (BattleForm.Instance != null && BattleForm.Instance._walls != null)
        {
            // 获取所有受损的墙体
            var allDamaged = BattleForm.Instance._walls.Where(w => w.HP < w.MaxHP).ToList();
            
            if (allDamaged.Count > 0)
            {
                // 每个工程兵根据 ID 挑选不同的起始维修点进行分工，避免“集火”同一个
                int baseRepair = 1 + (engineerLevel / 3); 
                int myJobCount = 1 + (engineerLevel / 4); // 随等级提高同时兼顾的任务数
                
                for (int i = 0; i < myJobCount; i++)
                {
                    int targetIdx = (Id + i * 7) % allDamaged.Count; 
                    var w = allDamaged[targetIdx];
                    w.Repair(baseRepair);
                    
                    // 偶尔冒个烟火特效
                    if (Rand.Next(100) < 5)
                    {
                        var wp = w.GetWorldPosition(baseBot.X + baseBot.Size/2, baseBot.Y + baseBot.Size/2);
                        BattleForm.Instance.AddExplosion(wp.X, wp.Y, Color.LightSkyBlue, 1, "SPARK");
                    }
                }
            }
        }
    }

    private void UpdateSafetyRetreat()
    {
        var baseBot = BattleForm.Instance?.GetBaseRobot();
        if (baseBot == null) return;

        float bx = baseBot.X + baseRobotCenterOffset;
        float by = baseBot.Y + baseRobotCenterOffset;
        float dx = bx - X, dy = by - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist > 50)
        {
            float maxSpeed = 5.0f * SpeedMultiplier;
            Dx = Dx * 0.9f + (dx / dist) * maxSpeed * 0.1f;
            Dy = Dy * 0.9f + (dy / dist) * maxSpeed * 0.1f;
            IsMoving = true;
        }
        else
        {
            Dx *= 0.5f;
            Dy *= 0.5f;
            IsMoving = false;
        }
    }

    private float baseRobotCenterOffset => BattleForm.Instance?.GetBaseRobot()?.Size / 2 ?? 20;

    public void UnlockTargets()
    {
        if (TargetMineral != null && TargetMineral.LockingRobot == this) TargetMineral.LockingRobot = null;
        if (TargetWall != null && TargetWall.LockingRobot == this) TargetWall.LockingRobot = null;
        
        // 关键：释放驻扎位锁定
        if (AssignedWall != null && AssignedWall.GarrisonRobot == this) AssignedWall.GarrisonRobot = null;
        
        BattleForm.Instance?.ReleaseGarrison(this); // 二次保险
        
        TargetMineral = null;
        TargetWall = null;
        AssignedWall = null;
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

        // 【割草改动】医疗兵变为"全图光环枢纽"，围绕基地绝对安全地巡航
        var baseBot = BattleForm.Instance?.GetBaseRobot();
        if (baseBot != null)
        {
            float targetRadius = 100f + (Id % 5) * 20f; // 环绕在比矿工稍远一点的内圈
            float bDx = (baseBot.X + baseBot.Size / 2) - (X + Size / 2);
            float bDy = (baseBot.Y + baseBot.Size / 2) - (Y + Size / 2);
            float bDist = Math.Max(1, (float)Math.Sqrt(bDx * bDx + bDy * bDy));

            float maxSpeed = 3.0f * SpeedMultiplier;
            if (bDist > targetRadius + 15) { Dx += (bDx / bDist) * 0.4f; Dy += (bDy / bDist) * 0.4f; }
            else if (bDist < targetRadius - 15) { Dx -= (bDx / bDist) * 0.4f; Dy -= (bDy / bDist) * 0.4f; }
            else 
            { 
                float tangentDx = -(bDy / bDist) * maxSpeed * 0.5f;
                float tangentDy =  (bDx / bDist) * maxSpeed * 0.5f;
                Dx = Dx * 0.9f + tangentDx * 0.1f;
                Dy = Dy * 0.9f + tangentDy * 0.1f;
            }
            Dx *= 0.9f; Dy *= 0.9f;
        }

        int healerLevel = BattleForm.Instance?._healerLevel ?? 1;
        int interval = Math.Max(30, 120 - healerLevel * 10); // 随等级光环加速，最快半秒一跳

        if (_healCooldown <= 0)
        {
            _healCooldown = interval;
            
            // 【全图爆奶】包含百分比回血，极大加强对基地和被围墙单位的保护！
            int flatHeal = 50 + healerLevel * 30; // 基础大口奶
            float pctHeal = 0.02f + healerLevel * 0.005f; // 最大生命值百分比治疗
            
            // 奶机器人与基地
            foreach (var r in allRobots)
            {
                if (r.IsActive && !r.IsDead && r.HP < r.MaxHP)
                {
                    int totalHeal = flatHeal + (int)(r.MaxHP * pctHeal);
                    r.HP = Math.Min(r.MaxHP, r.HP + totalHeal);
                    
                    // 特效：全屏幕撒下生命之光
                    BattleForm.Instance?.AddExplosion(r.X + r.Size / 2, r.Y + r.Size / 2, Color.LimeGreen, 2, "SPARK");
                    // 为防止满屏跳字太卡，仅缺血严重的单位跳绿色大字
                    if (r == this || r.ClassType == RobotClass.Base || (float)r.HP / r.MaxHP < 0.4f)
                    {
                        BattleForm.Instance?.AddFloatingText(r.X + r.Size / 2, r.Y - 10, $"+{totalHeal}", Color.LimeGreen);
                    }
                }
            }
            
            // 奶所有城墙 (医疗兵的黑科技：纳米机器人隔空修复城墙)
            if (BattleForm.Instance != null && BattleForm.Instance._walls != null)
            {
                foreach(var w in BattleForm.Instance._walls)
                {
                    if (w.HP > 0 && w.HP < w.MaxHP)
                    {
                        int wallHeal = flatHeal * 2 + (int)(w.MaxHP * pctHeal);
                        w.HP = Math.Min(w.MaxHP, w.HP + wallHeal);
                    }
                }
            }
            
            // 中心爆发的大治疗圈特效
            BattleForm.Instance?.AddExplosion(X + Size / 2, Y + Size / 2, Color.LimeGreen, 15, "RING");
        }
    }

    private void UpdateGuardianLogic(List<Robot> allRobots, List<Monster> allMonsters)
    {
        var baseBot = BattleForm.Instance?.GetBaseRobot();
        if (baseBot == null) return;
        float bx = baseBot.X + baseBot.Size / 2;
        float by = baseBot.Y + baseBot.Size / 2;

        // 1. 轨道半径：严格同步墙体半径 (Layer 0: 150, Layer 1: 450)
        // 增加 25 像素偏移，使守护兵处于墙体稍微外一点点的位置。
        // 修正：守护者将跟随基地扩张，永远绕着最外层已激活防线外围 25 像素飞行
        int maxLayer = BattleForm.Instance?.MaxActiveLayer() ?? 0;
        float orbitRadius = 175f + maxLayer * 300f;

        // 2. 轨道旋转
        float orbitSpeed = 0.015f * SpeedMultiplier;
        OrbitAngle += orbitSpeed;
        if (OrbitAngle > (float)Math.PI * 2) OrbitAngle -= (float)Math.PI * 2;

        // 计算目标点 (使用 ID 作为偏移，使多个守卫者均匀分布在圆周上)
        int gCount = Math.Max(1, allRobots.Count(r => r.ClassType == RobotClass.Guardian && r.IsActive));
        float sector = (float)(Math.PI * 2 / gCount);
        int gIndex = allRobots.Where(r => r.ClassType == RobotClass.Guardian && r.IsActive).ToList().FindIndex(r => r == this);
        if (gIndex < 0) gIndex = 0;
        
        float targetX = bx + (float)Math.Cos(OrbitAngle + gIndex * sector) * orbitRadius;
        float targetY = by + (float)Math.Sin(OrbitAngle + gIndex * sector) * orbitRadius;

        // 移动到轨道点 (移除死区导致的一抖一抖，使用顺滑的前馈控制)
        float dxO = targetX - (X + Size / 2); // 以自身中心移动
        float dyO = targetY - (Y + Size / 2);
        
        // 预判轨道切线速度
        float targetVx = -(float)Math.Sin(OrbitAngle + gIndex * sector) * orbitRadius * orbitSpeed;
        float targetVy = (float)Math.Cos(OrbitAngle + gIndex * sector) * orbitRadius * orbitSpeed;

        // 丝滑 PD 控制：保持切线速度的同时，用位移偏差进行软修正
        Dx = Dx * 0.8f + (targetVx + dxO * 0.1f) * 0.2f;
        Dy = Dy * 0.8f + (targetVy + dyO * 0.1f) * 0.2f;

        // 限制最高爆发速度防止过度抖动
        float maxSpeed = 12.0f * SpeedMultiplier;
        float currentSpeed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
        if (currentSpeed > maxSpeed)
        {
            Dx = (Dx / currentSpeed) * maxSpeed;
            Dy = (Dy / currentSpeed) * maxSpeed;
        }

        // 3. 撞击战斗 (Body Slam) - 增强碰撞检测：高速下增加"扫描半径"防止穿模
        float currentV = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
        float extraHitRange = Math.Min(30f, currentV * 1.5f); // 速度越快，受击判定范围越大

        foreach (var m in allMonsters)
        {
            if (!m.IsActive || m.IsDead) continue;
            
            float mx = m.X + m.Size / 2;
            float my = m.Y + m.Size / 2;
            float dxM = mx - (X + Size / 2);
            float dyM = my - (Y + Size / 2);
            float distM = (float)Math.Sqrt(dxM * dxM + dyM * dyM);
            
            // 基础接触距离 + 动能扩展距离
            float contactDist = (Size + m.Size) / 2f + 5f + extraHitRange;

            if (distM < contactDist)
            {
                // 【修复：守卫者秒杀漏洞】之前守卫者会在0.1秒内撞击怪物数十次造成十万点真实伤害！
                // 现在增加"物理撞击内置冷却判定"，借用怪物的受击硬闪时间，使其撞击伤害更加合理
                if (m.HitFlashTimer <= 5)
                {
                    // 【守卫者强化：不仅撞击伤害更高，还带有毁天灭地的击退属性】
                    int totalDmg = (int)(Damage * 1.5f + Damage * 0.5f * currentV);
                    m.TakeDamage(totalDmg);
                    
                    // 撞击力场：将怪物狠狠撞飞！
                    float push = 10f + currentV * 2.5f;
                    m.Dx += (dxM / distM) * push;
                    m.Dy += (dyM / distM) * push;

                    // 视觉反馈：撞击产生更耀眼的闪光
                    BattleForm.Instance?.AddExplosion(mx, my, Color.Cyan, totalDmg > 300 ? 5 : 3, "SPARK");
                    
                    // 撞击反作用力 (略微损耗动能)
                    Dx *= 0.9f;
                    Dy *= 0.9f;
                }
            }
        }

        // 4. 被动电离力场 (守护者专属：对周围怪物进行随机电磁干扰)
        if (_frameCount % 20 == 0)
        {
            var lightningRange = 220f;
            var nearbyZaps = allMonsters
                .Where(m => m.IsActive && !m.IsDead && DistSq(m, X + Size / 2, Y + Size / 2) < lightningRange * lightningRange)
                .Take(2).ToList();

            if (nearbyZaps.Count > 0)
            {
                foreach (var nz in nearbyZaps) {
                    nz.TakeDamage(Damage / 4);
                }
                IsFiringLightning = true;
                LightningTargets.Clear();
                LightningTargets.AddRange(nearbyZaps);
                _delayedAttackTimer = 15; // 维持视觉连线
            }
        }

        // 应用阻力
        Dx *= 0.98f;
        Dy *= 0.98f;
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
        if (ClassType == RobotClass.Gunner || ClassType == RobotClass.Rocket || 
                 ClassType == RobotClass.Plasma || ClassType == RobotClass.Laser || ClassType == RobotClass.Lightning)
        {
            // 输出型：寻找敌人前方的城墙点驻扎防守
            if (AssignedWall == null || (AssignedWall.GarrisonRobot != null && AssignedWall.GarrisonRobot != this))
            {
                UnlockTargets();
                AssignedWall = BattleForm.Instance?.GetGarrisonWall(this, MonsterTarget);
            }

            if (AssignedWall != null)
            {
                var br = BattleForm.Instance?.GetBaseRobot();
                var wp = AssignedWall.GetWorldPosition(br?.X ?? 0, br?.Y ?? 0);
                float wDx = wp.X - X, wDy = wp.Y - Y;
                float wDist = (float)Math.Sqrt(wDx * wDx + wDy * wDy);

                if (wDist > 15) // 向墙上预定点移动
                {
                    float wallSpeed = 4.0f * SpeedMultiplier;
                    Dx = Dx * 0.8f + (wDx / wDist) * wallSpeed * 0.2f;
                    Dy = Dy * 0.8f + (wDy / wDist) * wallSpeed * 0.2f;
                }
                else // 已驻守，原地锁定并开火
                {
                    Dx *= 0.7f; Dy *= 0.7f;
                }
            }
            else // 极端情况：没找到城墙驻点 (如全部被毁)，则维持距离射击
            {
                float idealDistance = 300f; // 【范围倍增：150 -> 300】
                if (dist > idealDistance + 30) { Dx += (dx / dist) * 0.2f; Dy += (dy / dist) * 0.2f; }
                else if (dist < idealDistance - 30) { Dx -= (dx / dist) * 0.4f; Dy -= (dy / dist) * 0.4f; }
                else { Dx *= 0.9f; Dy *= 0.9f; }
            }

            // 发射攻击 (超远距离、必触发)
            if (ShootCooldown <= 0 && dist <= 1200) // 【范围倍增：600 -> 1200】
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

        AudioManager.PlayShootSound();
        SpecialState = "ANGRY";
        SpecialStateTimer = 100;

        var (targetX, targetY) = monster.GetCenter();
        float centerX = X + Size / 2;
        float centerY = Y + Size / 2;

        // 移除随机大招逻辑，确保兵种纯净度

        // 根据兵种确定攻击参数
        string type = CurrentAttackType;
        int level = 1;
        float scatter = 60f;
        int projectileCount = 1;

        switch (ClassType)
        {
            case RobotClass.Gunner:
                level = BattleForm.Instance?._shooterLevel ?? 1;
                ShootCooldown = AttackInterval;
                projectileCount = 2 + (level / 3);
                scatter = 200f; // 【散播火力：大幅增加散射范围以覆盖多目标】
                break;
            case RobotClass.Rocket:
                level = BattleForm.Instance?._rocketLevel ?? 1;
                ShootCooldown = AttackInterval;
                projectileCount = 1 + (level / 5);
                scatter = 20f;
                break;
            case RobotClass.Plasma:
                level = BattleForm.Instance?._plasmaLevel ?? 1;
                ShootCooldown = AttackInterval;
                projectileCount = 3 + (level / 4);
                scatter = 80f;
                break;
            case RobotClass.Laser:
                level = BattleForm.Instance?._laserLevel ?? 1;
                ShootCooldown = 35; // 【改为重型点射 约为 0.6s 一发】
                projectileCount = 1;
                type = "LASER"; 
                IsFiringLaser = false;
                break;
            case RobotClass.Lightning:
                level = BattleForm.Instance?._lightningLevel ?? 1;
                ShootCooldown = AttackInterval;
                projectileCount = 4 + (level / 2); // 【根据需求调整：闪电变为多目标持续电能链接】
                IsFiringLightning = true;
                _delayedAttackTimer = 30; // 维持 0.5 秒左右的链接效果
                LightningTargets.Clear();
                
                // 瞬发寻找范围内所有受害者并锁定
                if (BattleForm.Instance != null)
                {
                    var cx = X + Size / 2;
                    var cy = Y + Size / 2;
                    var nearby = BattleForm.Instance._monsters
                        .Where(m => m.IsActive && !m.IsDead && Math.Sqrt(Math.Pow(m.X - cx, 2) + Math.Pow(m.Y - cy, 2)) < 1200)
                        .OrderBy(m => Math.Sqrt(Math.Pow(m.X - cx, 2) + Math.Pow(m.Y - cy, 2)))
                        .Take(projectileCount)
                        .ToList();
                    
                    foreach(var target in nearby) LightningTargets.Add(target);
                }
                return; // 进入延迟攻击循环处理持续伤害
            default:
                ShootCooldown = 60;
                break;
        }

        IsFiringLaser = false;

        for (int i = 0; i < projectileCount; i++)
        {
            float pTargetX = targetX + (float)((Rand.NextDouble() - 0.5) * scatter);
            float pTargetY = targetY + (float)((Rand.NextDouble() - 0.5) * scatter);
            
            var p = new Projectile(this, centerX, centerY, pTargetX, pTargetY, type);
            // 基础追踪能力：所有攻击型单位等级 5 以上具备基本追踪，火箭 1 级即追踪
            if (level >= 5 || ClassType == RobotClass.Rocket) p.TrackingMonster = monster;
            
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

        // 使用自身确定的攻击类型与间隔
        ShootCooldown = AttackInterval;
        SpecialState = "ANGRY";
        SpecialStateTimer = 100;

        float centerX = X + Size / 2;
        float centerY = Y + Size / 2;

        var p = new Projectile(this, centerX, centerY, tx, ty, CurrentAttackType);
        BattleForm.Instance?.AddProjectile(p);
    }

    // 移除废弃的LaunchRemoteAttack

    private void UpdateDelayedAttack()
    {
        if (_delayedAttackTimer > 0)
        {
            _delayedAttackTimer--;
            
            // 【处理持续电弧链接伤害】
            if (IsFiringLightning)
            {
                foreach (var target in LightningTargets.ToList())
                {
                    if (target.IsActive && !target.IsDead)
                    {
                        if (_delayedAttackTimer % 2 == 0)
                        {
                            target.TakeDamage(Math.Max(1, Damage / 20));
                        }
                    }
                }
                if (_delayedAttackTimer == 0) { IsFiringLightning = false; LightningTargets.Clear(); }
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

            // --- AI 巡逻半径动态扩展：防线合拢后，机器人巡逻圈外推 ---
            int maxLayer = BattleForm.Instance?.MaxActiveLayer() ?? 0;
            float baseRadius = 70f + maxLayer * 350f; // 动态外推，如 Layer1 420, Layer2 770
            
            float idealRadius = baseRadius;
            if (ClassType == RobotClass.Guardian) idealRadius = baseRadius + (maxLayer > 0 ? 30f : 50f);
            else if (ClassType == RobotClass.Gunner || ClassType == RobotClass.Rocket || ClassType == RobotClass.Plasma || ClassType == RobotClass.Laser || ClassType == RobotClass.Lightning) idealRadius = baseRadius + (maxLayer > 0 ? 10f : 15f) + (Id % 4) * 12; 
            else if (ClassType == RobotClass.Healer) idealRadius = maxLayer > 0 ? (baseRadius - 20f) : 50f;

            float boostMult = SpeedBoostTimer > 0 ? 3.0f : 1.0f;
            float maxSpeed = 2.5f * SpeedMultiplier * boostMult;

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
        UnlockTargets();
        AudioManager.PlayDeathSound();

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
            else if (p.Type == "CANNON") baseDmg = 40 + wave * 8; // 【修复】之前精英怪的炮弹被遗漏，伤害只有5，现在补回精英级伤害
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
            "METEOR" => 15, // 【平衡削弱】从 60 狂砍到 15，因为有8连发和极限射速，否则DPS太夸张
            "BLACK_HOLE" => 2,
            "DEATH_RAY" => 250, // 【割草加强】工程师放的死亡射线，一发必死
            _ => 5
        };

        // 获取对应的等级加成
        int levelBonus = 0;
        if (BattleForm.Instance != null)
        {
            levelBonus = ClassType switch
            {
                RobotClass.Gunner => BattleForm.Instance._shooterLevel - 1,
                RobotClass.Rocket => BattleForm.Instance._rocketLevel - 1,
                RobotClass.Plasma => BattleForm.Instance._plasmaLevel - 1,
                RobotClass.Laser => BattleForm.Instance._laserLevel - 1,
                RobotClass.Lightning => BattleForm.Instance._lightningLevel - 1,
                RobotClass.Worker => BattleForm.Instance._workerLevel - 1,
                RobotClass.Healer => BattleForm.Instance._healerLevel - 1,
                RobotClass.Guardian => BattleForm.Instance._guardianLevel - 1,
                RobotClass.Engineer => BattleForm.Instance._engineerLevel - 1,
                _ => 0
            };
        }

        // 基础伤害 + 等级提升 (等级3以上每级+25%，Lv.1为100%伤害)
        float factor = (1.0f + levelBonus * 0.25f) * 11; // 【全域伤害 11 倍化】
        
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

    private float DistSq(Monster m, float x, float y)
    {
        float dx = (m.X + m.Size / 2) - x;
        float dy = (m.Y + m.Size / 2) - y;
        return dx * dx + dy * dy;
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
