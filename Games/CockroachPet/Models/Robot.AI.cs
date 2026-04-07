using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PureBattleGame.Games.CockroachPet;

public partial class Robot
{
    // AI 独立体状态
    public int AiThoughtTimer { get; set; } = 1200;
    private bool _isThinking = false;
    public bool IsThinking => _isThinking;
    public string LastAiThought { get; set; } = "";
    public string Personality { get; set; } = "Friendly";
    public event Action<string, string, string>? OnChatMessageReceived;

    // 自我意识成长体系
    public double ConsciousnessLevel { get; set; } = 1.0;
    public int Experience { get; set; } = 0;
    public List<string> LearnedInsights { get; set; } = new List<string>();
    public string InternalGuidelines { get; set; } = "";
    public event Action<Robot>? OnGrowthUpdated;

    // 是否忙碌
    public bool IsBusy => IsDead || PhysicalAction != "NONE" || PullingMe != null || _isThinking || IsBeingThrown;

    private SelfImprovingManager _selfImproving;

    private void UpdateAiThinking()
    {
        if (_isThinking || !IsActive || !EnableAiThinking) return;

        if (AiThoughtTimer > 0)
        {
            AiThoughtTimer--;
        }
        else
        {
            int targetFrames = AiThoughtFrequency * 30;
            int jitter = (int)(targetFrames * 0.2);
            AiThoughtTimer = targetFrames + Rand.Next(-jitter, jitter);

            TriggerAiThought();
        }
    }

    private async void TriggerAiThought()
    {
        _isThinking = true;

        string currentActivity = IsMoving ? (FollowingTarget != null ? "Following friend" : "Exploring") : "Resting";
        if (IsWarning) currentActivity = "Alerted: " + AlertMessage;

        var thought = await AiService.GetThoughtAsync(Name, StatusMessage, currentActivity, Personality,
            GetEmotionName(), EmotionIntensity, GetPersonalityPrompt());

        _isThinking = false;

        if (!string.IsNullOrEmpty(thought) && IsActive)
        {
            IsAiSpeaking = true;
            LastAiThought = thought;
            _fullChatText = thought;
            ChatText = "";
            _streamCounter = 0;
            ChatTimer = 180 + thought.Length * 5;
            System.Diagnostics.Debug.WriteLine($"[Robot {Name}] AI Thought: {thought}");
        }
    }

    public async Task ReflectAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[Robot {Name}] 正在进入深度自省...");

        string oldChat = ChatText;
        _fullChatText = "正在进化思想...";
        ChatText = "";
        ChatTimer = 100;

        var result = await AiService.ReflectOnHistoryAsync(Name, Personality, ChatHistory, LearnedInsights);

        if (!string.IsNullOrEmpty(result.Insight))
        {
            LearnedInsights.Add(result.Insight);
            if (LearnedInsights.Count > 5) LearnedInsights.RemoveAt(0);

            Experience += 10;
            if (Experience >= 100)
            {
                Experience = 0;
                ConsciousnessLevel += 0.5;
            }

            InternalGuidelines = result.NewGuidelines;

            _selfImproving.UpdateMemory("Patterns", result.Insight);

            if (result.Memories != null)
            {
                foreach (var memo in result.Memories)
                {
                    _selfImproving.UpdateMemory("Preferences", memo);
                }
            }

            var skillKeys = Skills.Keys.ToList();
            if (skillKeys.Count > 0)
            {
                var randomSkill = skillKeys[Rand.Next(skillKeys.Count)];
                Skills[randomSkill].GainExperience(50);

                SkillManager.SaveRobotSkills(this);
                System.Diagnostics.Debug.WriteLine($"[Robot {Name}] 进化成功！新感悟: {result.Insight}, 技能{randomSkill}+50XP");
            }

            _fullChatText = $"（记忆已更新：{result.Insight}）";
            ChatText = "";
            ChatTimer = 120;

            OnGrowthUpdated?.Invoke(this);
        }

        ChatTimer = 0;
    }
}
