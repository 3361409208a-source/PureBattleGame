using System;
using System.Drawing;

namespace PureBattleGame.Games.CockroachPet;

/// <summary>
/// 怪物渲染器
/// </summary>
public static class MonsterRenderer
{
    private const int PIXEL_SIZE = 4;

    public static void DrawMonster(Graphics g, Monster monster)
    {
        float x = monster.X;
        float y = monster.Y;
        int size = monster.Size;
        float scale = size / 96.0f;

        float centerX = x + size / 2f;
        float centerY = y + size / 2f;

        // 受击闪烁效果
        if (monster.HitFlashTimer > 0)
        {
            using (var flashBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
            {
                g.FillRectangle(flashBrush, x - 10, y - 10, size + 20, size + 20);
            }
        }

        // 绘制阴影
        using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
        {
            g.FillEllipse(shadowBrush, x + size * 0.1f, y + size * 0.8f, size * 0.8f, size * 0.2f);
        }

        // 绘制身体 - 深色方块风格怪物
        DrawBody(g, monster, centerX, centerY, scale);

        // 绘制眼睛
        DrawEyes(g, monster, centerX, centerY, scale);

        // 绘制尖刺/角
        DrawHorns(g, monster, centerX, centerY, scale);

        // 绘制血条
        DrawHealthBar(g, monster, x, y);

        // 绘制伤害文字
        DrawDamageText(g, monster, x, y);
    }

    private static void DrawBody(Graphics g, Monster monster, float cx, float cy, float scale)
    {
        int baseSize = (int)(PIXEL_SIZE * scale * 5);
        int w = baseSize * 4;
        int h = baseSize * 3;

        float x = cx - w / 2;
        float y = cy - h / 2;

        // 身体主体 - 深红色
        Color bodyColor = Color.FromArgb(139, 0, 0);
        Color darkBodyColor = Color.FromArgb(100, 0, 0);
        Color lightBodyColor = Color.FromArgb(180, 30, 30);

        using (var bodyBrush = new SolidBrush(bodyColor))
        using (var darkBrush = new SolidBrush(darkBodyColor))
        using (var lightBrush = new SolidBrush(lightBodyColor))
        {
            // 主体方块
            g.FillRectangle(bodyBrush, x, y, w, h);

            // 像素风格细节 - 高光
            g.FillRectangle(lightBrush, x + baseSize, y + baseSize, baseSize * 2, baseSize);
            g.FillRectangle(lightBrush, x + baseSize, y + baseSize * 2, baseSize, baseSize);

            // 阴影细节
            g.FillRectangle(darkBrush, x + baseSize * 3, y + baseSize * 2, baseSize, baseSize);
            g.FillRectangle(darkBrush, x + baseSize, y + baseSize * 2, baseSize * 2, baseSize);
        }

        // 身体边缘描边
        using (var borderPen = new Pen(Color.FromArgb(60, 0, 0), 2))
        {
            g.DrawRectangle(borderPen, x, y, w, h);
        }
    }

    private static void DrawEyes(Graphics g, Monster monster, float cx, float cy, float scale)
    {
        int eyeSize = (int)(PIXEL_SIZE * scale * 2);
        float eyeOffset = PIXEL_SIZE * scale * 6;

        float leftEyeX = cx - eyeOffset;
        float rightEyeX = cx + eyeOffset - eyeSize;
        float eyeY = cy - PIXEL_SIZE * scale * 2;

        // 眼睛闪烁动画
        bool isBlinking = monster.AnimationFrame == 3;

        if (isBlinking)
        {
            // 闭眼 - 一条线
            using (var eyePen = new Pen(Color.Black, 3))
            {
                g.DrawLine(eyePen, leftEyeX, eyeY + eyeSize / 2, leftEyeX + eyeSize, eyeY + eyeSize / 2);
                g.DrawLine(eyePen, rightEyeX, eyeY + eyeSize / 2, rightEyeX + eyeSize, eyeY + eyeSize / 2);
            }
        }
        else
        {
            // 睁眼 - 白色眼球 + 红色瞳孔
            using (var eyeWhiteBrush = new SolidBrush(Color.White))
            using (var pupilBrush = new SolidBrush(Color.FromArgb(200, 0, 0)))
            {
                // 左眼
                g.FillRectangle(eyeWhiteBrush, leftEyeX, eyeY, eyeSize, eyeSize);
                int pupilSize = eyeSize / 2;
                g.FillRectangle(pupilBrush, leftEyeX + pupilSize / 2, eyeY + pupilSize / 2, pupilSize, pupilSize);

                // 右眼
                g.FillRectangle(eyeWhiteBrush, rightEyeX, eyeY, eyeSize, eyeSize);
                g.FillRectangle(pupilBrush, rightEyeX + pupilSize / 2, eyeY + pupilSize / 2, pupilSize, pupilSize);
            }
        }
    }

    private static void DrawHorns(Graphics g, Monster monster, float cx, float cy, float scale)
    {
        int hornSize = (int)(PIXEL_SIZE * scale * 2);
        float hornY = cy - PIXEL_SIZE * scale * 8;

        using (var hornBrush = new SolidBrush(Color.FromArgb(80, 80, 80)))
        using (var hornTipBrush = new SolidBrush(Color.FromArgb(150, 150, 150)))
        {
            // 左角
            float leftHornX = cx - PIXEL_SIZE * scale * 5;
            g.FillRectangle(hornBrush, leftHornX, hornY, hornSize, hornSize * 3);
            g.FillRectangle(hornTipBrush, leftHornX + hornSize / 4, hornY - hornSize / 2, hornSize / 2, hornSize / 2);

            // 右角
            float rightHornX = cx + PIXEL_SIZE * scale * 3;
            g.FillRectangle(hornBrush, rightHornX, hornY, hornSize, hornSize * 3);
            g.FillRectangle(hornTipBrush, rightHornX + hornSize / 4, hornY - hornSize / 2, hornSize / 2, hornSize / 2);
        }
    }

    private static void DrawHealthBar(Graphics g, Monster monster, float x, float y)
    {
        int barWidth = monster.Size;
        int barHeight = 8;
        float barX = x;
        float barY = y - 20;

        // 背景
        using (var bgBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
        {
            g.FillRectangle(bgBrush, barX, barY, barWidth, barHeight);
        }

        // 血量比例
        float hpPercent = (float)monster.HP / monster.MaxHP;
        int hpWidth = (int)(barWidth * hpPercent);

        // 血量颜色
        Color hpColor = hpPercent > 0.5f ? Color.FromArgb(0, 200, 0) :
                       hpPercent > 0.25f ? Color.FromArgb(255, 200, 0) : Color.FromArgb(255, 0, 0);

        using (var hpBrush = new SolidBrush(hpColor))
        {
            g.FillRectangle(hpBrush, barX, barY, hpWidth, barHeight);
        }

        // 边框
        using (var borderPen = new Pen(Color.Black, 1))
        {
            g.DrawRectangle(borderPen, barX, barY, barWidth, barHeight);
        }

        // 血量文字
        using (var font = new Font("微软雅黑", 8, FontStyle.Bold))
        using (var brush = new SolidBrush(Color.White))
        {
            string hpText = $"{monster.HP}/{monster.MaxHP}";
            var size = g.MeasureString(hpText, font);
            g.DrawString(hpText, font, brush, barX + (barWidth - size.Width) / 2, barY - 2);
        }
    }

    private static void DrawDamageText(Graphics g, Monster monster, float x, float y)
    {
        if (monster.DamageTextTimer <= 0 || string.IsNullOrEmpty(monster.DamageText)) return;

        float alpha = monster.DamageTextTimer / 30f;
        int offsetY = (30 - monster.DamageTextTimer) / 2;

        using (var font = new Font("微软雅黑", 14, FontStyle.Bold))
        using (var brush = new SolidBrush(Color.FromArgb((int)(255 * alpha), 255, 50, 50)))
        using (var outlinePen = new Pen(Color.FromArgb((int)(255 * alpha), 150, 0, 0), 2))
        {
            var textX = x + monster.Size / 2;
            var textY = y - 40 - offsetY;

            // 描边效果
            var gp = new System.Drawing.Drawing2D.GraphicsPath();
            gp.AddString(monster.DamageText, font.FontFamily, (int)font.Style, font.Size,
                new PointF(textX - 20, textY), new StringFormat());
            g.DrawPath(outlinePen, gp);
            g.FillPath(brush, gp);
        }
    }
}
