using System;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

public class Particle
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Dx { get; set; }
    public float Dy { get; set; }
    public Color Color { get; set; }
    public int Life { get; set; }
    public int MaxLife { get; set; }
    public float Size { get; set; }
    public bool IsActive { get; set; } = true;

    // 特效类型: "NORMAL", "SPARK", "SMOKE", "RING"
    public string Type { get; set; } = "NORMAL"; 

    public Particle(float x, float y, float dx, float dy, Color color, int life, float size, string type = "NORMAL")
    {
        X = x;
        Y = y;
        Dx = dx;
        Dy = dy;
        Color = color;
        Life = life;
        MaxLife = life;
        Size = size;
        Type = type;
    }

    public void Update()
    {
        X += Dx;
        Y += Dy;
        Life--;

        // 阻力
        Dx *= 0.95f;
        Dy *= 0.95f;

        if (Type == "SMOKE")
        {
            Size += 0.2f; // 烟雾扩散
            Dy -= 0.05f;  // 烟雾上升
        }
        else if (Type == "RING")
        {
            Size += 2.0f; // 冲击波扩散
        }

        if (Life <= 0) IsActive = false;
    }
}
