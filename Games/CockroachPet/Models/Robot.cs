using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PureBattleGame.Games.CockroachPet;

public partial class Robot
{
    // 基本信息
    public string Name { get; set; }
    public int Id { get; set; }

    // 位置
    public float X { get; set; }
    public float Y { get; set; }

    // 速度
    public float Dx { get; set; }
    public float Dy { get; set; }

    // 朝向
    public bool FacingRight { get; set; } = true;

    // 状态
    public bool IsMoving { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsDead { get; set; } = false;

    // 动画
    public int AnimationFrame { get; set; } = 0;
    public int AnimationCounter { get; set; } = 0;

    // 大小
    public int Size { get; set; } = 64;
    public int OriginalSize { get; set; } = 64;

    // 速度倍率
    public float SpeedMultiplier { get; set; } = 1.0f;

    // 终端状态
    public string LastOutput { get; set; } = "";
    public string StatusMessage { get; set; } = "IDLE";
    public string AlertMessage { get; set; } = "";
    public bool IsWarning { get; set; } = false;
    public int WarningTimer { get; set; } = 0;
    public event Action<string>? OnTerminalOutput;

    // AI 自主思考设置
    public bool EnableAiThinking { get; set; } = false;
    public int AiThoughtFrequency { get; set; } = 60;
    public int FightFrequency { get; set; } = 15;
    public bool IsWeaponMaster { get; set; } = false;

    // 颜色主题
    public Color PrimaryColor { get; set; }
    public Color SecondaryColor { get; set; }
    public Color EyeColor { get; set; }

    // 透明度 (0-255, 255为不透明)
    public int Opacity { get; set; } = 255;

    // 随机行为
    public int PauseTimer { get; set; } = 0;
    public int ChangeDirectionTimer { get; set; } = 0;
    public int RestTimer { get; set; } = 0;
    public Random Rand { get; set; } = new Random();

    // 八爪鱼触手动画
    public float[] TentacleOffsets { get; set; } = new float[8];

    public Robot(int id, string name, float x, float y)
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;

        // 随机颜色主题
        var colors = new (Color primary, Color secondary, Color eye)[]
        {
            (Color.FromArgb(255, 107, 107), Color.FromArgb(255, 77, 77), Color.FromArgb(255, 255, 255)),
            (Color.FromArgb(77, 171, 255), Color.FromArgb(51, 153, 255), Color.FromArgb(255, 255, 0)),
            (Color.FromArgb(107, 255, 107), Color.FromArgb(77, 221, 77), Color.FromArgb(255, 100, 100)),
            (Color.FromArgb(255, 200, 77), Color.FromArgb(255, 170, 51), Color.FromArgb(100, 50, 255)),
            (Color.FromArgb(200, 107, 255), Color.FromArgb(170, 77, 221), Color.FromArgb(0, 255, 255)),
            (Color.FromArgb(255, 150, 200), Color.FromArgb(255, 120, 170), Color.FromArgb(0, 0, 0)),
        };
        var theme = colors[Rand.Next(colors.Length)];
        PrimaryColor = theme.primary;
        SecondaryColor = theme.secondary;
        EyeColor = theme.eye;

        // 随机个性类型（新个性系统）
        PersonalityType = Robot.GetRandomPersonality(Rand);
        InitializePersonalityTraits();

        // 随机初始方向
        double angle = Rand.NextDouble() * Math.PI * 2;
        float speed = 1.5f + Rand.NextFloat() * 1.5f;
        Dx = (float)Math.Cos(angle) * speed;
        Dy = (float)Math.Sin(angle) * speed;

        // 初始化基础技能
        InitializeDefaultSkills();

        // 初始化自愈/进化逻辑
        _selfImproving = new SelfImprovingManager(Id, Name);
        OriginalSize = Size;
    }

