using System;
using System.Linq;

namespace PureBattleGame.Games.CockroachPet;

public partial class Robot
{
    // 生命值系统
    public int HP { get; set; } = 1000;
    public int MaxHP { get; set; } = 1000;

    // 战斗与追逐
    public Robot? ChasingTarget { get; set; }
    public Monster? MonsterTarget { get; set; } // 怪物目标
    public int ChaseTimer { get; set; } = 0;
    public int ShootCooldown { get; set; } = 0;
    public bool IsFiringLaser { get; set; } = false;
    public float LaserTargetX { get; set; }
    public float LaserTargetY { get; set; }
    public string CurrentAttackType { get; set; } = "LASER";
    public Robot? TargetRobot { get; set; }

    // 陀螺格斗模式
    public Robot? DuelTarget { get; set; }
    public int DuelTimer { get; set; } = 0;
    private float _duelAngle = 0;

    private Robot? _delayedAttackTarget = null;
    private int _delayedAttackTimer = 0;

    private void UpdateChasingLogic()
    {
        // 如果有怪物目标，优先攻击怪物，不追逐其他机器人
        if (MonsterTarget != null && MonsterTarget.IsActive && !MonsterTarget.IsDead)
            return;

        if (ChaseTimer > 0 && ChasingTarget != null && ChasingTarget.IsActive)
        {
            float tdx = ChasingTarget.X - X;
            float tdy = ChasingTarget.Y - Y;
            float dist = (float)Math.Sqrt(tdx * tdx + tdy * tdy);

            if (dist > 50)
            {
                Dx = (Dx * 0.85f) + (tdx / dist * 0.4f * SpeedMultiplier * 1.5f);
                Dy = (Dy * 0.85f) + (tdy / dist * 0.4f * SpeedMultiplier * 1.5f);

                if (ChaseTimer % 60 == 0)
                {
                    string[] rages = { "嗝呐！", "受死吧！", "站住别跑！", "我要拆了你的主板！", "吃我一招！" };
                    SetBark(rages[Rand.Next(rages.Length)], 60);
                }
            }
            else
            {
                StartDuel(ChasingTarget);
                ChaseTimer = 0;
                ChasingTarget = null;
            }

            ChaseTimer--;
            if (ChaseTimer == 0) ChasingTarget = null;
        }

        if (IsFiringLaser && TargetRobot != null && TargetRobot.IsActive)
        {
            LaserTargetX = TargetRobot.X + (float)TargetRobot.Size / 2;
            LaserTargetY = TargetRobot.Y + (float)TargetRobot.Size / 2;
        }
        else if (IsFiringLaser)
        {
            IsFiringLaser = false;
            TargetRobot = null;
        }
    }

    private void UpdateDelayedAttack()
    {
        if (_delayedAttackTimer > 0)
        {
            _delayedAttackTimer--;
            if (_delayedAttackTimer == 0 && _delayedAttackTarget != null)
            {
                IsFiringLaser = false;
                if (_delayedAttackTarget.IsActive && !_delayedAttackTarget.IsDead)
                {
                    _delayedAttackTarget.ApplyAttackEffect();
                    _delayedAttackTarget.ChasingTarget = this;
                    _delayedAttackTarget.ChaseTimer = 450;
                }
                _delayedAttackTarget = null;
                TargetRobot = null;
            }
        }
    }

