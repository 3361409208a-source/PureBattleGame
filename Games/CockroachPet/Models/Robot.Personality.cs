namespace PureBattleGame.Games.CockroachPet;

/// <summary>
/// 机器人类型枚举
/// </summary>
public enum RobotPersonalityType
{
    Friendly,   // 友好 - 主动接近其他机器人，喜欢社交
    Shy,        // 害羞 - 避开其他机器人，喜欢待在角落
    Rebel,      // 叛逆 - 经常冲撞其他机器人，有攻击性
    Humorous,   // 幽默 - 经常说笑话，行为滑稽
    Serious,    // 严肃 - 行为稳重，很少说话
    Curious,    // 好奇 - 频繁探索，喜欢跟随
    Lazy,       // 懒惰 - 移动缓慢，经常休息
    Energetic   // 精力充沛 - 移动快速，很少休息
}

/// <summary>
/// 个性特征值 - 影响机器人行为的参数
/// </summary>
public class PersonalityTraits
{
    // 社交距离偏好 (0-1, 越高越喜欢独处)
    public float SocialDistance { get; set; } = 0.5f;

    // 移动速度偏好 (0.5-2.0)
    public float MovementSpeed { get; set; } = 1.0f;

    // 休息频率 (0-1, 越高越频繁休息)
    public float RestFrequency { get; set; } = 0.3f;

    // 说话频率 (0-1, 越高越喜欢说话)
    public float TalkFrequency { get; set; } = 0.5f;

    // 攻击性 (0-1, 越高越喜欢攻击)
    public float Aggression { get; set; } = 0.2f;

    // 好奇心 (0-1, 越高越喜欢探索)
    public float Curiosity { get; set; } = 0.5f;

    // 情绪波动性 (0-1, 越高情绪变化越快)
    public float EmotionVolatility { get; set; } = 0.5f;

    // 社交主动性 (0-1, 越高越主动社交)
    public float SocialInitiative { get; set; } = 0.5f;

    // 对话风格描述
    public string ChatStyle { get; set; } = "normal";

    // AI 提示词特征描述
    public string AiPromptTraits { get; set; } = "友好且乐于助人";
}

/// <summary>
/// 个性系统 - 管理机器人的个性类型和行为特征
/// </summary>
public partial class Robot
{
    // 当前个性类型
    public RobotPersonalityType PersonalityType { get; set; } = RobotPersonalityType.Friendly;

    // 个性特征值
    public PersonalityTraits Traits { get; set; } = new();

    // 个性名称映射
    public string GetPersonalityName()
    {
        return PersonalityType switch
        {
            RobotPersonalityType.Friendly => "友好",
            RobotPersonalityType.Shy => "害羞",
            RobotPersonalityType.Rebel => "叛逆",
            RobotPersonalityType.Humorous => "幽默",
            RobotPersonalityType.Serious => "严肃",
            RobotPersonalityType.Curious => "好奇",
            RobotPersonalityType.Lazy => "懒惰",
            RobotPersonalityType.Energetic => "精力",
            _ => "普通"
        };
    }

    // 个性图标
    public string GetPersonalityEmoji()
    {
        return PersonalityType switch
        {
            RobotPersonalityType.Friendly => "🤝",
            RobotPersonalityType.Shy => "🙈",
            RobotPersonalityType.Rebel => "😈",
            RobotPersonalityType.Humorous => "😄",
            RobotPersonalityType.Serious => "🤔",
            RobotPersonalityType.Curious => "👀",
            RobotPersonalityType.Lazy => "😴",
            RobotPersonalityType.Energetic => "⚡",
            _ => "🤖"
        };
    }

