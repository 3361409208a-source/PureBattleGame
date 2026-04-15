using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PureBattleGame.Games.CockroachPet;

public class ChatMessage
{
    public string role { get; set; } = "";
    public string content { get; set; } = "";

    public ChatMessage() { }
    public ChatMessage(string r, string c) { role = r; content = c; }
}

public class SocialMessage
{
    public string sender { get; set; } = "";
    public string content { get; set; } = "";

    public SocialMessage() { }
    public SocialMessage(string s, string c) { sender = s; content = c; }
}

public partial class Robot
{
    // 社交交互
    public string ChatText { get; set; } = "";
    private string _fullChatText = "";
    private int _streamCounter = 0;
    public int ChatTimer { get; set; } = 0;
    public List<ChatMessage> ChatHistory { get; set; } = new();
    public List<SocialMessage> SocialHistory { get; set; } = new();
    public bool IsAiSpeaking { get; set; } = false;
    public bool LogSocialInteractions { get; set; } = true;
    public bool CurseMode { get; set; } = false; // 骂人模式开关
    public Robot? MeetingTarget { get; set; }
    public int MeetingTimer { get; set; } = 0;
    public int SocialCooldown { get; set; } = 0;
    public Robot? FollowingTarget { get; set; }
    public int FollowTimer { get; set; } = 0;

    // 自定义词库
    public List<string> CustomPhrases { get; set; } = new List<string>();
    private int _customPhraseTimer = 300;

    public void InteractWith(Robot other)
    {
        // 如果有怪物目标，优先攻击怪物，不进行社交互动
        if (MonsterTarget != null && MonsterTarget.IsActive && !MonsterTarget.IsDead)
            return;

        if (SocialCooldown > 0 || other.SocialCooldown > 0) return;
        if (IsBusy || other.IsBusy) return;

        float dx = other.X - X;
        float dy = other.Y - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        int aliveCount = PetForm.Instance?.GetRobots().Count(r => !r.IsDead && r.IsVisible && r.IsActive) ?? 0;

        if (aliveCount % 2 != 0)
        {
            // 奇数存活 - 远程狙击模式
            if (dist < 100)
            {
                PerformPush(other);
                SocialCooldown = 45;
                other.SocialCooldown = 45;
                if (ShootCooldown == 0) LaunchRemoteAttack(other);
                SetBark("人太多了，离我远点射击！", 60);
            }
            else if (dist < 600 && ShootCooldown == 0)
            {
                int chance = IsWeaponMaster ? 40 : 20;
                if (Rand.Next(100) < chance)
                {
                    LaunchRemoteAttack(other);
                    SocialCooldown = 60;
                }
            }
        }
        else
        {
            // 偶数存活 - 近身格斗
            if (dist < 80)
            {
                StartDuel(other);
                SocialCooldown = 100;
                other.SocialCooldown = 100;
                SetBark(aliveCount == 2 ? "这是最后的清算！💥" : "找到对手了，来格斗吧！", 80);
            }
            else
            {
                if (ChaseTimer <= 0 && FollowingTarget == null && Rand.Next(100) < 15)
                {
                    ChasingTarget = other;
                    ChaseTimer = 350;
                    SetBark($"锁定目标：{other.Name}！", 60);
                }

                if (dist > 150 && ShootCooldown == 0 && Rand.Next(100) < 15)
                {
                    LaunchRemoteAttack(other);
                    SocialCooldown = 30;
                }
            }
        }
    }

    private void UpdateSocialLogic()
    {
        if (FollowTimer > 0 && FollowingTarget != null && FollowingTarget.IsActive)
        {
            if (FollowingTarget.IsDead)
            {
                FollowTimer = 0;
                FollowingTarget = null;
            }
            else
            {
                FollowTimer--;
                float targetDx = FollowingTarget.X - X;
                float targetDy = FollowingTarget.Y - Y;
                float dist = (float)Math.Sqrt(targetDx * targetDx + targetDy * targetDy);
                if (dist > 50)
                {
                    Dx = (Dx * 0.95f) + (targetDx / dist * 0.1f * SpeedMultiplier);
                    Dy = (Dy * 0.95f) + (targetDy / dist * 0.1f * SpeedMultiplier);
                }
                if (FollowTimer == 0) FollowingTarget = null;
            }
        }

        if (MeetingTimer > 0)
        {
            MeetingTimer--;
            if (MeetingTimer == 0) MeetingTarget = null;
        }

        if (SocialCooldown > 0) SocialCooldown--;
    }

    private void UpdateCustomPhrases()
    {
        if (CustomPhrases.Count == 0 || !IsActive || IsBusy) return;

        if (_customPhraseTimer > 0)
        {
            _customPhraseTimer--;
        }
        else
        {
            if (Rand.Next(100) < 30)
            {
                string phrase = CustomPhrases[Rand.Next(CustomPhrases.Count)];
                SetBark(phrase, 120);
                Console.WriteLine($"[CustomPhrase] {Name} 说: {phrase}");
            }
            _customPhraseTimer = Rand.Next(180, 600);
        }
    }

    public async Task SendUserMessage(string message)
    {
        if (_isThinking) return;

        OnChatMessageReceived?.Invoke("user", message, "");
        ChatHistory.Add(new ChatMessage("user", message));
        if (ChatHistory.Count > 10) ChatHistory.RemoveAt(0);

        // 用户聊天触发开心情绪
        TriggerEmotionEvent(EmotionTrigger.UserChat);

        _isThinking = true;
        IsAiSpeaking = true;
        _fullChatText = "想着呢...";
        ChatText = "";
        _streamCounter = 0;
        ChatTimer = 60;

        string selfImproCtx = _selfImproving.GetHotMemory() + "\n" + _selfImproving.GetSoulSteering();

        if (message.Contains("不对", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("错了", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("应该是", StringComparison.OrdinalIgnoreCase))
        {
            _selfImproving.LogCorrection("User signaled mistake", message);
        }

        AiService.ChatResponse result = await AiService.GetChatResponseAsync(Name, Personality, message, ChatHistory, InternalGuidelines, LearnedInsights, GetSkillsDescription(), selfImproCtx, GetEmotionName(), GetPersonalityPrompt(), CurseMode);
        string thought = result.Thought;
        string response = result.Answer;

        _isThinking = false;
        ChatHistory.Add(new ChatMessage("assistant", response));
        if (ChatHistory.Count > 10) ChatHistory.RemoveAt(0);

        IsAiSpeaking = true;
        _fullChatText = response;
        ChatText = "";
        _streamCounter = 0;
        ChatTimer = 180 + response.Length * 5;
        OnChatMessageReceived?.Invoke("assistant", response, thought);

        bool isMemoryCommand = message.Contains("记住") || message.Contains("叫我") || message.Contains("身份") || message.Contains("我的名字");

        if (isMemoryCommand)
        {
            _ = ReflectAsync();
        }
        else if (ChatHistory.Count(m => m.role == "assistant") % 3 == 0)
        {
            _ = ReflectAsync();
        }
    }

    private void LogSocial(string sender, string content, bool broadcast = true)
    {
        string log = $"[SOCIAL] {sender}: {content}";
        SocialHistory.Add(new SocialMessage(sender, content));
        if (SocialHistory.Count > 20) SocialHistory.RemoveAt(0);

        if (LogSocialInteractions)
        {
            NotifyOutput(log);

            if (broadcast)
            {
                Color chatColor = sender == Name ? PrimaryColor : Color.SkyBlue;
                TerminalManagerForm.Instance.BroadcastToWorld(sender, content, chatColor);
            }
        }
    }
}
