using System;
using System.IO;
using System.Text.Json;

namespace PureBattleGame.Core;

public class AppSettings
{
    public double DefaultOpacity { get; set; } = 0.95;
    public string HomeUrl { get; set; } = "https://www.xiaoheiv.top";
    public bool AutoHideInTaskbar { get; set; } = true;
    public bool HideNameAndPersonality { get; set; } = false;
    public bool CurseModeByDefault { get; set; } = true;
    public string BattleMode { get; set; } = "近远交替";
    public string LanguageInteractionMode { get; set; } = "互骂吐槽";
    public string ActionInteractionMode { get; set; } = "近远交替";
    public bool AutoStart { get; set; } = false;
    public int RobotSize { get; set; } = 64;
    public int RobotSpeed { get; set; } = 100;
    public int SkillScale { get; set; } = 100;
    public int SoundVolume { get; set; } = 50;
    public int FightFrequency { get; set; } = 15;
    public bool EnableAiThinking { get; set; } = false;
    public int AiThoughtFrequency { get; set; } = 60;
    public bool IsWeaponMaster { get; set; } = false;
    public int RobotMaxHp { get; set; } = 1000;
    public bool IsGodMode { get; set; } = false;
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
                if (Current.HomeUrl.Contains("bing.com")) {
                    Current.HomeUrl = "https://www.xiaoheiv.top";
                    Save();
                }
            }
        } catch { }
    }

    public static void Save()
    {
        try {
            string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);

            // 同时写入 RobotPetSettings.txt 以保持向后兼容性
            string txtPath = Path.Combine(Path.GetTempPath(), "RobotPetSettings.txt");
            var lines = new[]
            {
                $"DefaultSize={Current.RobotSize}",
                $"DefaultSpeed={Current.RobotSpeed}",
                $"SkillScale={Current.SkillScale}",
                $"SoundVolume={Current.SoundVolume}",
                $"FightFreq={Current.FightFrequency}",
                $"EnableAi={Current.EnableAiThinking}",
                $"AiFreq={Current.AiThoughtFrequency}",
                $"WeaponMaster={Current.IsWeaponMaster}",
                $"AutoStart={Current.AutoStart}"
            };
            File.WriteAllLines(txtPath, lines);
        } catch { }
    }
}
