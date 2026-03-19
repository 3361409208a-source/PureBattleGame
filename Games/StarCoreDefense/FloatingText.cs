using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

public class FloatingText
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Dy { get; set; }
    public string Text { get; set; } = "";
    public Color TextColor { get; set; }
    public int Life { get; set; }
    public int MaxLife { get; set; }
    public bool IsActive => Life > 0;
}
