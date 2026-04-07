using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Drawing;

namespace PureBattleGame.Games.CockroachPet
{
    public class AppSettings
    {
        public string ApiKey { get; set; } = "";
        public bool EnableBossModeHotkey { get; set; } = true;
    }

    public class RobotData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Personality { get; set; } = "";
        public int PersonalityType { get; set; } = 0; // RobotPersonalityType
        public int CurrentEmotion { get; set; } = 0; // EmotionState
        public double ConsciousnessLevel { get; set; }
        public int Experience { get; set; }
        public List<string> LearnedInsights { get; set; } = new List<string>();
        public string InternalGuidelines { get; set; } = "";
        public Dictionary<string, Skill> Skills { get; set; } = new Dictionary<string, Skill>();
        public int Size { get; set; }
        public float SpeedMultiplier { get; set; }
        public int PrimaryColorR { get; set; }
        public int PrimaryColorG { get; set; }
        public int PrimaryColorB { get; set; }
        public List<string> CustomPhrases { get; set; } = new List<string>();

    }

    public static class PersistenceManager
    {
        private static string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CockroachPet", "robots.json");
        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CockroachPet", "settings.json");

        // AppSettings 配置
        public static AppSettings LoadAppSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new AppSettings();

                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings Load Error: {ex.Message}");
                return new AppSettings();
            }
        }

        public static void SaveAppSettings(AppSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings Save Error: {ex.Message}");
            }
        }

        public static string GetApiKey()
        {
            return LoadAppSettings().ApiKey;
        }

        public static void SetApiKey(string apiKey)
        {
            var settings = LoadAppSettings();
            settings.ApiKey = apiKey;
            SaveAppSettings(settings);
        }

        public static void SaveRobots(List<Robot> robots)
        {
            try
            {
                var directory = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

                var data = robots.Select(r => new RobotData
                {
                    Id = r.Id,
                    Name = r.Name,
                    Personality = r.Personality,
                    PersonalityType = (int)r.PersonalityType,
                    CurrentEmotion = (int)r.CurrentEmotion,
                    ConsciousnessLevel = r.ConsciousnessLevel,
                    Experience = r.Experience,
                    LearnedInsights = r.LearnedInsights,
                    InternalGuidelines = r.InternalGuidelines,
                    Skills = r.Skills,
                    Size = r.Size,
                    SpeedMultiplier = r.SpeedMultiplier,
                    PrimaryColorR = r.PrimaryColor.R,
                    PrimaryColorG = r.PrimaryColor.G,
                    PrimaryColorB = r.PrimaryColor.B,
                    CustomPhrases = r.CustomPhrases,

                }).ToList();

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Persistence Error: {ex.Message}");
            }
        }

        public static List<RobotData> LoadRobots()
        {
            try
            {
                if (!File.Exists(SavePath)) return new List<RobotData>();

                string json = File.ReadAllText(SavePath);
                return JsonSerializer.Deserialize<List<RobotData>>(json) ?? new List<RobotData>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Persistence Load Error: {ex.Message}");
                return new List<RobotData>();
            }
        }
    }
}
