using System;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

public static class MonsterRenderer
{
    // 缓存静态 GDI+ 资源，避免每帧重复创建
    private static readonly Font _damageFont = new Font("Impact", 14, FontStyle.Bold);
    private static readonly StringFormat _centerFormat = new StringFormat { Alignment = StringAlignment.Center };
    private static readonly SolidBrush _damageBrush = new SolidBrush(Color.OrangeRed);
    private static readonly SolidBrush _eyeBrush = new SolidBrush(Color.Yellow);
    private static readonly SolidBrush _hpBgBrush = new SolidBrush(Color.Gray);
    private static readonly SolidBrush _hpRedBrush = new SolidBrush(Color.Red);

    public static void DrawMonster(Graphics g, Monster m)
    {
        if (!m.IsActive) return;

        int size = m.Size;
        float cx = m.X + size / 2;
        float cy = m.Y + size / 2;

        switch (m.Type)
        {
            case "SPIDER":
                using (var spiderBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                {
                    g.FillEllipse(spiderBrush, m.X + size * 0.1f, m.Y + size * 0.1f, size * 0.8f, size * 0.8f);
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
                using (var batBrush = new SolidBrush(Color.FromArgb(80, 0, 120)))
                {
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
                using (var bodyBrush = new SolidBrush(Color.FromArgb(180, 50, 50)))
                {
                    g.FillEllipse(bodyBrush, m.X, m.Y, size, size);
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
        float eyeSize = size * 0.15f;
        g.FillEllipse(_eyeBrush, m.X + size * 0.25f, m.Y + size * 0.3f, eyeSize, eyeSize);
        g.FillEllipse(_eyeBrush, m.X + size * 0.6f, m.Y + size * 0.3f, eyeSize, eyeSize);

        // 血条 (共通)
        if (m.HP < m.MaxHP)
        {
            float barWidth = size * 0.8f;
            float barHeight = 5;
            float barX = m.X + (size - barWidth) / 2;
            float barY = m.Y - 10;
            g.FillRectangle(_hpBgBrush, barX, barY, barWidth, barHeight);
            float hpPercent = Math.Clamp((float)m.HP / m.MaxHP, 0, 1);
            g.FillRectangle(_hpRedBrush, barX, barY, barWidth * hpPercent, barHeight);
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
            float floatOffset = (30 - m.DamageTextTimer) * 1.5f;
            g.DrawString(m.DamageText, _damageFont, _damageBrush, cx, m.Y - 20 - floatOffset, _centerFormat);
        }

        // 精英怪特效：金色光环
        if (m.IsElite)
        {
            using var elitePen = new Pen(Color.Gold, 3);
            g.DrawEllipse(elitePen, m.X - 3, m.Y - 3, size + 6, size + 6);
        }

        // 小Boss特效：橙色旋转光环
        if (m.IsMiniBoss)
        {
            float angle = (Environment.TickCount / 20f) % 360;
            using var miniBossPen = new Pen(Color.Orange, 4);
            using var miniBossBrush = new SolidBrush(Color.FromArgb(100, Color.Orange));
            g.DrawArc(miniBossPen, m.X - 8, m.Y - 8, size + 16, size + 16, angle, 270);
            g.FillEllipse(miniBossBrush, cx - 5, m.Y - 18, 10, 10);
        }

        // 大Boss特效：红色脉冲光环 + 皇冠标记
        if (m.IsBoss)
        {
            float pulse = 1 + (float)Math.Sin(Environment.TickCount / 100.0) * 0.2f;
            using var bossPen = new Pen(Color.Red, 5);
            using var glowBrush = new SolidBrush(Color.FromArgb(80, Color.Red));
            float glowSize = (size + 20) * pulse;
            float glowX = cx - glowSize / 2;
            float glowY = cy - glowSize / 2;
            g.FillEllipse(glowBrush, glowX, glowY, glowSize, glowSize);
            g.DrawEllipse(bossPen, m.X - 10, m.Y - 10, size + 20, size + 20);

            // 绘制皇冠
            using var crownBrush = new SolidBrush(Color.Yellow);
            PointF[] crown = {
                new PointF(cx - 12, m.Y - 15),
                new PointF(cx - 6, m.Y - 25),
                new PointF(cx, m.Y - 18),
                new PointF(cx + 6, m.Y - 25),
                new PointF(cx + 12, m.Y - 15)
            };
            g.FillPolygon(crownBrush, crown);
        }
    }
}
