using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PureBattleGame.Games.CockroachPet;

public static class SiliconFlowImageService
{
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    private const string ApiEndpoint = "https://api.siliconflow.cn/v1/images/generations";

    /// <summary>
    /// 调用 SiliconFlow 文生图 API (默认 Kwai-Kolors/Kolors) 生成纯绿幕角色背景图，自动抠图并保存为透明 PNG
    /// 返回元组: (FilePath, ErrorMessage)
    /// </summary>
    public static async Task<(string? FilePath, string? ErrorMessage)> GenerateAndProcessAvatarAsync(string robotName, string modelName = "Kwai-Kolors/Kolors", string? customPrompt = null)
    {
        try
        {
            var apiKey = AiService.GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) 
                return (null, "未配置 SiliconFlow API Key，请先在设置中填写");

            if (string.IsNullOrWhiteSpace(modelName)) modelName = "Kwai-Kolors/Kolors";

            string prompt = !string.IsNullOrWhiteSpace(customPrompt)
                ? customPrompt
                : $"((pure solid green background:1.8)), ((chroma key green background #00FF00:1.6)), ((simple background, isolated on green background:1.5)), full body 2D anime character sprite, {robotName}, standing pose, centered, studio lighting";

            string negativePrompt = "((complex background, detailed background, indoor, outdoor, room, street, scenery, gradient, floor, shadows, landscape:1.6)), photo, realistic";

            // Kwai-Kolors 官方标准图像分辨率建议为 1024x1024
            var requestBody = new
            {
                model = modelName,
                prompt = prompt,
                negative_prompt = negativePrompt,
                image_size = "1024x1024",
                batch_size = 1
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string parsedMsg = ParseErrorMessage(responseJson, (int)response.StatusCode);
                System.Diagnostics.Debug.WriteLine($"[SiliconFlowImage] 生图失败: {response.StatusCode} - {parsedMsg}");
                return (null, parsedMsg);
            }

            using var doc = JsonDocument.Parse(responseJson);
            string? imageUrl = null;
            var root = doc.RootElement;

            if (root.TryGetProperty("images", out var imagesArr) && imagesArr.GetArrayLength() > 0)
            {
                if (imagesArr[0].TryGetProperty("url", out var urlProp))
                    imageUrl = urlProp.GetString();
            }
            else if (root.TryGetProperty("data", out var dataArr) && dataArr.GetArrayLength() > 0)
            {
                if (dataArr[0].TryGetProperty("url", out var urlProp2))
                    imageUrl = urlProp2.GetString();
            }

            if (string.IsNullOrWhiteSpace(imageUrl)) 
                return (null, "API 返回了数据，但未包含有效的图片 URL");

            // 下载生成的网络图片
            byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
            using var ms = new MemoryStream(imageBytes);
            using var origBmp = new Bitmap(ms);

            // 智能双模自动扣图处理 (绿幕+边缘背景取样)
            using var transparentBmp = ChromaKeyProcessor.RemoveGreenScreen(origBmp);

            // 保存至 AppData/CockroachPet/Avatars
            string avatarsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CockroachPet", "Avatars");
            if (!Directory.Exists(avatarsDir)) Directory.CreateDirectory(avatarsDir);

            string safeName = string.Join("_", robotName.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(avatarsDir, $"avatar_{safeName}_{DateTime.Now.Ticks}.png");

            transparentBmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            return (filePath, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SiliconFlowImage] 生图或抠图异常: {ex.Message}");
            return (null, $"网络或抠图处理异常: {ex.Message}");
        }
    }

    private static string ParseErrorMessage(string jsonStr, int statusCode)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;
            int code = 0;
            string message = "";

            if (root.TryGetProperty("code", out var codeProp)) code = codeProp.GetInt32();
            if (root.TryGetProperty("message", out var msgProp)) message = msgProp.GetString() ?? "";

            if (code == 30001 || message.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
            {
                return "SiliconFlow 账号余额不足 (Code 30001)，请前往平台充值";
            }
            if (code == 50604 || message.Contains("limit", StringComparison.OrdinalIgnoreCase))
            {
                return "触发 SiliconFlow 生图频率限制 (IPM Limit)";
            }
            if (code == 30003 || message.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            {
                return "该生图模型在平台已被禁用或暂不可用";
            }

            if (!string.IsNullOrWhiteSpace(message)) return $"SiliconFlow 报错 [{statusCode}]: {message}";
        }
        catch { }

        return $"SiliconFlow 生图 API 响应错误 Status Code: {statusCode}";
    }
}
