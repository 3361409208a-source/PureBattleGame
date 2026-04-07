using System;
using System.Collections.Generic;
using System.Linq;

namespace PureBattleGame.Games.CockroachPet;

public partial class Robot
{
    // 技能系统
    public Dictionary<string, Skill> Skills { get; set; } = new Dictionary<string, Skill>();

    private void InitializeDefaultSkills()
    {
        Skills["逻辑推理"] = new Skill { Name = "逻辑推理", Description = "提高分析问题和深度思考的能力" };
        Skills["语言表达"] = new Skill { Name = "语言表达", Description = "使回复更加生动感性或充满幽默" };
        Skills["代码编写"] = new Skill { Name = "代码编写", Description = "在处理技术问题时更加专业" };
        Skills["情感模拟"] = new Skill { Name = "情感模拟", Description = "提升共情能力和性格表现力" };
    }

    public string GetSkillsDescription()
    {
        return string.Join(", ", Skills.Values.Select(s => $"{s.Name}(Lvl {s.Level})"));
    }

    public void SaveSkills()
    {
        SkillManager.SaveRobotSkills(this);
    }
}
