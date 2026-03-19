using System;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

public static class MonsterRenderer
{
    public static void DrawMonster(Graphics g, Monster m)
    {
        if (!m.IsActive) return;

        int size = m.Size;
        float cx = m.X + size / 2;
        float cy = m.Y + size / 2;

        switch (m.Type)
        {
            case "SPIDER":
                // 绘制蜘蛛风格
                using (var spiderBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                {
                    g.FillEllipse(spiderBrush, m.X + size * 0.1f, m.Y + size * 0.1f, size * 0.8f, size * 0.8f);
                    // 蜘蛛腿
                    using var legPen = new Pen(Color.FromArgb(60, 60, 60), 2);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = (float)(i * Math.PI / 4 + Math.Sin(m.AnimationFrame * 0.5f) * 0.2f);
                        float lx = cx + (float)Math.Cos(angle) * (size * 0.4f);
                        float ly = cy + (float)Math.Sin(angle) * (size * 0.4f);
                        float ex = cx + (float)Math.Cos(angle) * (size * 0.8f);
                        float ey = cy + (float)Math.Sin(angle) * (size * 0.8f);
                        g.DrawLine(legPen, lx, ly, ex, ey);
                    }
                }
                break;
            case "BAT":
                // 绘制蝙蝠/飞行器风格
                using (var batBrush = new SolidBrush(Color.FromArgb(80, 0, 120)))
                {
                    // 翅膀
                    float wingWave = (float)Math.Sin(m.AnimationFrame * 1.0f) * 15;
                    PointF[] points = {
                        new PointF(cx, cy),
                        new PointF(cx - size * 0.8f, cy - wingWave),
                        new PointF(cx - size * 0.5f, cy + size * 0.2f),
                        new PointF(cx + size * 0.5f, cy + size * 0.2f),
                        new PointF(cx + size * 0.8f, cy - wingWave)
                    };
                    g.FillPolygon(batBrush, points);
                    g.FillEllipse(batBrush, m.X + size * 0.25f, m.Y + size * 0.1f, size * 0.5f, size * 0.6f);
                }
                break;
            case "WORM":
                // 绘制蠕虫/机械虫风格
                using (var wormBrush = new SolidBrush(Color.FromArgb(0, 100, 50)))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float offX = (float)Math.Sin(m.AnimationFrame * 0.3f + i) * 10;
                        g.FillEllipse(wormBrush, m.X + offX + size * 0.2f, m.Y + i * (size * 0.2f), size * 0.6f, size * 0.3f);
                    }
                }
                break;
            default: // SLIME
                // 原有的红色史莱姆风格
                using (var bodyBrush = new SolidBrush(Color.FromArgb(180, 50, 50)))
                {
                    g.FillEllipse(bodyBrush, m.X, m.Y, size, size);
                    // 触手
                    using var tentacleBrush = new SolidBrush(Color.FromArgb(150, 30, 30));
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = (float)(i * Math.PI / 4 + m.AnimationFrame * 0.2);
                        float tx = cx + (float)Math.Cos(angle) * size * 0.5f;
                        float ty = cy + (float)Math.Sin(angle) * size * 0.5f;
                        g.FillEllipse(tentacleBrush, tx - 3, ty - 3, 6, 6);
                    }
                }
                break;
        }

        // 共通：眼睛
        using (var eyeBrush = new SolidBrush(Color.Yellow))
        {
            float eyeSize = size * 0.15f;
            g.FillEllipse(eyeBrush, m.X + size * 0.25f, m.Y + size * 0.3f, eyeSize, eyeSize);
            g.FillEllipse(eyeBrush, m.X + size * 0.6f, m.Y + size * 0.3f, eyeSize, eyeSize);
        }

        // 血条 (共通)
        if (m.HP < m.MaxHP)
        {
            float barWidth = size * 0.8f;
            float barHeight = 5;
            float barX = m.X + (size - barWidth) / 2;
            float barY = m.Y - 10;
            using var bgBrush = new SolidBrush(Color.Gray);
            g.FillRectangle(bgBrush, barX, barY, barWidth, barHeight);
            float hpPercent = (float)m.HP / m.MaxHP;
            using var hpBrush = new SolidBrush(Color.Red);
            g.FillRectangle(hpBrush, barX, barY, barWidth * hpPercent, barHeight);
        }

        // 受击闪烁 (共通)
        if (m.HitFlashTimer > 0)
        {
            using var flashBrush = new SolidBrush(Color.FromArgb(150, Color.White));
            g.FillEllipse(flashBrush, m.X, m.Y, size, size);
        }

        // 伤害文字 (共通)
        if (!string.IsNullOrEmpty(m.DamageText) && m.DamageTextTimer > 0)
        {
            using var font = new Font("Impact", 14, FontStyle.Bold);
            using var brush = new SolidBrush(Color.OrangeRed);
            float floatOffset = (30 - m.DamageTextTimer) * 1.5f;
            g.DrawString(m.DamageText, font, brush, cx, m.Y - 20 - floatOffset, new StringFormat { Alignment = StringAlignment.Center });
        }
    }
}
