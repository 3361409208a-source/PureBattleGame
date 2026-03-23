using System;
using System.IO;
using System.Text.Json;

namespace PureBattleGame.Core;

public class AppSettings
{
    public double DefaultOpacity { get; set; } = 0.95;
    public string HomeUrl { get; set; } = "https://www.bing.com";
    public bool AutoHideInTaskbar { get; set; } = true;
}

public static class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    public static AppSettings Current { get; private set; } = new();

    static SettingsManager()
    {
        Load();
    }

    public static void Load()
    {
        try {
            if (File.Exists(SettingsPath)) {
                string json = File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
            }
        } catch { }
    }

    public static void Save()
    {
        try {
            string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        } catch { }
    }
}
