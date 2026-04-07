namespace PureBattleGame.Games.CockroachPet;

/// <summary>
/// 机器人情绪状态枚举
/// </summary>
public enum EmotionState
{
    Neutral,    // 平静
    Happy,      // 开心
    Excited,    // 兴奋
    Sad,        // 难过
    Angry,      // 生气
    Scared,     // 害怕
    Curious,    // 好奇
    Bored,      // 无聊
    Sleepy      // 困倦
}

/// <summary>
/// 情绪触发源
/// </summary>
public enum EmotionTrigger
{
    None,           // 无
    UserChat,       // 用户聊天
    RobotChat,      // 机器人聊天
    PhysicalHit,    // 被物理碰撞
    Alone,          // 独处
    Crowded,        // 拥挤
    Exploring,      // 探索
    Resting,        // 休息
    Random          // 随机
}

/// <summary>
/// 情绪系统 - 管理机器人的情绪状态
/// </summary>
public partial class Robot
{
    // 当前情绪状态
    public EmotionState CurrentEmotion { get; set; } = EmotionState.Neutral;

    // 情绪强度 (0-100)
    public int EmotionIntensity { get; set; } = 50;

    // 情绪持续时间计时器
    public int EmotionTimer { get; set; } = 0;

    // 情绪变化冷却时间
    public int EmotionCooldown { get; set; } = 0;

    // 情绪相关的视觉表现
    public float EmotionAnimationOffset { get; set; } = 0f;
    public int EmotionParticleTimer { get; set; } = 0;

    // 情绪对行为的影响参数
    public float EmotionSpeedMultiplier => GetEmotionSpeedMultiplier();
    public float EmotionSocialMultiplier => GetEmotionSocialMultiplier();
    public bool EmotionMakesJumping => CurrentEmotion == EmotionState.Excited || CurrentEmotion == EmotionState.Happy;
    public bool EmotionMakesShaking => CurrentEmotion == EmotionState.Scared || CurrentEmotion == EmotionState.Angry;

    // 情绪颜色
    public Color GetEmotionColor()
    {
        return CurrentEmotion switch
        {
            EmotionState.Happy => Color.FromArgb(255, 220, 100),
            EmotionState.Excited => Color.FromArgb(255, 150, 50),
            EmotionState.Sad => Color.FromArgb(100, 150, 200),
            EmotionState.Angry => Color.FromArgb(255, 80, 80),
            EmotionState.Scared => Color.FromArgb(180, 180, 180),
            EmotionState.Curious => Color.FromArgb(150, 255, 150),
            EmotionState.Bored => Color.FromArgb(180, 180, 150),
            EmotionState.Sleepy => Color.FromArgb(150, 150, 200),
            _ => Color.White
        };
    }

    // 情绪表情符号
    public string GetEmotionEmoji()
    {
        return CurrentEmotion switch
        {
            EmotionState.Happy => "😊",
            EmotionState.Excited => "🤩",
            EmotionState.Sad => "😢",
            EmotionState.Angry => "😠",
            EmotionState.Scared => "😨",
            EmotionState.Curious => "🤔",
            EmotionState.Bored => "😴",
            EmotionState.Sleepy => "💤",
            _ => "😐"
        };
    }

    // 情绪名称
    public string GetEmotionName()
    {
        return CurrentEmotion switch
        {
            EmotionState.Happy => "开心",
            EmotionState.Excited => "兴奋",
            EmotionState.Sad => "难过",
            EmotionState.Angry => "生气",
            EmotionState.Scared => "害怕",
            EmotionState.Curious => "好奇",
            EmotionState.Bored => "无聊",
            EmotionState.Sleepy => "困倦",
            _ => "平静"
        };
    }

    /// <summary>
    /// 更新情绪系统
    /// </summary>
    private void UpdateEmotion()
    {
        // 情绪计时器递减
        if (EmotionTimer > 0)
        {
            EmotionTimer--;
            if (EmotionTimer == 0 && CurrentEmotion != EmotionState.Neutral)
            {
                // 情绪自然衰减到平静
                TransitionEmotion(EmotionState.Neutral, 30);
            }
        }

        if (EmotionCooldown > 0) EmotionCooldown--;

        // 动画效果
        if (EmotionAnimationOffset != 0)
        {
            EmotionAnimationOffset *= 0.95f;
            if (Math.Abs(EmotionAnimationOffset) < 0.1f) EmotionAnimationOffset = 0;
        }

        // 粒子效果计时器
        if (EmotionParticleTimer > 0) EmotionParticleTimer--;

        // 环境因素自动影响情绪
        if (EmotionCooldown <= 0 && Rand.Next(1000) < 5) // 低概率随机情绪变化
        {
            ApplyEnvironmentalEmotion();
        }
    }

    /// <summary>
    /// 设置情绪状态
    /// </summary>
    public void SetEmotion(EmotionState emotion, int intensity = 50, int duration = 300, EmotionTrigger trigger = EmotionTrigger.None)
    {
        if (EmotionCooldown > 0 && emotion != EmotionState.Neutral) return;

        CurrentEmotion = emotion;
        EmotionIntensity = Math.Clamp(intensity, 0, 100);
        EmotionTimer = duration + Rand.Next(60);
        EmotionCooldown = 60; // 情绪变化冷却

        // 触发视觉效果
        EmotionAnimationOffset = emotion == EmotionState.Excited || emotion == EmotionState.Scared ? 5f : 2f;
        EmotionParticleTimer = emotion == EmotionState.Happy || emotion == EmotionState.Excited ? 60 : 0;

        // 记录情绪变化到调试输出
        System.Diagnostics.Debug.WriteLine($"[Robot {Name}] 情绪变化: {GetEmotionName()} (强度:{EmotionIntensity}, 来源:{trigger})");
    }