    private bool UpdateDuelLogic()
    {
        // 如果有怪物目标，优先攻击怪物，不进行格斗
        if (MonsterTarget != null && MonsterTarget.IsActive && !MonsterTarget.IsDead)
            return false;

        if (DuelTimer > 0 && DuelTarget != null && DuelTarget.IsActive)
        {
            if (DuelTarget.IsDead)
            {
                DuelTimer = 0;
                DuelTarget = null;
                RotationAngle = 0;
                return true;
            }
            DuelTimer--;

            float centerX = (X + DuelTarget.X) / 2;
            float centerY = (Y + DuelTarget.Y) / 2;

            int phase = DuelTimer % 40;
            float radius = 0;

            if (phase > 25)
            {
                radius = 40 + (40 - phase) * 4;
                SpecialState = "NORMAL";
            }
            else if (phase > 10)
            {
                radius = (phase - 10) * 5;
                SpecialState = "ANGRY";
                if (phase == 15)
                {
                    string[] clashBarks = { "吃我一记冲撞！", "💥 像素碎裂！", "给老子死！", "铁拳制裁！" };
                    SetBark(clashBarks[Rand.Next(clashBarks.Length)], 30);
                }
            }
            else
            {
                radius = (float)Rand.NextDouble() * 8;
                SpecialState = "SHAKING";
                SpecialStateTimer = 2;
                RotationAngle += 60f;

                if (phase % 4 == 0) LaunchRemoteAttack(DuelTarget);

                X += (float)(Rand.NextDouble() - 0.5) * 15;
                Y += (float)(Rand.NextDouble() - 0.5) * 15;
            }

            float dirX = (Id % 2 == 0) ? 1 : -1;
            float angle = (float)(Math.PI / 4 + (Id % 2) * Math.PI);
            float globalRotate = (DuelTimer / 100f) * (float)Math.PI;
            float finalAngle = angle + globalRotate;

            X = centerX + (float)Math.Cos(finalAngle) * radius;
            Y = centerY + (float)Math.Sin(finalAngle) * radius;

            if (DuelTimer == 0)
            {
                DuelTarget = null;
                float escapeX = X - centerX;
                float escapeY = Y - centerY;
                float eDist = (float)Math.Max(1, Math.Sqrt(escapeX * escapeX + escapeY * escapeY));
                Dx = (escapeX / eDist) * 30;
                Dy = (escapeY / eDist) * 30;
                SetBark("像素核心...爆发！🔥", 100);
            }
            return true;
        }
        return false;
    }

    private void UpdateChaseAndAttack()
    {
        // 如果有怪物目标，优先攻击怪物，不互殴
        if (MonsterTarget != null && MonsterTarget.IsActive && !MonsterTarget.IsDead)
            return;

        if (ChaseTimer > 0 && ChasingTarget != null)
        {
            if (ChasingTarget.IsDead)
            {
                ChaseTimer = 0;
                ChasingTarget = null;
                return;
            }
            ChaseTimer--;
            float tx = ChasingTarget.X - X;
            float ty = ChasingTarget.Y - Y;
            float tdist = (float)Math.Sqrt(tx * tx + ty * ty);

            if (tdist > 40)
            {
                Dx = (Dx * 0.9f) + (tx / tdist * 0.5f * SpeedMultiplier);
                Dy = (Dy * 0.9f) + (ty / tdist * 0.5f * SpeedMultiplier);

                if (ShootCooldown == 0 && Rand.Next(100) < 5) LaunchRemoteAttack(ChasingTarget);
            }
            else
            {
                int aliveCount = PetForm.Instance?.GetRobots().Count(r => !r.IsDead && r.IsVisible && r.IsActive) ?? 0;
                if (aliveCount > 0 && aliveCount % 2 == 0)
                {
                    StartDuel(ChasingTarget);
                }
                else
                {
                    if (ShootCooldown == 0) LaunchRemoteAttack(ChasingTarget);
                    PerformPush(ChasingTarget);
                }
                ChaseTimer = 0;
                ChasingTarget = null;
            }

            if (ChaseTimer == 0) ChasingTarget = null;
        }
    }

