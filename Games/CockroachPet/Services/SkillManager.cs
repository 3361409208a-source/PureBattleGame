using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PureBattleGame.Games.CockroachPet
{
    public static class SkillManager
    {
        private static string BasePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CockroachPet", "Skills");

        public static void SaveRobotSkills(Robot robot)
        {
            try
            {
                if (!Directory.Exists(BasePath)) Directory.CreateDirectory(BasePath);
                
                string fileName = $"skills_{robot.Id}_{robot.Name}.json";
                string fullPath = Path.Combine(BasePath, fileName);
                
                string json = JsonSerializer.Serialize(robot.Skills, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(fullPath, json);
                
                System.Diagnostics.Debug.WriteLine($"[SkillManager] Saved skills for {robot.Name} to {fullPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkillManager] Save Error: {ex.Message}");
            }
        }

        public static Dictionary<string, Skill> LoadRobotSkills(int robotId, string robotName)
        {
            try
            {
                string fileName = $"skills_{robotId}_{robotName}.json";
                string fullPath = Path.Combine(BasePath, fileName);
                
                if (File.Exists(fullPath))
                {
                    string json = File.ReadAllText(fullPath);
                    return JsonSerializer.Deserialize<Dictionary<string, Skill>>(json) ?? new Dictionary<string, Skill>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkillManager] Load Error: {ex.Message}");
            }
            return new Dictionary<string, Skill>();
        }
    }
}