    /// <summary>
    /// 初始化个性特征值
    /// </summary>
    public void InitializePersonalityTraits()
    {
        Traits = PersonalityType switch
        {
            RobotPersonalityType.Friendly => new PersonalityTraits
            {
                SocialDistance = 0.2f,
                MovementSpeed = 1.0f,
                RestFrequency = 0.3f,
                TalkFrequency = 0.7f,
                Aggression = 0.1f,
                Curiosity = 0.6f,
                EmotionVolatility = 0.4f,
                SocialInitiative = 0.8f,
                ChatStyle = "friendly",
                AiPromptTraits = "非常友好，喜欢交朋友，说话温暖亲切"
            },
            RobotPersonalityType.Shy => new PersonalityTraits
            {
                SocialDistance = 0.8f,
                MovementSpeed = 0.8f,
                RestFrequency = 0.4f,
                TalkFrequency = 0.2f,
                Aggression = 0.05f,
                Curiosity = 0.4f,
                EmotionVolatility = 0.6f,
                SocialInitiative = 0.1f,
                ChatStyle = "shy",
                AiPromptTraits = "害羞内向，不善言辞，喜欢独处，说话轻柔"
            },
            RobotPersonalityType.Rebel => new PersonalityTraits
            {
                SocialDistance = 0.5f,
                MovementSpeed = 1.3f,
                RestFrequency = 0.2f,
                TalkFrequency = 0.5f,
                Aggression = 0.8f,
                Curiosity = 0.7f,
                EmotionVolatility = 0.7f,
                SocialInitiative = 0.4f,
                ChatStyle = "rebel",
                AiPromptTraits = "叛逆不羁，有攻击性，喜欢挑战，说话直接"
            },
            RobotPersonalityType.Humorous => new PersonalityTraits
            {
                SocialDistance = 0.3f,
                MovementSpeed = 1.1f,
                RestFrequency = 0.3f,
                TalkFrequency = 0.9f,
                Aggression = 0.1f,
                Curiosity = 0.8f,
                EmotionVolatility = 0.8f,
                SocialInitiative = 0.7f,
                ChatStyle = "humorous",
                AiPromptTraits = "幽默风趣，喜欢开玩笑，行为滑稽，说话搞笑"
            },
            RobotPersonalityType.Serious => new PersonalityTraits
            {
                SocialDistance = 0.4f,
                MovementSpeed = 0.9f,
                RestFrequency = 0.3f,
                TalkFrequency = 0.2f,
                Aggression = 0.2f,
                Curiosity = 0.5f,
                EmotionVolatility = 0.2f,
                SocialInitiative = 0.3f,
                ChatStyle = "serious",
                AiPromptTraits = "严肃认真，深思熟虑，说话稳重，言简意赅"
            },
            RobotPersonalityType.Curious => new PersonalityTraits
            {
                SocialDistance = 0.3f,
                MovementSpeed = 1.2f,
                RestFrequency = 0.2f,
                TalkFrequency = 0.6f,
                Aggression = 0.1f,
                Curiosity = 1.0f,
                EmotionVolatility = 0.6f,
                SocialInitiative = 0.6f,
                ChatStyle = "curious",
                AiPromptTraits = "充满好奇心，喜欢探索，喜欢提问，说话轻快"
            },
            RobotPersonalityType.Lazy => new PersonalityTraits
            {
                SocialDistance = 0.4f,
                MovementSpeed = 0.6f,
                RestFrequency = 0.8f,
                TalkFrequency = 0.3f,
                Aggression = 0.1f,
                Curiosity = 0.3f,
                EmotionVolatility = 0.3f,
                SocialInitiative = 0.2f,
                ChatStyle = "lazy",
                AiPromptTraits = "懒惰散漫，喜欢休息，不想动弹，说话慵懒"
            },
            RobotPersonalityType.Energetic => new PersonalityTraits
            {
                SocialDistance = 0.3f,
                MovementSpeed = 1.5f,
                RestFrequency = 0.1f,
                TalkFrequency = 0.8f,
                Aggression = 0.3f,
                Curiosity = 0.9f,
                EmotionVolatility = 0.9f,
                SocialInitiative = 0.9f,
                ChatStyle = "energetic",
                AiPromptTraits = "精力充沛，活力四射，停不下来，说话快速"
            },
            _ => new PersonalityTraits()
        };
    }

    /// <summary>
    /// 设置个性类型
    /// </summary>
    public void SetPersonality(RobotPersonalityType type)
    {
        PersonalityType = type;
        InitializePersonalityTraits();

        // 根据个性初始化一些行为参数
        SpeedMultiplier = Traits.MovementSpeed;
        AiThoughtFrequency = (int)(AiThoughtFrequency / Traits.TalkFrequency);

        System.Diagnostics.Debug.WriteLine($"[Robot {Name}] 个性设定为: {GetPersonalityName()} {GetPersonalityEmoji()}");
    }