    /// <summary>
    /// 平滑过渡情绪
    /// </summary>
    private void TransitionEmotion(EmotionState targetEmotion, int speed = 30)
    {
        if (CurrentEmotion == targetEmotion) return;
        SetEmotion(targetEmotion, EmotionIntensity / 2, speed, EmotionTrigger.None);
    }

    /// <summary>
    /// 根据环境因素自动调整情绪
    /// </summary>
    private void ApplyEnvironmentalEmotion()
    {
        var robots = PetForm.Instance?.GetRobots();
        if (robots == null) return;

        int nearbyCount = 0;
        foreach (var r in robots)
        {
            if (r != this && r.IsActive && r.IsVisible)
            {
                float dist = (float)Math.Sqrt(Math.Pow(r.X - X, 2) + Math.Pow(r.Y - Y, 2));
                if (dist < 150) nearbyCount++;
            }
        }

        // 根据环境决定情绪
        if (nearbyCount >= 3)
        {
            SetEmotion(EmotionState.Curious, 60, 240, EmotionTrigger.Crowded);
        }
        else if (nearbyCount == 0 && Rand.Next(100) < 30)
        {
            SetEmotion(EmotionState.Bored, 40, 300, EmotionTrigger.Alone);
        }
        else if (IsMoving && Rand.Next(100) < 20)
        {
            SetEmotion(EmotionState.Curious, 50, 240, EmotionTrigger.Exploring);
        }
    }

    /// <summary>
    /// 获取情绪对速度的影响倍数
    /// </summary>
    private float GetEmotionSpeedMultiplier()
    {
        return CurrentEmotion switch
        {
            EmotionState.Excited => 1.3f + (EmotionIntensity / 200f),
            EmotionState.Happy => 1.1f + (EmotionIntensity / 300f),
            EmotionState.Angry => 1.2f + (EmotionIntensity / 250f),
            EmotionState.Scared => 1.4f + (EmotionIntensity / 200f),
            EmotionState.Sad => 0.7f - (EmotionIntensity / 300f),
            EmotionState.Bored => 0.6f - (EmotionIntensity / 250f),
            EmotionState.Sleepy => 0.5f - (EmotionIntensity / 200f),
            _ => 1.0f
        };
    }

    /// <summary>
    /// 获取情绪对社交的影响倍数
    /// </summary>
    private float GetEmotionSocialMultiplier()
    {
        return CurrentEmotion switch
        {
            EmotionState.Happy => 1.5f,
            EmotionState.Excited => 2.0f,
            EmotionState.Curious => 1.8f,
            EmotionState.Sad => 0.3f,
            EmotionState.Angry => 0.2f,
            EmotionState.Scared => 0.1f,
            EmotionState.Bored => 1.2f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// 根据情绪获取AI思考提示词
    /// </summary>
    public string GetEmotionPrompt()
    {
        string baseEmotion = GetEmotionName();
        string intensityDesc = EmotionIntensity switch
        {
            > 80 => "非常",
            > 60 => "比较",
            > 40 => "有点",
            _ => "略微"
        };

        return $"你当前{intensityDesc}{baseEmotion}。";
    }

    /// <summary>
    /// 根据情绪获取行为描述
    /// </summary>
    public string GetEmotionBehaviorDescription()
    {
        return CurrentEmotion switch
        {
            EmotionState.Happy => "心情很好，想和其他机器人互动",
            EmotionState.Excited => "非常兴奋，动作变得轻快",
            EmotionState.Sad => "心情低落，想一个人待着",
            EmotionState.Angry => "很生气，可能会冲撞其他机器人",
            EmotionState.Scared => "感到害怕，想逃离",
            EmotionState.Curious => "很好奇，想要探索",
            EmotionState.Bored => "很无聊，想找点乐子",
            EmotionState.Sleepy => "很困，想休息",
            _ => "状态平静"
        };
    }

    /// <summary>
    /// 触发情绪变化的事件
    /// </summary>
    public void TriggerEmotionEvent(EmotionTrigger trigger, Robot? otherRobot = null)
    {
        switch (trigger)
        {
            case EmotionTrigger.UserChat:
                SetEmotion(EmotionState.Happy, 70, 360, trigger);
                break;

            case EmotionTrigger.RobotChat:
                if (CurrentEmotion != EmotionState.Angry)
                    SetEmotion(EmotionState.Curious, 60, 300, trigger);
                break;

            case EmotionTrigger.PhysicalHit:
                if (EmotionIntensity > 50)
                    SetEmotion(EmotionState.Angry, EmotionIntensity, 240, trigger);
                else
                    SetEmotion(EmotionState.Scared, 60, 180, trigger);
                break;

            case EmotionTrigger.Exploring:
                if (CurrentEmotion == EmotionState.Bored || CurrentEmotion == EmotionState.Sleepy)
                    SetEmotion(EmotionState.Curious, 50, 300, trigger);
                break;

            case EmotionTrigger.Resting:
                if (EmotionIntensity < 30)
                    SetEmotion(EmotionState.Sleepy, 40, 600, trigger);
                break;
        }
    }
}
