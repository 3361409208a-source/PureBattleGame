using System;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

public class Mineral
{
    public float X { get; set; }
    public float Y { get; set; }
    public int Size { get; set; } = 15;
    public int Value { get; set; } = 100; // 基础价值
    public bool IsActive { get; set; } = true;
    public float Rotation { get; set; }
    public Color Color { get; set; } = Color.Cyan;
    public Robot? LockingRobot { get; set; } // 哪个机器人锁定了这个矿物，防止扎堆

    public Mineral(float x, float y)
    {
        X = x;
        Y = y;
        Rotation = (float)(new Random().NextDouble() * Math.PI * 2);
    }

    public void Draw(Graphics g)
    {
        if (!IsActive) return;

        // 绘制一个发光的晶体形状
        using var brush = new SolidBrush(Color.FromArgb(180, Color));
        using var lightBrush = new SolidBrush(Color.FromArgb(100, Color.White));
        
        PointF[] points = new PointF[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Rotation + i * (float)Math.PI / 3;
            points[i] = new PointF(
                X + (float)Math.Cos(angle) * (Size / 2),
                Y + (float)Math.Sin(angle) * (Size / 2)
            );
        }
        
        g.FillPolygon(brush, points);
        g.FillEllipse(lightBrush, X - 3, Y - 3, 6, 6);
        
        // 绘制边缘线
        using var pen = new Pen(Color.White, 1);
        g.DrawPolygon(pen, points);
    }
}