    public void Update(int screenWidth, int screenHeight)
    {
        // 安全检查：防止坐标异常
        if (float.IsNaN(X) || float.IsInfinity(X)) X = screenWidth / 2f;
        if (float.IsNaN(Y) || float.IsInfinity(Y)) Y = screenHeight / 2f;

        // 1. 基础物理边界限制
        if (X < 0) { X = 0; Dx = -Dx; }
        if (X > screenWidth - Size) { X = screenWidth - Size; Dx = -Dx; }
        if (Y < 0) { Y = 0; Dy = -Dy; }
        if (Y > screenHeight - Size) { Y = screenHeight - Size; Dy = -Dy; }

        // 如果游戏进入终局（胜者吞噬阶段），仅执行基础位移
        if (PetForm.Instance != null && PetForm.Instance.IsGameEnding)
        {
            if (IsDead) { Dx = 0; Dy = 0; return; }
            X += Dx;
            Y += Dy;
            Dx *= 0.98f;
            Dy *= 0.98f;
            return;
        }

        if (!IsActive) return;
        if (ShootCooldown > 0) ShootCooldown--;

        if (WarningTimer > 0)
        {
            WarningTimer--;
            if (WarningTimer == 0) IsWarning = false;
        }

        if (!IsActive) return;

        // 追逐逻辑 (优先级最高)
        UpdateChasingLogic();

        // 怪物攻击逻辑
        UpdateMonsterAttack();

        // 被拉取/被抛投的物理限制
        UpdatePhysicsConstraints();

        // 处理延迟攻击反馈
        UpdateDelayedAttack();

        if (!IsMoving && ChaseTimer <= 0) return;

        // 停顿逻辑
        if (PauseTimer > 0)
        {
            PauseTimer--;
            UpdateTentacles(true);
            return;
        }

        if (Rand.Next(1000) < 3)
        {
            PauseTimer = Rand.Next(30, 90);
            return;
        }

        // 陀螺格斗逻辑
        if (UpdateDuelLogic()) return;

        // 追逐逻辑与边追边射
        UpdateChaseAndAttack();

        // 随机改变方向
        UpdateRandomDirection();

        // 移动与摩擦力
        ApplyMovement();

        // 社交与跟随逻辑
        UpdateSocialLogic();

        // 生命耗尽处理
        if (HP <= 0 && !IsDead)
        {
            HandleDeath();
        }

        if (IsDead)
        {
            HandleDeadState(screenWidth, screenHeight);
            return;
        }

        // 流式文字逻辑
        UpdateStreamingChat();

        // 自定义台词随机触发
        UpdateCustomPhrases();

        // AI 思考逻辑
        UpdateAiThinking();

        // 情绪系统更新
        UpdateEmotion();

        // 应用个性到行为
        ApplyPersonalityToBehavior();

        // 更新朝向
        if (Dx > 0.1f) FacingRight = true;
        else if (Dx < -0.1f) FacingRight = false;

        // 更新动画
        AnimationCounter++;
        if (AnimationCounter >= 8)
        {
            AnimationCounter = 0;
            AnimationFrame = (AnimationFrame + 1) % 4;
        }

        UpdateSpecialAnimations();
        UpdateTentacles(false);
    }

    private void ApplyMovement()
    {
        float finalSpeed = SpeedMultiplier * EmotionSpeedMultiplier;
        if (SlowTimer > 0) finalSpeed *= 0.4f;
        if (StunTimer > 0) finalSpeed = 0;

        // 安全检查：防止 NaN 或 Infinity
        if (float.IsNaN(Dx) || float.IsInfinity(Dx)) Dx = 0;
        if (float.IsNaN(Dy) || float.IsInfinity(Dy)) Dy = 0;

        // 限制最大速度，防止跳变
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

    private void HandleDeadState(int screenWidth, int screenHeight)
    {
        Dx = 0; Dy = 0;
        X = Math.Max(0, Math.Min(X, screenWidth - Size));
        Y = Math.Max(0, Math.Min(Y, screenHeight - Size));
    }

    public bool HitTest(int mx, int my)
    {
        return mx >= X && mx <= X + Size &&
               my >= Y && my <= Y + Size;
    }

    public void OpenTerminal()
    {
        TerminalManagerForm.Instance.OpenTerminal(this);
    }

    public void CloseTerminal()
    {
        TerminalManagerForm.Instance.CloseTerminal(this);
    }

    public void NotifyOutput(string text, bool isError = false)
    {
        LastOutput = text;
        OnTerminalOutput?.Invoke(text);

        if (isError)
        {
            StatusMessage = "ERROR";
            AlertMessage = "SOMETHING BROKE!";
            IsWarning = true;
            WarningTimer = 180;
            return;
        }

        if (text.Contains("(y/n)", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("[y/n]", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Confirm?", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Proceed?", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Continue?", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "WAITING";
            AlertMessage = "CLAUDE NEEDS YOU!";
            IsWarning = true;
            WarningTimer = 300;
        }
        else if (text.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("not recognized", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "ERROR";
            AlertMessage = "SOMETHING BROKE!";
            IsWarning = true;
            WarningTimer = 180;
        }
        else if (text.Contains("Finished", StringComparison.OrdinalIgnoreCase)) StatusMessage = "COMPLETED";
        else if (text.Contains("Running", StringComparison.OrdinalIgnoreCase)) StatusMessage = "BUSY";
    }

    public void NotifyAiToolStarted(string toolName, int processId)
    {
        StatusMessage = $"RUNNING {toolName.ToUpper()}";
        AlertMessage = $"USING {toolName.ToUpper()}!";
        IsWarning = false;
        WarningTimer = 60;
        System.Diagnostics.Debug.WriteLine($"[Robot {Name}] AI tool started: {toolName} (PID: {processId})");
    }
}

public static class RandomExtensions
{
    public static float NextFloat(this Random rand)
    {
        return (float)rand.NextDouble();
    }
}
