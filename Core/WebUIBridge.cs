using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace PureBattleGame.Core;

public class WebUIBridge
{
    private readonly CoreWebView2 _webView;
    private readonly Dictionary<string, Func<JsonElement, Task<object?>>> _handlers = new();

    public WebUIBridge(CoreWebView2 webView)
    {
        _webView = webView;
        _webView.WebMessageReceived += OnWebMessageReceived;
    }

    public void RegisterHandler(string action, Func<JsonElement, Task<object?>> handler)
    {
        _handlers[action] = handler;
    }

    public void RegisterSyncHandler(string action, Func<JsonElement, object?> handler)
    {
        _handlers[action] = payload => Task.FromResult(handler(payload));
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            string jsonString = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            if (!root.TryGetProperty("action", out var actionProp)) return;
            string action = actionProp.GetString() ?? "";

            string? callbackId = null;
            if (root.TryGetProperty("callbackId", out var cbProp))
            {
                callbackId = cbProp.GetString();
            }

            JsonElement payload = default;
            if (root.TryGetProperty("payload", out var payloadProp))
            {
                payload = payloadProp.Clone();
            }

            if (_handlers.TryGetValue(action, out var handler))
            {
                object? result = await handler(payload);
                if (!string.IsNullOrEmpty(callbackId))
                {
                    SendResponse(callbackId, success: true, result: result, error: null);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(callbackId))
                {
                    SendResponse(callbackId, success: false, result: null, error: $"Action '{action}' not registered.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebUIBridge Error] {ex}");
        }
    }

    public void SendResponse(string callbackId, bool success, object? result, string? error)
    {
        var response = new
        {
            type = "response",
            callbackId = callbackId,
            success = success,
            result = result,
            error = error
        };
        SendRawJson(JsonSerializer.Serialize(response));
    }

    public void SendEvent(string eventName, object? data)
    {
        var eventMsg = new
        {
            type = "event",
            eventName = eventName,
            data = data
        };
        SendRawJson(JsonSerializer.Serialize(eventMsg));
    }

    private void SendRawJson(string json)
    {
        try
        {
            _webView.PostWebMessageAsJson(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebUIBridge Post Error] {ex.Message}");
        }
    }
}
