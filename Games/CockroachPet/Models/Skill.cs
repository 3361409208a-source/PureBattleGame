using System;

namespace PureBattleGame.Games.CockroachPet
{
    public class Skill
    {
        public string Name { get; set; } = "";
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int NextLevelXp => Level * 100;
        public string Description { get; set; } = "";

        public bool GainExperience(int amount)
        {
            Experience += amount;
            if (Experience >= NextLevelXp)
            {
                Experience -= NextLevelXp;
                Level++;
                return true; // Leveled up
            }
            return false;
        }
    }
}