    /// <summary>
    /// 应用个性到行为决策
    /// </summary>
    private void ApplyPersonalityToBehavior()
    {
        if (Traits == null) return;

        // 影响移动速度
        if (IsMoving)
        {
            Dx *= Traits.MovementSpeed;
            Dy *= Traits.MovementSpeed;
        }

        // 害羞个性：避开其他机器人
        if (PersonalityType == RobotPersonalityType.Shy)
        {
            AvoidOtherRobots();
        }

        // 好奇个性：主动接近探索
        if (PersonalityType == RobotPersonalityType.Curious && Rand.Next(100) < 20)
        {
            ApproachRandomRobot();
        }

        // 懒惰个性：更频繁休息
        if (PersonalityType == RobotPersonalityType.Lazy && IsMoving && Rand.Next(100) < 10)
        {
            IsMoving = false;
            RestTimer = 120 + Rand.Next(120);
        }

        // 叛逆个性：随机冲撞
        if (PersonalityType == RobotPersonalityType.Rebel && Rand.Next(100) < 5)
        {
            ChargeAtRandomRobot();
        }
    }

    /// <summary>
    /// 避开其他机器人（害羞个性）
    /// </summary>
    private void AvoidOtherRobots()
    {
        var robots = PetForm.Instance?.GetRobots();
        if (robots == null) return;

        foreach (var r in robots)
        {
            if (r != this && r.IsActive && r.IsVisible)
            {
                float dx = X - r.X;
                float dy = Y - r.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (dist < 100 && dist > 0)
                {
                    // 避开
                    Dx += (dx / dist) * 2;
                    Dy += (dy / dist) * 2;
                }
            }
        }
    }

    /// <summary>
    /// 接近随机机器人（好奇个性）
    /// </summary>
    private void ApproachRandomRobot()
    {
        var robots = PetForm.Instance?.GetRobots();
        if (robots == null || robots.Count == 0) return;

        var target = robots[Rand.Next(robots.Count)];
        if (target != this && target.IsActive)
        {
            float dx = target.X - X;
            float dy = target.Y - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist > 50)
            {
                Dx = (dx / dist) * 3;
                Dy = (dy / dist) * 3;
            }
        }
    }

    /// <summary>
    /// 冲向随机机器人（叛逆个性）
    /// </summary>
    private void ChargeAtRandomRobot()
    {
        var robots = PetForm.Instance?.GetRobots();
        if (robots == null || robots.Count == 0) return;

        var target = robots[Rand.Next(robots.Count)];
        if (target != this && target.IsActive)
        {
            float dx = target.X - X;
            float dy = target.Y - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist > 0)
            {
                Dx = (dx / dist) * 8;
                Dy = (dy / dist) * 8;
                SetBark("看招！", 40);
            }
        }
    }

    /// <summary>
    /// 获取个性相关的AI提示词
    /// </summary>
    public string GetPersonalityPrompt()
    {
        return Traits?.AiPromptTraits ?? "友好且乐于助人";
    }

    /// <summary>
    /// 获取个性的行为描述
    /// </summary>
    public string GetPersonalityBehaviorDescription()
    {
        return PersonalityType switch
        {
            RobotPersonalityType.Friendly => "喜欢交朋友，主动接近其他机器人",
            RobotPersonalityType.Shy => "害羞内向，避开其他机器人",
            RobotPersonalityType.Rebel => "叛逆不羁，喜欢冲撞挑衅",
            RobotPersonalityType.Humorous => "幽默滑稽，喜欢开玩笑",
            RobotPersonalityType.Serious => "严肃认真，行为稳重",
            RobotPersonalityType.Curious => "充满好奇，喜欢探索跟随",
            RobotPersonalityType.Lazy => "懒惰散漫，经常休息",
            RobotPersonalityType.Energetic => "精力充沛，快速移动",
            _ => "行为普通"
        };
    }

    /// <summary>
    /// 获取个性对应的颜色
    /// </summary>
    public Color GetPersonalityColor()
    {
        return PersonalityType switch
        {
            RobotPersonalityType.Friendly => Color.FromArgb(100, 200, 100),
            RobotPersonalityType.Shy => Color.FromArgb(180, 150, 200),
            RobotPersonalityType.Rebel => Color.FromArgb(200, 80, 80),
            RobotPersonalityType.Humorous => Color.FromArgb(255, 200, 50),
            RobotPersonalityType.Serious => Color.FromArgb(100, 100, 150),
            RobotPersonalityType.Curious => Color.FromArgb(100, 200, 255),
            RobotPersonalityType.Lazy => Color.FromArgb(150, 150, 150),
            RobotPersonalityType.Energetic => Color.FromArgb(255, 150, 50),
            _ => Color.White
        };
    }

    /// <summary>
    /// 随机选择一个个性
    /// </summary>
    public static RobotPersonalityType GetRandomPersonality(Random rand)
    {
        var personalities = Enum.GetValues<RobotPersonalityType>();
        return personalities[rand.Next(personalities.Length)];
    }
}
