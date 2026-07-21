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
            
            return result?.Trim(' ', '"', '\n', '\r', '。', '！') ?? "哼！";
        }
        catch { return "别挡道！"; }
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

    public static async Task<AiSpawnRequest> ParseSpawnCommandAsync(string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt)) return new AiSpawnRequest();

        if (IsApiKeyConfigured)
        {
            try
            {
                var apiKey = GetApiKey();
                var systemPrompt = @"你是一个控制像素格斗游戏角色生成的AI助手。根据用户的自然语言指示，解析出需要生成的角色或怪物。
必须严格且仅返回如下格式的 JSON 字符串：
{
  ""ClearExisting"": false,
  ""Robots"": [
    { ""Name"": ""角色名称"", ""Personality"": ""Rebel"", ""IsWeaponMaster"": true, ""Count"": 1 }
  ],
  ""Monsters"": [
    { ""Name"": ""怪物名称"", ""Count"": 1 }
  ]
}
Personality 可选值：Friendly, Rebel, Energetic, Calm, Tsundere, Mysterious, Master。
如果用户没有指定具体数量，默认为1。如果用户说'十个奥特曼成员'，应当在 Robots 中列出10个具有具体名字的奥特曼角色（例如：赛罗奥特曼、迪迦奥特曼、戴拿奥特曼、盖亚奥特曼、阿古茹奥特曼、高斯奥特曼、杰斯提斯奥特曼、奈克瑟斯奥特曼、麦克斯奥特曼、梦比优斯奥特曼）。
如果用户提到清空/清除敌人或角色，ClearExisting 设为 true。";

                var requestBody = new
                {
                    model = Model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    max_tokens = 512,
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
                    var rawResult = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                    if (!string.IsNullOrEmpty(rawResult))
                    {
                        int start = rawResult.IndexOf("{");
                        int end = rawResult.LastIndexOf("}");
                        if (start != -1 && end != -1)
                        {
                            string jsonString = rawResult.Substring(start, end - start + 1);
                            var parsed = JsonSerializer.Deserialize<AiSpawnRequest>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (parsed != null && (parsed.Robots.Count > 0 || parsed.Monsters.Count > 0 || parsed.ClearExisting))
                            {
                                return parsed;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return ParseSpawnCommandOffline(userPrompt);
    }

    private static AiSpawnRequest ParseSpawnCommandOffline(string userPrompt)
    {
        var request = new AiSpawnRequest();

        if (userPrompt.Contains("清空") || userPrompt.Contains("清除") || userPrompt.Contains("清理"))
        {
            request.ClearExisting = true;
        }

        int count = 1;
        var numMatch = System.Text.RegularExpressions.Regex.Match(userPrompt, @"(\d+|一|二|两|三|四|五|六|七|八|九|十)");
        if (numMatch.Success)
        {
            string numStr = numMatch.Value;
            count = numStr switch
            {
                "一" => 1, "二" => 2, "两" => 2, "三" => 3, "四" => 4, "五" => 5,
                "六" => 6, "七" => 7, "八" => 8, "九" => 9, "十" => 10,
                _ => int.TryParse(numStr, out int val) ? val : 1
            };
        }

        string cleanPrompt = userPrompt.Replace("加入", "").Replace("生成", "").Replace("召唤", "").Replace("添加", "").Trim();

        if (cleanPrompt.Contains("奥特曼"))
        {
            string[] ultramanList = { "赛罗奥特曼", "迪迦奥特曼", "戴拿奥特曼", "盖亚奥特曼", "阿古茹奥特曼", "高斯奥特曼", "杰斯提斯奥特曼", "奈克瑟斯奥特曼", "麦克斯奥特曼", "梦比优斯奥特曼", "赛文奥特曼", "雷欧奥特曼" };
            for (int i = 0; i < count; i++)
            {
                request.Robots.Add(new AiSpawnItem
                {
                    Name = ultramanList[i % ultramanList.Length],
                    Personality = "Rebel",
                    IsWeaponMaster = true,
                    Count = 1
                });
            }
        }
        else if (cleanPrompt.Contains("怪兽") || cleanPrompt.Contains("怪物"))
        {
            request.Monsters.Add(new AiSpawnMonsterItem
            {
                Name = string.IsNullOrWhiteSpace(cleanPrompt) ? "哥尔赞" : cleanPrompt,
                Count = count
            });
        }
        else
        {
            string name = string.IsNullOrWhiteSpace(cleanPrompt) ? "赛罗" : cleanPrompt;
            request.Robots.Add(new AiSpawnItem
            {
                Name = name,
                Personality = "Rebel",
                IsWeaponMaster = true,
                Count = count
            });
        }

        return request;
    }
}

public class AiSpawnRequest
{
    public bool ClearExisting { get; set; } = false;
    public List<AiSpawnItem> Robots { get; set; } = new List<AiSpawnItem>();
    public List<AiSpawnMonsterItem> Monsters { get; set; } = new List<AiSpawnMonsterItem>();
}

public class AiSpawnItem
{
    public string Name { get; set; } = "机器人";
    public string Personality { get; set; } = "Rebel";
    public bool IsWeaponMaster { get; set; } = true;
    public int Count { get; set; } = 1;
}

public class AiSpawnMonsterItem
{
    public string Name { get; set; } = "怪兽";
    public int Count { get; set; } = 1;
}
