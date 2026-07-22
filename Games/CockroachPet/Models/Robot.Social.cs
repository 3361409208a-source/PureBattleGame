using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using PureBattleGame.Core;

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
    public bool CurseMode { get; set; } = false; // 吐槽/骂人模式开关
    public Robot? MeetingTarget { get; set; }
    public int MeetingTimer { get; set; } = 0;
    public int SocialCooldown { get; set; } = 0;
    private DateTime _lastAiFightTime = DateTime.MinValue;
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

        string actionMode = SettingsManager.Current.ActionInteractionMode;
        string langMode = SettingsManager.Current.LanguageInteractionMode;

        if (actionMode == "和平相处")
        {
            // 和平相处模式：不决斗、不轰炸、不推搡
            if (dist < 120)
            {
                SocialCooldown = 120;
                other.SocialCooldown = 120;
                SpecialState = "HAPPY";
                other.SpecialState = "HEART_EYES";
                SpecialStateTimer = 60;
                other.SpecialStateTimer = 60;

                string[] peacefulBarks = langMode switch
                {
                    "科幻极客" => new[] { $"{other.Name}，检测到协议对齐！🤖", $"{other.Name}，量子通信链路已建立！✨", $"{other.Name}，同频共振中...⚡" },
                    "友好哲理" => new[] { $"{other.Name}，很高兴遇见你！🌸", $"{other.Name}，漫漫星河，幸甚有你。✨", $"{other.Name}，今天也是充满希望的一天~" },
                    "幽默搞笑" => new[] { $"{other.Name}，好巧啊，你也在这遛弯？", $"{other.Name}，吃了吗？没吃吃我一脚！开玩笑的~😜" },
                    _ => new[] { $"{other.Name}，哈啰！小伙伴！", $"{other.Name}，一起在桌面上逛逛吧~" }
                };

                SetBark(peacefulBarks[Rand.Next(peacefulBarks.Length)], 80);
                if (EnableAiThinking) _ = TriggerAiFightAsync(other);
            }
            return;
        }

        if (actionMode == "近身格斗")
        {
            if (dist < 90)
            {
                StartDuel(other);
                SocialCooldown = 100;
                other.SocialCooldown = 100;
                SetBark($"对决开始！{other.Name}！", 80);
                if (EnableAiThinking) _ = TriggerAiFightAsync(other);
            }
            else if (dist < 300 && ChaseTimer <= 0)
            {
                ChasingTarget = other;
                ChaseTimer = 250;
            }
            return;
        }

        if (actionMode == "远程狙击")
        {
            if (dist < 80)
            {
                PerformPush(other);
                SocialCooldown = 45;
                other.SocialCooldown = 45;
                if (ShootCooldown == 0) LaunchRemoteAttack(other);
                if (EnableAiThinking) _ = TriggerAiFightAsync(other);
            }
            else if (dist < 600 && ShootCooldown == 0)
            {
                if (Rand.Next(100) < 30)
                {
                    LaunchRemoteAttack(other);
                    SocialCooldown = 60;
                    if (EnableAiThinking) _ = TriggerAiFightAsync(other);
                }
            }
            return;
        }

        // 近远交替（默认）
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
                if (EnableAiThinking) _ = TriggerAiFightAsync(other);
            }
            else if (dist < 600 && ShootCooldown == 0)
            {
                int chance = IsWeaponMaster ? 40 : 20;
                if (Rand.Next(100) < chance)
                {
                    LaunchRemoteAttack(other);
                    SocialCooldown = 60;
                    if (EnableAiThinking) _ = TriggerAiFightAsync(other);
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
                if (EnableAiThinking) _ = TriggerAiFightAsync(other);
            }
            else
            {
                if (ChaseTimer <= 0 && FollowingTarget == null && Rand.Next(100) < 15)
                {
                    ChasingTarget = other;
                    ChaseTimer = 350;
                }

                if (dist > 150 && ShootCooldown == 0 && Rand.Next(100) < 15)
                {
                    LaunchRemoteAttack(other);
                    SocialCooldown = 30;
                    if (EnableAiThinking) _ = TriggerAiFightAsync(other);
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

    public async Task TriggerAiFightAsync(Robot target)
    {
        if (_isThinking || target == null || !target.IsActive || target.IsDead || IsDead) return;

        if ((DateTime.Now - _lastAiFightTime).TotalSeconds < 4) return;

        string apiKey = AiService.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        _isThinking = true;
        _lastAiFightTime = DateTime.Now;
        try
        {
            var history = SocialHistory.Select(h => (h.sender, h.content)).ToList();
            var lastMsg = history.LastOrDefault();
            string lastInsult = string.IsNullOrEmpty(lastMsg.content) ? "在干嘛呢？" : lastMsg.content;

            string langMode = SettingsManager.Current.LanguageInteractionMode;

            string fightReply = await AiService.GetFightResponseAsync(
                Name, GetPersonalityName(), lastInsult, history, target.Name, langMode
            );

            if (!string.IsNullOrWhiteSpace(fightReply))
            {
                SetBark(fightReply, 140);
                TerminalManagerForm.Instance?.BroadcastToWorld(Name, fightReply, PrimaryColor);
                SocialHistory.Add(new SocialMessage(Name, fightReply));

                if (Rand.Next(100) < 70)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1800);
                        if (target.IsActive && !target.IsDead && !IsDead)
                        {
                            string counterReply = await AiService.GetFightResponseAsync(
                                target.Name, target.GetPersonalityName(), fightReply, history, Name, langMode
                            );
                            if (!string.IsNullOrWhiteSpace(counterReply))
                            {
                                target.SetBark(counterReply, 140);
                                TerminalManagerForm.Instance?.BroadcastToWorld(target.Name, counterReply, target.PrimaryColor);
                                target.SocialHistory.Add(new SocialMessage(target.Name, counterReply));
                            }
                        }
                    });
                }
            }
        }
        catch { }
        finally
        {
            _isThinking = false;
        }
    }
}
