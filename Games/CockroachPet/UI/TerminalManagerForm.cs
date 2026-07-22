using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using PureBattleGame.Core;

namespace PureBattleGame.Games.CockroachPet;

public class TerminalManagerForm : WebUIHostForm
{
    private static TerminalManagerForm? _instance;
    private System.Windows.Forms.Timer _pushTimer = null!;
    private List<SocialMessage> _globalWorldHistory = new();
    private HashSet<Robot> _subscribedRobots = new();

    public static TerminalManagerForm Instance
    {
        get
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new TerminalManagerForm();
            }
            return _instance;
        }
    }

    private TerminalManagerForm() : base("social-hub", "💬 机器人社交中心 & 控制台 | Robot Social Hub")
    {
        this.Size = new Size(1150, 750);
        this.MinimumSize = new Size(850, 620);

        _pushTimer = new System.Windows.Forms.Timer
        {
            Interval = 800
        };
        _pushTimer.Tick += (s, e) => PushRealtimeData();
        _pushTimer.Start();
    }

    protected override void OnBridgeReady(WebUIBridge bridge)
    {
        EnsureSubscribedToRobots();

        // 1. 获取所有机器人列表
        bridge.RegisterSyncHandler("getRobots", payload => GetRobotDTOs());

        // 2. 获取系统状态 Badges
        bridge.RegisterSyncHandler("getStats", payload => GetStatsDTO());

        // 3. 获取世界广播历史
        bridge.RegisterSyncHandler("getWorldHistory", payload => _globalWorldHistory);

        // 4. 全局广播 (发送到世界频道并触发活跃机器人公开回应)
        bridge.RegisterSyncHandler("sendWorldBroadcast", payload =>
        {
            if (payload.TryGetProperty("message", out var msgProp))
            {
                string msg = msgProp.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    BroadcastToWorld("管理员", msg, Color.Yellow);

                    var activeRobots = (PetForm.Instance?.GetRobots() ?? new List<Robot>())
                        .Where(r => r.IsActive && !r.IsDead).ToList();

                    if (activeRobots.Count > 0)
                    {
                        var responder = activeRobots[new Random().Next(activeRobots.Count)];
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            string langMode = SettingsManager.Current.LanguageInteractionMode;
                            var history = _globalWorldHistory.Select(m => (m.sender, m.content)).ToList();
                            string reply = await AiService.GetFightResponseAsync(
                                responder.Name, responder.GetPersonalityName(), msg,
                                history, "管理员", langMode
                            );

                            if (!string.IsNullOrWhiteSpace(reply))
                            {
                                responder.SetBark(reply, 120);
                                BroadcastToWorld(responder.Name, reply, responder.PrimaryColor);
                            }
                        });
                    }
                }
            }
            return true;
        });

        // 5. 单人对话发送
        bridge.RegisterSyncHandler("sendPrivateMessage", payload =>
        {
            if (payload.TryGetProperty("robotId", out var idProp) && payload.TryGetProperty("message", out var msgProp))
            {
                string robotId = idProp.GetString() ?? "";
                string msg = msgProp.GetString() ?? "";
                var target = PetForm.Instance?.GetRobots().FirstOrDefault(r => r.Name == robotId);
                if (target != null)
                {
                    _ = target.SendUserMessage(msg);
                }
            }
            return true;
        });

        // 6. 获取单人对话历史
        bridge.RegisterSyncHandler("getPrivateHistory", payload =>
        {
            if (payload.TryGetProperty("robotId", out var idProp))
            {
                string robotId = idProp.GetString() ?? "";
                var target = PetForm.Instance?.GetRobots().FirstOrDefault(r => r.Name == robotId);
                if (target != null)
                {
                    return target.ChatHistory.Select(c => new
                    {
                        role = c.role,
                        content = c.content
                    }).ToList();
                }
            }
            return new List<object>();
        });

        // 7. 触发启发
        bridge.RegisterSyncHandler("triggerInspiration", payload =>
        {
            string? robotId = null;
            if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("robotId", out var idProp))
            {
                robotId = idProp.GetString();
            }

            var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
            if (!string.IsNullOrEmpty(robotId))
            {
                var r = robots.FirstOrDefault(x => x.Name == robotId);
                if (r != null) _ = r.SendUserMessage("发表一下你现在的思考想法");
            }
            else
            {
                foreach (var r in robots)
                {
                    if (r.IsActive && !r.IsDead) _ = r.SendUserMessage("发表一下你现在的思考想法");
                }
            }
            return true;
        });

        // 8. 切换骂人模式
        bridge.RegisterSyncHandler("toggleCurseMode", payload =>
        {
            if (payload.TryGetProperty("robotId", out var idProp) && payload.TryGetProperty("enable", out var enableProp))
            {
                string robotId = idProp.GetString() ?? "";
                bool enable = enableProp.GetBoolean();
                var r = PetForm.Instance?.GetRobots().FirstOrDefault(x => x.Name == robotId);
                if (r != null) r.CurseMode = enable;
            }
            return true;
        });

        // 9. 获取系统设置
        bridge.RegisterSyncHandler("getSettings", payload => new
        {
            opacity = SettingsManager.Current.DefaultOpacity,
            hideNameAndPersonality = SettingsManager.Current.HideNameAndPersonality,
            curseModeByDefault = SettingsManager.Current.CurseModeByDefault,
            battleMode = SettingsManager.Current.BattleMode,
            languageMode = SettingsManager.Current.LanguageInteractionMode,
            actionMode = SettingsManager.Current.ActionInteractionMode,
            apiKey = PersistenceManager.GetApiKey(),
            homeUrl = SettingsManager.Current.HomeUrl
        });

        // 10. 保存系统设置
        bridge.RegisterSyncHandler("saveSettings", payload =>
        {
            if (payload.TryGetProperty("hideNameAndPersonality", out var hideProp))
                SettingsManager.Current.HideNameAndPersonality = hideProp.GetBoolean();
            if (payload.TryGetProperty("curseModeByDefault", out var curseProp))
                SettingsManager.Current.CurseModeByDefault = curseProp.GetBoolean();
            if (payload.TryGetProperty("battleMode", out var battleProp))
                SettingsManager.Current.BattleMode = battleProp.GetString() ?? "近远交替";
            if (payload.TryGetProperty("languageMode", out var langProp))
                SettingsManager.Current.LanguageInteractionMode = langProp.GetString() ?? "互骂吐槽";
            if (payload.TryGetProperty("actionMode", out var actProp))
            {
                string act = actProp.GetString() ?? "近远交替";
                SettingsManager.Current.ActionInteractionMode = act;
                SettingsManager.Current.BattleMode = act;
            }
            if (payload.TryGetProperty("apiKey", out var keyProp))
            {
                var appSet = PersistenceManager.LoadAppSettings();
                appSet.ApiKey = keyProp.GetString() ?? "";
                PersistenceManager.SaveAppSettings(appSet);
            }

            SettingsManager.Save();
            return true;
        });

        // 11. 控制面板快捷操作
        bridge.RegisterSyncHandler("spawnRobot", payload => {
            this.Invoke(() => PetForm.Instance?.SpawnRobotWithName());
            return true;
        });
        bridge.RegisterSyncHandler("quickSpawnRobot", payload => {
            string[] names = { "小八", "阿呆", "像素仔", "蓝灵", "红豆", "大眼", "触手大王", "碳基生物" };
            string name = names[new Random().Next(names.Length)];
            this.Invoke(() => PetForm.Instance?.SpawnRobot(name, -1, -1));
            return true;
        });
        bridge.RegisterSyncHandler("aiSpawnRobot", payload => {
            this.Invoke(() => PetForm.Instance?.ShowAiRobotGenerator());
            return true;
        });
        bridge.RegisterSyncHandler("pauseAllRobots", payload => {
            var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
            foreach (var r in robots) r.IsMoving = false;
            return true;
        });
        bridge.RegisterSyncHandler("resumeAllRobots", payload => {
            var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
            foreach (var r in robots) r.IsMoving = true;
            return true;
        });
        bridge.RegisterSyncHandler("clearAllRobots", payload => {
            this.Invoke(() => PetForm.Instance?.ClearAllRobots());
            return true;
        });
        bridge.RegisterSyncHandler("toggleRobotVisibility", payload => {
            if (payload.TryGetProperty("robotId", out var idProp)) {
                string robotId = idProp.GetString() ?? "";
                var target = PetForm.Instance?.GetRobots().FirstOrDefault(r => r.Name == robotId);
                if (target != null) target.IsVisible = !target.IsVisible;
            }
            return true;
        });
    }

    private void EnsureSubscribedToRobots()
    {
        var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
        foreach (var r in robots)
        {
            if (!_subscribedRobots.Contains(r))
            {
                _subscribedRobots.Add(r);
                r.OnChatMessageReceived += (role, content, thought) =>
                {
                    Bridge?.SendEvent("privateMessageReceived", new
                    {
                        robotId = r.Name,
                        message = new
                        {
                            role = role,
                            content = content,
                            thought = thought
                        }
                    });
                };
            }
        }
    }

    private object GetRobotDTOs()
    {
        var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
        return robots.Select(r => new
        {
            id = r.Name,
            name = r.Name,
            personality = r.Personality,
            hp = r.HP,
            maxHp = r.MaxHP,
            level = 1,
            exp = 0,
            maxExp = 100,
            killCount = 0,
            deathCount = 0,
            isDead = r.IsDead,
            isActive = r.IsActive,
            isThinking = r.IsBusy,
            isAiSpeaking = r.IsAiSpeaking,
            curseMode = r.CurseMode,
            colorHex = ColorToHex(r.PrimaryColor),
            chatText = r.ChatText,
            isMoving = r.IsMoving,
            isVisible = r.IsVisible,
            size = r.Size,
            speedMultiplier = r.SpeedMultiplier,
            posX = (int)r.X,
            posY = (int)r.Y
        }).ToList();
    }

    private object GetStatsDTO()
    {
        long tokens = AiService.TotalTokensUsed;
        double cost = tokens * 0.000002;
        var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
        int total = robots.Count;
        int moving = robots.Count(r => r.IsMoving && !r.IsDead);
        int paused = robots.Count(r => !r.IsMoving && !r.IsDead);

        return new
        {
            onlineCount = robots.Count(r => !r.IsDead && r.IsVisible && r.IsActive),
            totalRobots = total,
            movingRobots = moving,
            pausedRobots = paused,
            battleMode = SettingsManager.Current.BattleMode,
            totalTokens = tokens,
            totalCostYuan = cost
        };
    }

    private string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void PushRealtimeData()
    {
        if (Bridge == null) return;

        EnsureSubscribedToRobots();

        Bridge.SendEvent("robotsUpdated", GetRobotDTOs());
        Bridge.SendEvent("statsUpdated", GetStatsDTO());
    }

    public void BroadcastToWorld(string sender, string message, Color color)
    {
        var msg = new SocialMessage(sender, message);
        _globalWorldHistory.Add(msg);
        if (_globalWorldHistory.Count > 50) _globalWorldHistory.RemoveAt(0);

        Bridge?.SendEvent("worldMessageReceived", new
        {
            sender = sender,
            content = message,
            color = ColorToHex(color)
        });
    }

    public void ShowWorldChat()
    {
        this.Show();
        this.Activate();
    }

    public void OpenTerminal(Robot robot)
    {
        this.Show();
        this.Activate();
    }

    public void CloseTerminal(Robot robot)
    {
    }

    public void Shutdown()
    {
        _pushTimer?.Stop();
        this.Close();
    }
}
