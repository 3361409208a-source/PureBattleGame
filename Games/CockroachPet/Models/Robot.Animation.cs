using System;
using System.Drawing;

namespace PureBattleGame.Games.CockroachPet;

public partial class Robot
{
    // 聊天和表情属性
    public string? ChatMessage { get; set; }
    public string? EmojiBubble { get; set; }

    // 特殊动画状态
    public string SpecialState { get; set; } = "NORMAL";
    public int SpecialStateTimer { get; set; } = 0;
    public float ShakingOffset { get; set; } = 0;
    public float RotationAngle { get; set; } = 0;
    public int EmojiBubbleTimer { get; set; } = 0;
    public string CurrentEmoji { get; set; } = "";

    // 受击反馈
    public int DamageFeedbackTimer { get; set; } = 0;
    public string LastDamageText { get; set; } = "";
    public int DamageTextTimer { get; set; } = 0;

    // 状态计时器
    public int StunTimer { get; set; } = 0;
    public int SlowTimer { get; set; } = 0;
    public int BlindTimer { get; set; } = 0;
    public int AggressionTimer { get; set; } = 0;

    private void UpdateSpecialAnimations()
    {
        if (SpecialStateTimer > 0)
        {
            SpecialStateTimer--;
            if (SpecialState == "SPINNING")
            {
                RotationAngle += 15f;
            }
            else if (SpecialState == "SHAKING")
            {
                ShakingOffset = (float)(Math.Sin(SpecialStateTimer * 1.5) * 4);
            }

            if (SpecialStateTimer == 0)
            {
                SpecialState = "NORMAL";
                RotationAngle = 0;
                ShakingOffset = 0;
            }
        }
        else
        {
            // 反“渔翁得利”逻辑（只在没有怪物目标时触发）
            if (MonsterTarget == null && DuelTimer == 0 && ChaseTimer == 0 && FollowingTarget == null && !IsDead)
            {
                AggressionTimer++;
                if (AggressionTimer > 600)
                {
                    var targets = PetForm.Instance?.GetRobots()
                                   .Where(r => r != this && !r.IsDead && r.IsVisible)
                                   .OrderByDescending(r => r.HP)
                                   .ToList();
                    if (targets != null && targets.Count > 0)
                    {
                        ChasingTarget = targets[0];
                        ChaseTimer = 300;
                        SetBark($"{ChasingTarget.Name}，别在那划水了！过两招！💢", 100);
                        AggressionTimer = 0;
                    }
                }
            }
            else
            {
                AggressionTimer = 0;
            }

            if (Rand.Next(1000) < 5)
            {
                string[] states = { "HEART_EYES", "SPINNING", "BLUSH", "SLEEPY", "ANGRY" };
                SpecialState = states[Rand.Next(states.Length)];
                SpecialStateTimer = Rand.Next(60, 180);
            }
        }

        // 武器大师视觉增强
        if (IsWeaponMaster && SpecialState == "NORMAL")
        {
            SpecialState = "ANGRY";
            SpecialStateTimer = 60;
        }

        // 表情气泡逻辑
        if (EmojiBubbleTimer > 0)
        {
            EmojiBubbleTimer--;
        }
        else if (!IsAiSpeaking && Rand.Next(2000) < 3)
        {
            string[] emojis = { "☕", "💡", "🎮", "🎵", "🍕", "⭐", "🔥", "💨" };
            CurrentEmoji = emojis[Rand.Next(emojis.Length)];
            EmojiBubbleTimer = Rand.Next(60, 120);
        }

        // 更新计时器
        if (StunTimer > 0) StunTimer--;
        if (SlowTimer > 0) SlowTimer--;
        if (BlindTimer > 0) BlindTimer--;
        if (ChatTimer > 0) ChatTimer--;
        if (DamageTextTimer > 0) DamageTextTimer--;
        if (DamageFeedbackTimer > 0) DamageFeedbackTimer--;

        // 自动回血逻辑
        if (DuelTimer == 0 && DuelTarget == null && !IsDead && HP < MaxHP && Rand.Next(100) < 5)
        {
            HP = Math.Min(MaxHP, HP + 1);
        }
    }

    private void UpdateTentacles(bool idle)
    {
        float speed = idle ? 0.1f : 0.3f;
        for (int i = 0; i < 8; i++)
        {
            TentacleOffsets[i] += speed + Rand.NextFloat() * 0.1f;
        }
    }

    private void UpdateStreamingChat()
    {
        if (ChatText.Length < _fullChatText.Length)
        {
            _streamCounter++;
            if (_streamCounter >= 2)
            {
                ChatText = _fullChatText.Substring(0, ChatText.Length + 1);
                _streamCounter = 0;
            }
        }
        else if (IsAiSpeaking && ChatTimer == 0)
        {
            IsAiSpeaking = false;
        }
    }

    public void SetBark(string text, int duration = 90)
    {
        if (IsAiSpeaking) return;

        _fullChatText = text;
        ChatText = text;
        ChatTimer = duration;
    }

    private void UpdateRandomDirection()
    {
        ChangeDirectionTimer++;
        if (ChangeDirectionTimer > 120 && Rand.Next(100) < 5)
        {
            ChangeDirectionTimer = 0;
            double angle = Rand.NextDouble() * Math.PI * 2;
            float speed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
            Dx = (float)Math.Cos(angle) * speed;
            Dy = (float)Math.Sin(angle) * speed;
        }
    }

    private void HandleDeath()
    {
        IsDead = true;
        IsMoving = false;
        SpecialState = "NORMAL";
        RotationAngle = 90f;
        SetBark("核心崩溃...系统下线 💀", 200);
        Dx = 0; Dy = 0;
    }
}
