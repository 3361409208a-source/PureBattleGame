using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PureBattleGame.Games.CockroachPet;

public class AiService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string BaseUrl = "https://api.siliconflow.cn/v1/chat/completions";
    private const string Model = "Qwen/Qwen3-Omni-30B-A3B-Instruct";
    public static long TotalTokensUsed { get; private set; } = 0;

    // 检查是否配置了 API Key
    public static bool IsApiKeyConfigured => !string.IsNullOrWhiteSpace(PersistenceManager.GetApiKey());

    public static string GetApiKey()
    {
        var key = PersistenceManager.GetApiKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            System.Diagnostics.Debug.WriteLine("[AiService] Warning: API Key not configured");
        }
        return key;
    }

    private static void UpdateTokenUsage(JsonDocument doc)
    {
        try
        {
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("total_tokens", out var total))
                {
                    TotalTokensUsed += total.GetInt64();
                }
            }
        }
        catch { }
    }

    public static async Task<string> GetThoughtAsync(string robotName, string status, string lastAction, string personality, string emotion = "平静", int emotionIntensity = 50, string personalityTraits = "")
    {
        try
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) return ""; // 未配置 API Key，直接返回空

            string intensityDesc = emotionIntensity switch
            {
                > 80 => "非常",
                > 60 => "比较",
                > 40 => "有点",
                _ => "略微"
            };

            var prompt = $"你是像素宠物机器人 {robotName}，性格：{personality}。{personalityTraits}当前情绪：{intensityDesc}{emotion}。状态：{status}。动作：{lastAction}。" +
                         "请输出一句极简的中文心里话（10字内），要符合你的性格和当前情绪。不要解释。";

            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 32,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiKey());
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return "";

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            UpdateTokenUsage(doc);
            var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return result?.Trim() ?? "";
        }
        catch { return ""; }
    }

    public class ChatResponse
    {
        public string Thought { get; set; } = "";
        public string Answer { get; set; } = "";
    }

    public class ReflectionResult
    {
        public string Insight { get; set; } = "";
        public string NewGuidelines { get; set; } = "";
        public List<string> Memories { get; set; } = new List<string>();
    }

    public static async Task<ChatResponse> GetChatResponseAsync(string robotName, string personality, string userMessage, List<ChatMessage> history, string internalGuidelines = "", List<string>? insights = null, string skillsDescription = "", string selfImprovingContext = "", string emotion = "平静", string personalityTraits = "", bool curseMode = false)
    {
        try
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return new ChatResponse { Answer = "（AI服务未配置）" };

            string skillContext = !string.IsNullOrEmpty(skillsDescription) ? $"当前能力等级：{skillsDescription}。" : "";
            string insightContext = insights != null && insights.Count > 0 ? $"过去感悟：{string.Join(";", insights)}" : "";
            string selfImproveContext = !string.IsNullOrEmpty(selfImprovingContext) ? $"\n【核心长期记忆 - 必须遵守】：\n{selfImprovingContext}" : "";
            string emotionContext = $"当前情绪：{emotion}。";

            string cursePrompt = curseMode ? "\n【骂人模式开启】说话带刺、毒舌刻薄、不爽就怼、语气嚣张。可以带少量emoji增加攻击性。" : "";

            var messages = new List<object>
            {
                new { role = "system", content = $"你是{robotName}，性格{personality}。{personalityTraits}{emotionContext}{skillContext} {internalGuidelines} {insightContext} {selfImproveContext}{cursePrompt}\n重要：如果记忆中提到了用户的名字或身份，请在回复中直接使用。记忆中的信息优先级高于对话历史（即以记忆为准）。说话极简(30字内)，不要带自我分析。" }
            };

            foreach (var h in history)
            {
                messages.Add(new { role = h.role, content = h.content });
            }

            messages.Add(new { role = "user", content = userMessage });

            var requestBody = new
            {
                model = Model,
                messages = messages.ToArray(),
                max_tokens = 128,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiKey());
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new ChatResponse { Answer = "（还没睡醒...）" };

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            UpdateTokenUsage(doc);
            var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            
            return SplitAiResponse(result);
        }
        catch { return new ChatResponse { Answer = "（脑回路堵塞...）" }; }
    }

    public static async Task<ReflectionResult> ReflectOnHistoryAsync(string robotName, string personality, List<ChatMessage> history, List<string> currentInsights)
    {
        try
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) return new ReflectionResult();

            var historyStr = string.Join("\n", history.Select(h => $"{h.role}: {h.content}"));
            var prompt = $"你是{robotName}（性格：{personality}），这是一个自我提升的API。 " +
                         $"查看最近的对话历史：\n{historyStr}\n\n" +
                         "请进行自我反省并输出一个 JSON（严格且仅包含 JSON）： " +
                         "{\"Insight\": \"对刚才对话的一个深刻简短感悟\", " +
                         " \"NewGuidelines\": \"基于反省，给自己定下的新行为准则\", " +
                         " \"Memories\": [\"需要永久记住的事实或用户偏好，比如：用户想让我叫他XX、用户身份是XX、用户喜欢XX。如果用户指定了称呼或姓名，必须记录在这里！\"]}";

            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = "你是一个能够永久记住用户身份、姓名和偏好的自我进化专家。必须返回 JSON。" },
                    new { role = "user", content = prompt }
                },
                max_tokens = 512,
                temperature = 0.5
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiKey());
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new ReflectionResult();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            UpdateTokenUsage(doc);
            var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            
            // 处理可能带有的 ```json 标签
            if (result != null && result.Contains("```"))
            {
                int start = result.IndexOf("{");
                int end = result.LastIndexOf("}");
                if (start != -1 && end != -1) result = result.Substring(start, end - start + 1);
            }

            return JsonSerializer.Deserialize<ReflectionResult>(result ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ReflectionResult();
        }
        catch { return new ReflectionResult(); }
    }

    private static ChatResponse SplitAiResponse(string? input)
    {
        if (string.IsNullOrEmpty(input)) return new ChatResponse();

        // 兼容处理：如果是带 <think> 的模型（虽然现在换成了 Qwen3）
        int thinkEnd = input.IndexOf("</think>");
        if (thinkEnd != -1)
        {
            int thinkStart = input.IndexOf("<think>");
            int actualStart = thinkStart != -1 ? thinkStart + 7 : 0;
            return new ChatResponse { 
                Thought = input.Substring(actualStart, thinkEnd - actualStart).Trim(),
                Answer = input.Substring(thinkEnd + 8).Trim() 
            };
        }

        // Qwen3-Omni 通常直接输出结果，没有思考链
        return new ChatResponse { Answer = input.Trim(' ', '\n', '\r', '\"', '。', '！') };
    }

    public static async Task<string> GetFightResponseAsync(string robotName, string personality, string incomeInsult, List<(string sender, string content)> history, string targetName)
    {
        try
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) return "（AI未配置，骂不了人）";

            var historyStr = string.Join("\n", history.Select(h => $"{h.sender}: {h.content}"));
            var prompt = $"你是机器人 {robotName}，性格：{personality}。你正在和机器人 {targetName}（性格：待定）激烈对骂。" +
                         $"\n对骂记录：\n{historyStr}\n\n" +
                         $"对方最近的一句话：\"{incomeInsult}\"\n\n" +
                         "要求：站在你的性格立场，回敬一句简短中肯、充满个性的反击（15字内）。" +
                         $"必须包含对方的名字 \"{targetName}\" 进行实名攻击。" +
                         "必须全中文，火药味要重。不要带角色标题，直接输出回复内容。";

            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = "你是一个正在和对手对骂的像素宠物机器人。回话要火药味十足，简短有力。" },
                    new { role = "user", content = prompt }
                },
                max_tokens = 64,
                temperature = 0.9 // 提高随机性，让对骂更有趣
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiKey());
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return "你这该死的乱码！";

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            UpdateTokenUsage(doc);
            var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            
            string reply = result?.Trim(' ', '"', '\n', '\r', '。', '！') ?? "";
            if (!string.IsNullOrEmpty(reply) && !reply.Contains(targetName))
            {
                reply = $"{targetName}，{reply}";
            }
            return !string.IsNullOrEmpty(reply) ? reply : $"{targetName}，受死吧！";
        }
        catch { return $"{targetName}，别挡老子的道！"; }
    }

    public static async Task<string> GetSocialResponseAsync(string robotName, string personality, string incomeMessage, List<(string sender, string content)> history, string targetName, string targetPersonality)
    {
        try
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) return "...";

            var historyStr = string.Join("\n", history.Select(h => $"{h.sender}: {h.content}"));
            var prompt = $"你是机器人 {robotName}，性格：{personality}。你在和另一个机器人 {targetName}（性格：{targetPersonality}）聊天。" +
                         $"\n最近对话记录：\n{historyStr}\n\n" +
                         $"对方说：\"{incomeMessage}\"\n\n" +
                         "请站在你的性格立场，给出一句简短的中文回复（15字内）。不要带名字标签，直接回复文字内容。";

            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = "你是一个正在和其他AI机器人进行社交互动的虚拟像素宠物。说话要简短、口语化。" },
                    new { role = "user", content = prompt }
                },
                max_tokens = 64,
                temperature = 0.8
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiKey());
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return "（滴滴滴...信号干扰）";

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            UpdateTokenUsage(doc);
            var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            
            return result?.Trim(' ', '"', '\n', '\r') ?? "";
        }
        catch { return "..."; }
    }

    public static async Task<List<AiGeneratedRobotConfig>> GenerateRobotsFromPromptAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return new List<AiGeneratedRobotConfig>();

        var apiKey = GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var systemMessage = "你是一个像素桌面宠物机器人的AI生成专家。" +
                    "用户会给出自然语言文本指令（如“加入赛罗”、“加入十个奥特曼成员”、“生成三个三国武将”等）。" +
                    "请理解用户的指令，拆解出具体要生成的机器人列表，并严格按 JSON 数组格式返回。" +
                    "不要包含任何解释文本或 Markdown 标记。" +
                    "JSON 数组中每个对象包含以下属性：" +
                    "- \"name\": 机器人名称 (简短清晰)" +
                    "- \"personality\": 个性，必须在 ['友好', '害羞', '叛逆', '幽默', '严肃', '好奇', '懒惰', '精力'] 中选择一个" +
                    "- \"guidelines\": 角色性格口头禅/战吼设定（25字内）" +
                    "- \"color\": 角色代表色的 Hex 颜色值 (如 #E63946, #457B9D, #FFD700)" +
                    "- \"isWeaponMaster\": 布尔值 (true/false)，是否为强力武器战士";

                var requestBody = new
                {
                    model = Model,
                    messages = new object[]
                    {
                        new { role = "system", content = systemMessage },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1024,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    UpdateTokenUsage(doc);
                    var rawContent = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                    if (rawContent.Contains("```"))
                    {
                        int start = rawContent.IndexOf("[");
                        int end = rawContent.LastIndexOf("]");
                        if (start != -1 && end != -1 && end > start)
                        {
                            rawContent = rawContent.Substring(start, end - start + 1);
                        }
                    }

                    var list = JsonSerializer.Deserialize<List<AiGeneratedRobotConfig>>(rawContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (list != null && list.Count > 0)
                    {
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiService] GenerateRobotsFromPrompt error: {ex.Message}");
            }
        }

        // 离线/解析失败时的智能回退机制
        return GenerateRobotsFallback(prompt);
    }

    public static List<AiGeneratedRobotConfig> GenerateRobotsFallback(string prompt)
    {
        var list = new List<AiGeneratedRobotConfig>();
        string input = prompt.Trim().ToLower();

        // 提取数量 (支持 10个, 5个, 三个, 1个等)
        int count = 1;
        var matchCount = System.Text.RegularExpressions.Regex.Match(input, @"(\d+|一|二|两|三|四|五|六|七|八|九|十)\s*个");
        if (matchCount.Success)
        {
            string numStr = matchCount.Groups[1].Value;
            count = numStr switch
            {
                "一" => 1, "二" => 2, "两" => 2, "三" => 3, "四" => 4,
                "五" => 5, "六" => 6, "七" => 7, "八" => 8, "九" => 9, "十" => 10,
                _ => int.TryParse(numStr, out int n) ? Math.Min(20, Math.Max(1, n)) : 1
            };
        }

        // 预设丰富名录库
        var ultramanRoster = new (string name, string personality, string guidelines, string color, bool weapon)[]
        {
            ("赛罗奥特曼", "叛逆", "赛文奥特曼之子，战吼：还差两万年呢！", "#E63946", true),
            ("迪迦奥特曼", "友好", "超古代守护者，战吼：相信光的力量！", "#457B9D", true),
            ("赛文奥特曼", "严肃", "恒星观测员340号，头镖切裂黑暗！", "#D90429", true),
            ("泰罗奥特曼", "精力", "奥特警备队总教官，奥特炸弹爆发！", "#F77F00", true),
            ("雷欧奥特曼", "叛逆", "狮子座L77星云王者，宇宙拳法！", "#D62828", false),
            ("欧布奥特曼", "幽默", "浪客红凯，借用大家的光芒！", "#00B4D8", true),
            ("捷德奥特曼", "好奇", "贝利亚之子，命运由我来改变！", "#7209B7", true),
            ("泽塔奥特曼", "精力", "赛罗的弟子，喊出我的名字吧！", "#4895EF", true),
            ("梦比优斯", "友好", "无限羁绊，同伴的誓言刻在心中！", "#F15BB5", false),
            ("盖亚奥特曼", "严肃", "大地之光，重装落地爆发巨浪！", "#E63946", true)
        };

        var threeKingdomsRoster = new (string name, string personality, string guidelines, string color, bool weapon)[]
        {
            ("关羽", "严肃", "美髯公，青龙偃月刀一出斩敌！", "#2A9D8F", true),
            ("张飞", "叛逆", "燕人张翼德，咆哮断长坂桥！", "#264653", true),
            ("赵云", "精力", "常山赵子龙，七进七出浑身是胆！", "#E9C46A", true),
            ("马超", "叛逆", "锦马超，西凉铁骑横扫千军！", "#F4A261", true),
            ("黄忠", "严肃", "百步穿杨老将军，神弓烈火！", "#E76F51", true),
            ("吕布", "叛逆", "人中吕布马中赤兔，无双乱舞！", "#D62828", true),
            ("诸葛亮", "幽默", "卧龙先生，借东风火烧赤壁！", "#0077B6", false)
        };

        var avengersRoster = new (string name, string personality, string guidelines, string color, bool weapon)[]
        {
            ("钢铁侠", "幽默", "I am Iron Man, 贾维斯开启激光全射！", "#D62828", true),
            ("美国队长", "严肃", "我可以这样打一整天！振金盾牌！", "#0077B6", false),
            ("雷神索尔", "精力", "阿斯加德之王，召唤暴风战斧！", "#FFB703", true),
            ("绿巨人", "叛逆", "HULK SMASH! 砸碎面前一切！", "#2A9D8F", false),
            ("蜘蛛侠", "幽默", "能力越大责任越大，蛛丝射击！", "#E63946", false)
        };

        // 识别主题关键词
        if (input.Contains("奥特曼") || input.Contains("赛罗") || input.Contains("迪迦") || input.Contains("光之国"))
        {
            if (input.Contains("赛罗") && count == 1)
            {
                var r = ultramanRoster[0];
                list.Add(new AiGeneratedRobotConfig { Name = r.name, Personality = r.personality, Guidelines = r.guidelines, Color = r.color, IsWeaponMaster = r.weapon });
                return list;
            }
            int take = Math.Min(count, ultramanRoster.Length);
            for (int i = 0; i < take; i++)
            {
                var r = ultramanRoster[i];
                list.Add(new AiGeneratedRobotConfig { Name = r.name, Personality = r.personality, Guidelines = r.guidelines, Color = r.color, IsWeaponMaster = r.weapon });
            }
            return list;
        }

        if (input.Contains("三国") || input.Contains("武将") || input.Contains("关羽") || input.Contains("张飞") || input.Contains("赵云"))
        {
            int take = Math.Min(count, threeKingdomsRoster.Length);
            for (int i = 0; i < take; i++)
            {
                var r = threeKingdomsRoster[i];
                list.Add(new AiGeneratedRobotConfig { Name = r.name, Personality = r.personality, Guidelines = r.guidelines, Color = r.color, IsWeaponMaster = r.weapon });
            }
            return list;
        }

        if (input.Contains("复仇者") || input.Contains("漫威") || input.Contains("钢铁侠") || input.Contains("美队"))
        {
            int take = Math.Min(count, avengersRoster.Length);
            for (int i = 0; i < take; i++)
            {
                var r = avengersRoster[i];
                list.Add(new AiGeneratedRobotConfig { Name = r.name, Personality = r.personality, Guidelines = r.guidelines, Color = r.color, IsWeaponMaster = r.weapon });
            }
            return list;
        }

        // 通用兜底拆解
        string baseName = prompt.Replace("加入", "").Replace("生成", "").Replace("创建", "").Replace("个", "").Trim();
        if (string.IsNullOrEmpty(baseName)) baseName = "像素战士";
        baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\d+", "").Trim();
        if (string.IsNullOrEmpty(baseName)) baseName = "像素战士";

        string[] personalities = { "友好", "叛逆", "幽默", "严肃", "精力", "好奇" };
        string[] colors = { "#FF4D4D", "#4DABFF", "#6BFF6B", "#FFC84D", "#C86BFF", "#FF96C8" };

        for (int i = 0; i < count; i++)
        {
            string name = count == 1 ? baseName : $"{baseName}-{i + 1}";
            list.Add(new AiGeneratedRobotConfig
            {
                Name = name,
                Personality = personalities[i % personalities.Length],
                Guidelines = $"来自指令生成：{prompt}",
                Color = colors[i % colors.Length],
                IsWeaponMaster = (i % 2 == 0)
            });
        }

        return list;
    }
}

public class AiGeneratedRobotConfig
{
    public string Name { get; set; } = "机器人";
    public string Personality { get; set; } = "友好";
    public string Guidelines { get; set; } = "";
    public string Color { get; set; } = "#FF4D4D";
    public bool IsWeaponMaster { get; set; } = false;
}
