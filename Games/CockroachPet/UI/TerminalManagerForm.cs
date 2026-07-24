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
            Interval = 1500
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

        // 9. 获取系统设置 (全量属性)
        bridge.RegisterSyncHandler("getSettings", payload => new
        {
            opacity = SettingsManager.Current.DefaultOpacity,
            homeUrl = SettingsManager.Current.HomeUrl,
            autoStart = SettingsManager.Current.AutoStart,
            hideNameAndPersonality = SettingsManager.Current.HideNameAndPersonality,
            curseModeByDefault = SettingsManager.Current.CurseModeByDefault,
            battleMode = SettingsManager.Current.BattleMode,
            languageMode = SettingsManager.Current.LanguageInteractionMode,
            actionMode = SettingsManager.Current.ActionInteractionMode,
            robotSize = SettingsManager.Current.RobotSize,
            robotSpeed = SettingsManager.Current.RobotSpeed,
            skillScale = SettingsManager.Current.SkillScale,
            soundVolume = SettingsManager.Current.SoundVolume,
            fightFrequency = SettingsManager.Current.FightFrequency,
            enableAiThinking = SettingsManager.Current.EnableAiThinking,
            aiThoughtFrequency = SettingsManager.Current.AiThoughtFrequency,
            isWeaponMaster = SettingsManager.Current.IsWeaponMaster,
            robotMaxHp = SettingsManager.Current.RobotMaxHp,
            isGodMode = SettingsManager.Current.IsGodMode,
            enabledWeapons = SettingsManager.Current.EnabledWeapons,
            apiKey = PersistenceManager.GetApiKey()
        });

        // 10. 保存系统设置 (全量属性)
        bridge.RegisterSyncHandler("saveSettings", payload =>
        {
            if (payload.TryGetProperty("opacity", out var opProp))
            {
                double op = Math.Clamp(opProp.GetDouble(), 0.1, 1.0);
                SettingsManager.Current.DefaultOpacity = op;
                if (PetForm.Instance != null && !PetForm.Instance.IsDisposed)
                    PetForm.Instance.Invoke(() => PetForm.Instance.Opacity = op);
            }
            if (payload.TryGetProperty("homeUrl", out var urlProp))
                SettingsManager.Current.HomeUrl = urlProp.GetString() ?? "https://www.xiaoheiv.top";
            if (payload.TryGetProperty("autoStart", out var autoProp))
                SettingsManager.Current.AutoStart = autoProp.GetBoolean();

            if (payload.TryGetProperty("hideNameAndPersonality", out var hideProp))
                SettingsManager.Current.HideNameAndPersonality = hideProp.GetBoolean();
            if (payload.TryGetProperty("curseModeByDefault", out var curseProp))
                SettingsManager.Current.CurseModeByDefault = curseProp.GetBoolean();
            if (payload.TryGetProperty("languageMode", out var langProp))
                SettingsManager.Current.LanguageInteractionMode = langProp.GetString() ?? "互骂吐槽";
            if (payload.TryGetProperty("actionMode", out var actProp))
            {
                string act = actProp.GetString() ?? "近远交替";
                SettingsManager.Current.ActionInteractionMode = act;
                SettingsManager.Current.BattleMode = act;
            }

            if (payload.TryGetProperty("robotSize", out var sizeProp))
            {
                int newSize = Math.Clamp(sizeProp.GetInt32(), 16, 128);
                SettingsManager.Current.RobotSize = newSize;
                if (PetForm.Instance != null && !PetForm.Instance.IsDisposed)
                {
                    PetForm.Instance.DefaultRobotSize = newSize;
                    var activeRobots = PetForm.Instance.GetRobots();
                    foreach (var r in activeRobots)
                    {
                        r.Size = newSize;
                        r.OriginalSize = newSize;
                    }
                }
            }

            if (payload.TryGetProperty("robotSpeed", out var speedProp))
            {
                int newSpeed = Math.Clamp(speedProp.GetInt32(), 50, 300);
                SettingsManager.Current.RobotSpeed = newSpeed;
                if (PetForm.Instance != null && !PetForm.Instance.IsDisposed)
                {
                    float mult = newSpeed / 100.0f;
                    var activeRobots = PetForm.Instance.GetRobots();
                    foreach (var r in activeRobots)
                    {
                        r.SpeedMultiplier = mult;
                    }
                }
            }

            if (payload.TryGetProperty("skillScale", out var scaleProp))
            {
                int newScale = Math.Clamp(scaleProp.GetInt32(), 10, 300);
                SettingsManager.Current.SkillScale = newScale;
                if (PetForm.Instance != null && !PetForm.Instance.IsDisposed)
                {
                    PetForm.Instance.GlobalSkillScale = newScale;
                }
            }
            if (payload.TryGetProperty("soundVolume", out var volProp))
            {
                int vol = volProp.GetInt32();
                SettingsManager.Current.SoundVolume = vol;
                AudioManager.VolumeScale = vol / 100.0f;
            }
            if (payload.TryGetProperty("fightFrequency", out var fightProp))
                SettingsManager.Current.FightFrequency = fightProp.GetInt32();
            if (payload.TryGetProperty("enableAiThinking", out var aiProp))
                SettingsManager.Current.EnableAiThinking = aiProp.GetBoolean();
            if (payload.TryGetProperty("aiThoughtFrequency", out var aiFreqProp))
                SettingsManager.Current.AiThoughtFrequency = aiFreqProp.GetInt32();
            if (payload.TryGetProperty("isWeaponMaster", out var masterProp))
                SettingsManager.Current.IsWeaponMaster = masterProp.GetBoolean();

            if (payload.TryGetProperty("robotMaxHp", out var hpProp))
            {
                int maxHp = Math.Max(100, hpProp.GetInt32());
                SettingsManager.Current.RobotMaxHp = maxHp;
                var activeRobots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
                foreach (var r in activeRobots)
                {
                    r.MaxHP = maxHp;
                    if (r.HP > maxHp) r.HP = maxHp;
                }
            }

            if (payload.TryGetProperty("isGodMode", out var godProp))
            {
                bool god = godProp.GetBoolean();
                SettingsManager.Current.IsGodMode = god;
                var activeRobots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
                foreach (var r in activeRobots)
                {
                    r.IsGodMode = god;
                    if (god) { r.HP = r.MaxHP; r.IsDead = false; }
                }
            }

            if (payload.TryGetProperty("enabledWeapons", out var weaponsProp) && weaponsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var list = new System.Collections.Generic.List<string>();
                foreach (var el in weaponsProp.EnumerateArray())
                {
                    if (el.GetString() is string s) list.Add(s);
                }
                SettingsManager.Current.EnabledWeapons = list;
            }

            if (payload.TryGetProperty("apiKey", out var keyProp))
            {
                var appSet = PersistenceManager.LoadAppSettings();
                appSet.ApiKey = keyProp.GetString() ?? "";
                PersistenceManager.SaveAppSettings(appSet);
            }

            SettingsManager.Save();

            // 立刻重置所有机器人的交战与锁敌状态，使新设置立刻生效
            var robots = PetForm.Instance?.GetRobots() ?? new List<Robot>();
            foreach (var r in robots)
            {
                r.ChasingTarget = null;
                r.ChaseTimer = 0;
                r.DuelTarget = null;
                r.DuelTimer = 0;
                r.PhysicalTarget = null;
                r.PhysicalAction = "NONE";
                r.IsFiringLaser = false;
                r.IsMoving = true;
                r.CurseMode = SettingsManager.Current.CurseModeByDefault;
                r.Size = SettingsManager.Current.RobotSize;
                r.SpeedMultiplier = SettingsManager.Current.RobotSpeed / 100.0f;
                if (SettingsManager.Current.ActionInteractionMode == "和平相处")
                {
                    r.SpecialState = "HAPPY";
                    r.SpecialStateTimer = 30;
                }
                else
                {
                    r.SpecialState = "";
                }
            }

            return true;
        });

        bridge.RegisterSyncHandler("startRecording", payload =>
        {
            string mode = payload.TryGetProperty("mode", out var m) ? m.GetString() ?? "CUSTOM_BG" : "CUSTOM_BG";
            string hex = payload.TryGetProperty("hexColor", out var h) ? h.GetString() ?? "#00FF00" : "#00FF00";
            bool success = Services.PromoRecorder.Instance.StartRecording(mode, hex);
            return new { success };
        });

        bridge.RegisterSyncHandler("stopRecording", payload =>
        {
            return Services.PromoRecorder.Instance.StopRecording();
        });

        bridge.RegisterSyncHandler("getRecordingStatus", payload =>
        {
            return Services.PromoRecorder.Instance.GetStatus();
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
        bridge.RegisterSyncHandler("clearProjectiles", payload => {
            this.Invoke(() => PetForm.Instance?.ClearAllProjectiles());
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

        // 12. AI 智能生成机器人 (含 SiliconFlow 生图与绿幕抠图全流程步骤进度推送)
        bridge.RegisterHandler("generateAiRobots", async payload =>
        {
            if (!payload.TryGetProperty("prompt", out var promptProp))
                return new { success = false, message = "未提供生成指令" };

            string prompt = promptProp.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(prompt))
                return new { success = false, message = "指令不能为空" };

            bool enableImageGen = true;
            if (payload.TryGetProperty("enableImageGen", out var imgGenProp))
                enableImageGen = imgGenProp.GetBoolean();

            string imageModel = "Kwai-Kolors/Kolors";
            if (payload.TryGetProperty("imageModel", out var modelProp) && !string.IsNullOrWhiteSpace(modelProp.GetString()))
                imageModel = modelProp.GetString()!;

            Bridge?.SendEvent("aiGenerateProgress", new { percent = 15, step = 1, message = "⚡ 正在连接 SiliconFlow 大模型 API 服务通道..." });
            await System.Threading.Tasks.Task.Delay(200);

            Bridge?.SendEvent("aiGenerateProgress", new { percent = 35, step = 2, message = "🧠 LLM 大模型解析中：拆解角色名称、性格特质与口头禅..." });
            
            List<AiGeneratedRobotConfig> configs;
            try
            {
                configs = await AiService.GenerateRobotsFromPromptAsync(prompt);
            }
            catch (Exception ex)
            {
                Bridge?.SendEvent("aiGenerateProgress", new { percent = 100, step = 4, error = true, message = $"❌ LLM解析失败: {ex.Message}" });
                return new { success = false, message = ex.Message };
            }

            if (configs == null || configs.Count == 0)
            {
                Bridge?.SendEvent("aiGenerateProgress", new { percent = 100, step = 4, error = true, message = "❌ AI 未生成有效的机器人配置" });
                return new { success = false, message = "AI 未生成有效配置" };
            }

            // 图像生成与绿幕抠图处理（如果开启了 SiliconFlow 生图功能）
            string? lastImageError = null;
            if (enableImageGen && AiService.IsApiKeyConfigured)
            {
                int total = configs.Count;
                for (int i = 0; i < total; i++)
                {
                    var cfg = configs[i];
                    int currentPercent = 50 + (int)((i + 1) * 35.0 / total);
                    Bridge?.SendEvent("aiGenerateProgress", new { 
                        percent = currentPercent, 
                        step = 3, 
                        message = $"🎨 SiliconFlow ({imageModel}) 生图抠图中 ({i + 1}/{total}): 正在为 [{cfg.Name}] 生成纯绿幕角色图并自动抠图..." 
                    });

                    var (avatarPath, errorMsg) = await SiliconFlowImageService.GenerateAndProcessAvatarAsync(cfg.Name, imageModel);
                    if (!string.IsNullOrEmpty(avatarPath))
                    {
                        cfg.AvatarPath = avatarPath;
                    }
                    else if (!string.IsNullOrEmpty(errorMsg))
                    {
                        lastImageError = errorMsg;
                        Bridge?.SendEvent("aiGenerateProgress", new { 
                            percent = currentPercent, 
                            step = 3, 
                            message = $"⚠️ 生图提醒: {errorMsg}，已跳过图片抠图生成" 
                        });
                        await System.Threading.Tasks.Task.Delay(1200);
                    }
                }
            }
            else
            {
                Bridge?.SendEvent("aiGenerateProgress", new { percent = 80, step = 3, message = $"🎨 像素外貌合成：生成 {configs.Count} 个专属色值、技能与装备判定..." });
                await System.Threading.Tasks.Task.Delay(300);
            }

            this.Invoke(() => PetForm.Instance?.SpawnRobotsFromConfigs(configs));

            string completionMsg = !string.IsNullOrEmpty(lastImageError)
                ? $"⚠️ 成功降临 {configs.Count} 个机器人！(生图未应用: {lastImageError})"
                : $"🎉 成功生成并降临 {configs.Count} 个专属角色机器人！";

            Bridge?.SendEvent("aiGenerateProgress", new { percent = 100, step = 4, completed = true, message = completionMsg });

            return new
            {
                success = true,
                count = configs.Count,
                configs = configs.Select(c => new {
                    name = c.Name,
                    personality = c.Personality,
                    guidelines = c.Guidelines,
                    color = c.Color,
                    isWeaponMaster = c.IsWeaponMaster,
                    avatarPath = c.AvatarPath,
                    weapons = c.Weapons
                }).ToList()
            };
        });

        // 13. 更新机器人人设指南 (Guidelines)
        bridge.RegisterHandler("updateRobotGuidelines", async payload =>
        {
            if (!payload.TryGetProperty("robotId", out var idProp) || !payload.TryGetProperty("guidelines", out var gProp))
                return new { success = false, message = "缺失参数" };

            string robotId = idProp.GetString() ?? "";
            string guidelines = gProp.GetString() ?? "";

            var robot = PetForm.Instance?.GetRobots().FirstOrDefault(r => r.Name == robotId);
            if (robot != null)
            {
                robot.InternalGuidelines = guidelines;
                PersistenceManager.SaveRobots(PetForm.Instance!.GetRobots());
                PushRealtimeData();
                return new { success = true };
            }
            return new { success = false, message = "未找到机器人" };
        });

        // 14. 桌面聚焦/定位机器人
        bridge.RegisterHandler("focusRobot", async payload =>
        {
            if (!payload.TryGetProperty("robotId", out var idProp)) return new { success = false };
            string robotId = idProp.GetString() ?? "";
            var robot = PetForm.Instance?.GetRobots().FirstOrDefault(r => r.Name == robotId);
            if (robot != null)
            {
                this.Invoke(() => {
                    robot.IsVisible = true;
                    robot.SetBark($"📍 我在这里！({robot.Name})", 120);
                });
                return new { success = true };
            }
            return new { success = false };
        });

        // 15. 移除/召回机器人
        bridge.RegisterHandler("removeRobot", async payload =>
        {
            if (!payload.TryGetProperty("robotId", out var idProp)) return new { success = false };
            string robotId = idProp.GetString() ?? "";
            var robot = PetForm.Instance?.GetRobots().FirstOrDefault(r => r.Name == robotId);
            if (robot != null)
            {
                this.Invoke(() => PetForm.Instance?.RemoveRobot(robot));
                PushRealtimeData();
                return new { success = true };
            }
            return new { success = false };
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
            level = Math.Max(1, r.Experience / 100 + 1),
            exp = r.Experience % 100,
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
            posY = (int)r.Y,

            // 扩展全维度属性
            guidelines = r.InternalGuidelines,
            consciousnessLevel = r.ConsciousnessLevel,
            learnedInsights = r.LearnedInsights ?? new List<string>(),
            skills = r.Skills?.ToDictionary(
                k => k.Key,
                v => new {
                    name = v.Value.Name,
                    level = v.Value.Level,
                    experience = v.Value.Experience,
                    nextLevelXp = v.Value.NextLevelXp,
                    description = v.Value.Description
                }
            ) ?? new object(),
            customPhrases = r.CustomPhrases ?? new List<string>(),
            avatarPath = r.CustomAvatarPath,
            avatarDataUrl = GetAvatarDataUrl(r.CustomAvatarPath),
            isWeaponMaster = r.IsWeaponMaster,
            fightFrequency = r.FightFrequency,
            aiThoughtFrequency = r.AiThoughtFrequency,
            enableAiThinking = r.EnableAiThinking
        }).ToList();
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime lastWrite, string dataUrl)> _avatarBase64Cache 
        = new System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime, string)>();

    private static string GetAvatarDataUrl(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath)) return "";
        try
        {
            var lastWrite = System.IO.File.GetLastWriteTimeUtc(filePath);
            if (_avatarBase64Cache.TryGetValue(filePath, out var cached) && cached.lastWrite == lastWrite)
            {
                return cached.dataUrl;
            }

            byte[] bytes = System.IO.File.ReadAllBytes(filePath);
            string dataUrl = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
            _avatarBase64Cache[filePath] = (lastWrite, dataUrl);
            return dataUrl;
        }
        catch
        {
            return "";
        }
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