    private void LaunchRemoteAttack(Robot other)
    {
        if (other == null || !other.IsActive || other.IsDead || IsDead) return;

        string[] attackBarks = { "看招！炸裂吧！💥", "吃我一记像素光波！⚡", "系统过载灌入！🔥", "目标锁定，发射！🎯", "吃我一记禁言锤！🔨", "像素风暴攻击！🌀" };
        SetBark(attackBarks[Rand.Next(attackBarks.Length)], 100);
        SpecialState = "ANGRY";
        SpecialStateTimer = 100;
        ShootCooldown = IsWeaponMaster ? 120 : 400;

        if (IsWeaponMaster)
        {
            string[] ammoTypes = { "BULLET", "ROCKET", "PLASMA", "CANNON", "LIGHTNING", "SPIT", "INK" };
            string selectedType = ammoTypes[Rand.Next(ammoTypes.Length)];

            string weaponBark = selectedType switch {
                "CANNON" => "超级重炮...发射！💣",
                "LIGHTNING" => "十万伏特！⚡",
                "SPIT" => "受死吧！呗！🤢",
                "INK" => "墨迹干扰！🕭️",
                "ROCKET" => "追踪火箭！🚀",
                _ => "接受像素打击吧！"
            };
            SetBark(weaponBark, 80);

            float centerX = X + Size / 2;
            float centerY = Y + Size / 2;
            float targetX = other.X + other.Size / 2;
            float targetY = other.Y + other.Size / 2;

            var p = new Projectile(this, centerX, centerY, targetX, targetY, selectedType, other);
            PetForm.Instance?.AddProjectile(p);
            AudioManager.PlayProjectileSound(selectedType);
        }
        else
        {
            string[] types = { "LASER", "SHOCK", "BURST" };
            CurrentAttackType = types[Rand.Next(types.Length)];
            TargetRobot = other;
            IsFiringLaser = true;
            LaserTargetX = other.X + (float)other.Size / 2;
            LaserTargetY = other.Y + (float)other.Size / 2;

            _delayedAttackTarget = other;
            _delayedAttackTimer = 25;
            AudioManager.PlayLaserSound();
        }
    }

    public void ApplyAttackEffect(int damage = 5)
    {
        if (IsDead) return;
        HP = Math.Max(0, HP - damage);

        if (HP <= 0)
        {
            IsDead = true;
            IsMoving = false;
            RotationAngle = 90f;
            Dx = 0; Dy = 0;
            SetBark("核心崩溃...系统下线 💀", 200);
            AudioManager.PlayDeathSound();
        }
        else
        {
            // 受伤音效
            AudioManager.PlayHitSound();
        }

        LastDamageText = $"-{damage}";
        DamageTextTimer = 45;
        DamageFeedbackTimer = 60;

        SpecialState = "SHAKING";
        SpecialStateTimer = 60;
        string[] reactBarks = { "哎呦！谁偷袭我？！", "我的电路着火了！", "你会付出代价的！", "发生错误！痛死我了！", "嗝呐！" };
        if (Rand.Next(100) < 30) SetBark(reactBarks[Rand.Next(reactBarks.Length)], 120);
    }

    public void HandleProjectileHit(Projectile p)
    {
        if (!IsActive) return;

        int baseDmg = p.Type switch {
            "ROCKET" => 15,
            "CANNON" => 25,
            "LIGHTNING" => 10,
            "PLASMA" => 20,
            _ => 5
        };
        ApplyAttackEffect(baseDmg);

        switch(p.Type)
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
                SetBark("好重！像是被卡车撞了！", 60);
                break;
            case "LIGHTNING":
                StunTimer = 90;
                SpecialState = "SHAKING";
                SpecialStateTimer = 90;
                SetBark("⚡ 呜啊！被电焦了！", 60);
                break;
            case "SPIT":
                SlowTimer = 180;
                SetBark("呗！太恶心了！🤢", 60);
                break;
            case "INK":
                BlindTimer = 240;
                StatusMessage = "谁在墨水里下毒了！";
                SetBark("黑漆漆的一片！🕭️", 60);
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

        if (p.Owner != null && p.Owner != this)
        {
            ChasingTarget = p.Owner;
            ChaseTimer = 400;
        }
    }

    /// <summary>
    /// 设置怪物目标 - 机器人会集火攻击怪物
    /// </summary>
    public void SetMonsterTarget(Monster monster)
    {
        MonsterTarget = monster;
        StatusMessage = "ATTACKING_BOSS";
        SetBark("发现目标！集火攻击！🔥", 60);
    }

