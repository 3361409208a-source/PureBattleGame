namespace PureBattleGame.Games.CockroachPet;

/// <summary>
/// 摸鱼模式主题类型
/// </summary>
public enum BossModeTheme
{
    None,       // 无 - 只隐藏机器人，不显示伪装
    Excel,      // Excel表格
    CodeEditor, // VS Code风格代码编辑器
    Terminal,   // 命令行终端
    Word        // Word文档
}