    /// <summary>
    /// 更新对怪物的攻击逻辑
    /// </summary>
    public void UpdateMonsterAttack()
    {
        if (MonsterTarget == null || !MonsterTarget.IsActive || MonsterTarget.IsDead)
        {
            MonsterTarget = null;
            return;
        }

        // 计算到怪物的距离
        var (monsterX, monsterY) = MonsterTarget.GetCenter();
        float dx = monsterX - (X + Size / 2);
        float dy = monsterY - (Y + Size / 2);
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        // 防止除以零
        if (dist < 1f) dist = 1f;

        // 移动到攻击距离 - 使用平滑移动，限制最大速度
        float maxSpeed = 3.0f * SpeedMultiplier;
        if (dist > 150)
        {
            // 计算目标速度
            float targetDx = (dx / dist) * maxSpeed;
            float targetDy = (dy / dist) * maxSpeed;
            // 平滑过渡到目标速度
            Dx = Dx * 0.9f + targetDx * 0.1f;
            Dy = Dy * 0.9f + targetDy * 0.1f;
        }
        else if (dist < 100)
        {
            // 太近了，后退
            Dx -= (dx / dist) * 0.3f;
            Dy -= (dy / dist) * 0.3f;
        }
        else
        {
            // 在理想距离内，减速停止
            Dx *= 0.95f;
            Dy *= 0.95f;
        }

        // 限制最大速度，防止飞出去
        float currentSpeed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
        if (currentSpeed > maxSpeed)
        {
            Dx = (Dx / currentSpeed) * maxSpeed;
            Dy = (Dy / currentSpeed) * maxSpeed;
        }

        // 发射子弹攻击怪物
        if (ShootCooldown == 0 && Rand.Next(100) < 8)
        {
            LaunchAttackAtMonster(MonsterTarget);
        }
    }

    /// <summary>
    /// 向怪物发射攻击
    /// </summary>
    private void LaunchAttackAtMonster(Monster monster)
    {
        if (monster == null || !monster.IsActive || monster.IsDead || IsDead) return;

        string[] attackBarks = { "吃我一招！💥", "怪物受死！⚡", "集火攻击！🎯", "火力全开！🔥", "为了奖励！💰" };
        SetBark(attackBarks[Rand.Next(attackBarks.Length)], 80);
        SpecialState = "ANGRY";
        SpecialStateTimer = 100;
        ShootCooldown = IsWeaponMaster ? 60 : 120;

        var (targetX, targetY) = monster.GetCenter();
        float centerX = X + Size / 2;
        float centerY = Y + Size / 2;

        if (IsWeaponMaster)
        {
            string[] ammoTypes = { "BULLET", "ROCKET", "PLASMA", "CANNON", "LIGHTNING" };
            string selectedType = ammoTypes[Rand.Next(ammoTypes.Length)];

            var p = new Projectile(this, centerX, centerY, targetX, targetY, selectedType, null);
            // 怪物不需要追踪，直接瞄准
            PetForm.Instance?.AddProjectile(p);
            AudioManager.PlayProjectileSound(selectedType);
        }
        else
        {
            // 普通攻击
            var p = new Projectile(this, centerX, centerY, targetX, targetY, "BULLET", null);
            PetForm.Instance?.AddProjectile(p);
            AudioManager.PlayShootSound();
        }

        // 激光效果
        IsFiringLaser = true;
        LaserTargetX = targetX;
        LaserTargetY = targetY;
        TargetRobot = null; // 不是攻击机器人
    }

    private void StartDuel(Robot other)
    {
        if (DuelTarget != null || other.DuelTarget != null) return;

        DuelTarget = other;
        other.DuelTarget = this;
        DuelTimer = 180;
        other.DuelTimer = 180;

        _duelAngle = (float)(Rand.NextDouble() * Math.PI * 2);
        other._duelAngle = _duelAngle + (float)Math.PI;

        string[] dBark = { "陀螺模式启动！", "看我旋风冲锋！", "格斗开始！", "像素激战！" };
        SetBark(dBark[Rand.Next(dBark.Length)], 60);
        other.SetBark(dBark[Rand.Next(dBark.Length)], 60);

        SpecialState = "SPINNING";
        other.SpecialState = "SPINNING";
    }
}
