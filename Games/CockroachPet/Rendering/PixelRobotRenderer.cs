using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace PureBattleGame.Games.CockroachPet;

public static class PixelRobotRenderer
{
    private const int PIXEL_SIZE = 4;

    public static void DrawRobot(Graphics g, Robot robot)
    {
        float scale = robot.Size / 64.0f; // 缩放比例
        float x = robot.X + robot.ShakingOffset;
        float y = robot.Y;
        int size = robot.Size;
        bool facingRight = robot.FacingRight;

        // 如果透明度为0，完全不绘制
        if (robot.Opacity <= 0) return;

        // 保存原始状态
        var oldCompositingMode = g.CompositingMode;
        var oldCompositingQuality = g.CompositingQuality;

        // 应用透明度（如果不是完全不透明）
        if (robot.Opacity < 255)
        {
            // 设置合成模式以支持透明度
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
        }

        // 创建带透明度的颜色变换矩阵
        float alpha = robot.Opacity / 255f;
        ColorMatrix colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, alpha, 0},
            new float[] {0, 0, 0, 0, 1}
        });

        ImageAttributes imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

        // 记录世界坐标中心，用于绘制不受翻转影响的效果
        float worldCenterX = x + size / 2;
        float worldCenterY = y + size / 2;

        // 1. 绘制相对于机器人的 UI 元素（始终正向显示）
        DrawName(g, robot, x, y, alpha);
        DrawHealthBar(g, robot, x, y, alpha);
        DrawAlertBubble(g, robot, x, y, alpha);
        DrawEmojiBubble(g, robot, x, y, alpha);
        DrawChatBubble(g, robot, x, y, alpha);
        DrawThinkingIndicator(g, robot, x, y, alpha);
        DrawDamageText(g, robot, x, y, alpha);

        // 2. 绘制机器人本体（包含翻转和旋转动画）
        var state = g.Save();

        // 限制坐标在合理范围内，防止溢出
        x = Math.Clamp(x, -10000, 10000);
        y = Math.Clamp(y, -10000, 10000);
        size = Math.Clamp(size, 1, 1000);

        if (!facingRight)
        {
            g.TranslateTransform(x + size, y);
            g.ScaleTransform(-1, 1);
            x = 0;
            y = 0;
        }

        // 扔出去时的旋转
        if (robot.RotationAngle != 0)
        {
            // 限制旋转角度
            float rotAngle = robot.RotationAngle % 360;
            g.TranslateTransform(x + size / 2, y + size / 2);
            g.RotateTransform(rotAngle);
            g.TranslateTransform(-(x + size / 2), -(y + size / 2));
        }

        float centerX = x + size / 2;
        float centerY = y + size / 2;

        DrawTentacles(g, robot, centerX, centerY, scale, alpha);
        DrawBody(g, robot, centerX, centerY, scale, alpha);

        // 眼睛和天线单独处理旋转，确保围绕中心转
        if (robot.SpecialState == "SPINNING")
        {
            // 补偿翻转造成的旋转轴偏移
            float rotAngle = facingRight ? robot.RotationAngle : -robot.RotationAngle;

            // 围绕局部中心旋转
            var m = g.Transform;
            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(rotAngle);
            g.TranslateTransform(-centerX, -centerY);

            DrawEyes(g, robot, centerX, centerY, scale, alpha);
            DrawAntennas(g, robot, centerX, centerY, scale, alpha);

            g.Transform = m;
        }
        else
        {
            DrawEyes(g, robot, centerX, centerY, scale, alpha);
            DrawAntennas(g, robot, centerX, centerY, scale, alpha);
        }

        if (robot.SpecialState == "BLUSH")
        {
            DrawBlush(g, robot, centerX, centerY, alpha);
        }

        g.Restore(state);

        // 恢复原始合成模式
        g.CompositingMode = oldCompositingMode;

        // 远程攻击效果 - 多样化增强 (不受翻转影响)
        if (robot.IsFiringLaser)
        {
            DrawLaserAttack(g, robot, worldCenterX, worldCenterY, alpha);
        }

        // 格斗碰撞特效 (星型冲击与闪烁)
        if (robot.SpecialState == "SHAKING" && robot.DuelTimer > 0)
        {
            DrawDuelEffect(g, robot, worldCenterX, worldCenterY, alpha);
        }

        // 物理互动视觉 (触手抓住)
        if (robot.PhysicalAction != "NONE" && robot.PhysicalTarget != null)
        {
            DrawPhysicalInteraction(g, robot, worldCenterX, worldCenterY, alpha);
        }

        // 恢复原始合成模式
        g.CompositingMode = oldCompositingMode;

        // 远程攻击效果 - 多样化增强 (不受翻转影响)
        if (robot.IsFiringLaser)
        {
            var r = new Random();
            Color attackColor = robot.PrimaryColor;
            
            switch (robot.CurrentAttackType)
            {
                case "SHOCK": // 电能震撼 - 单根强力闪电
                    using (var shockPen = new Pen(Color.Cyan, 4))
                    using (var whitePen = new Pen(Color.White, 1))
                    {
                        // 减少线条数量，强调主干
                        DrawElectricArc(g, r, worldCenterX, worldCenterY, robot.LaserTargetX, robot.LaserTargetY, shockPen, whitePen);
                        g.DrawEllipse(new Pen(Color.White, 2), robot.LaserTargetX - 15, robot.LaserTargetY - 15, 30, 30);
                    }
                    break;

                case "INK_BLAST": // 墨汁弹 - 保持现状，属于块状攻击
                    using (var inkBrush = new SolidBrush(Color.FromArgb(230, 10, 10, 10)))
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float t = (float)(r.NextDouble()); 
                            float px = worldCenterX + (robot.LaserTargetX - worldCenterX) * t;
                            float py = worldCenterY + (robot.LaserTargetY - worldCenterY) * t;
                            float jitter = (1 - t) * 15;
                            float pSize = 8 + (1-t) * 12;
                            g.FillEllipse(inkBrush, px - pSize/2 + r.Next(-(int)jitter, (int)jitter), 
                                                 py - pSize/2 + r.Next(-(int)jitter, (int)jitter), pSize, pSize);
                        }
                        for (int i = 0; i < 10; i++)
                        {
                            float ang = (float)(r.NextDouble() * Math.PI * 2);
                            float d = (float)(r.NextDouble() * 30);
                            g.FillEllipse(inkBrush, robot.LaserTargetX + (float)Math.Cos(ang)*d - 4, 
                                                 robot.LaserTargetY + (float)Math.Sin(ang)*d - 4, 8, 8);
                        }
                    }
                    break;

                case "BURST": // 像素爆发 - 减少线条数，增加厚度
                    using (var burstBrush = new SolidBrush(Color.FromArgb(220, Color.OrangeRed)))
                    {
                        for (int i = 0; i < 6; i++) // 减少到 6 根
                        {
                            float angleOff = (float)(r.NextDouble() - 0.5) * 0.5f;
                            float baseAngle = (float)Math.Atan2(robot.LaserTargetY - worldCenterY, robot.LaserTargetX - worldCenterX);
                            float pDist = (float)Math.Sqrt(Math.Pow(robot.LaserTargetX - worldCenterX, 2) + Math.Pow(robot.LaserTargetY - worldCenterY, 2));
                            float tx = worldCenterX + (float)Math.Cos(baseAngle + angleOff) * pDist;
                            float ty = worldCenterY + (float)Math.Sin(baseAngle + angleOff) * pDist;
                            DrawPixelLine(g, burstBrush, worldCenterX, worldCenterY, tx, ty, 6); // 变粗
                        }
                    }
                    break;

                default: // LASER - 这一块改为单线条平滑激光，解决“线条太多”的问题
                    using (var glowPen = new Pen(Color.FromArgb(150, robot.PrimaryColor), 12)) 
                    using (var corePen = new Pen(Color.White, 4))
                    {
                        // 使用 GDI+ 自带的 DrawLine 以获得平滑感，减少重复线
                        g.DrawLine(glowPen, worldCenterX, worldCenterY, robot.LaserTargetX, robot.LaserTargetY);
                        g.DrawLine(corePen, worldCenterX, worldCenterY, robot.LaserTargetX, robot.LaserTargetY);
                        
                        g.FillEllipse(Brushes.White, worldCenterX - 10, worldCenterY - 10, 20, 20);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(200, robot.PrimaryColor)), robot.LaserTargetX - 12, robot.LaserTargetY - 12, 24, 24);
                    }
                    break;
            }
        }

        // 4. 格斗碰撞特效 (星型冲击与闪烁)
        if (robot.SpecialState == "SHAKING" && robot.DuelTimer > 0)
        {
            var r = new Random();
            using (var impactBrush = new SolidBrush(Color.FromArgb(200, Color.White)))
            using (var flashBrush = new SolidBrush(Color.FromArgb(100, Color.Yellow)))
            {
                // 冲击星
                PointF[] points = new PointF[10];
                float radiusOuter = 25 + r.Next(15);
                float radiusInner = 10;
                for (int i = 0; i < 10; i++)
                {
                    float angle = (float)(i * Math.PI * 2 / 10);
                    float rad = (i % 2 == 0) ? radiusOuter : radiusInner;
                    points[i] = new PointF(worldCenterX + (float)Math.Cos(angle) * rad, worldCenterY + (float)Math.Sin(angle) * rad);
                }
                g.FillPolygon(impactBrush, points);
                g.FillEllipse(flashBrush, worldCenterX - radiusOuter, worldCenterY - radiusOuter, radiusOuter * 2, radiusOuter * 2);
            }
        }

        // 5. 物理互动视觉 (触手抓住)
        if (robot.PhysicalAction != "NONE" && robot.PhysicalTarget != null)
        {
            DrawPhysicalInteraction(g, robot, worldCenterX, worldCenterY);
        }
    }

    private static void DrawPhysicalInteraction(Graphics g, Robot robot, float cx, float cy, float alpha = 1.0f)
    {
        var target = robot.PhysicalTarget;
        if (target == null) return;

        float tx = target.X + target.Size / 2;
        float ty = target.Y + target.Size / 2;

        using (var armPen = new Pen(Color.FromArgb((int)(255 * alpha), robot.PrimaryColor), 10))
        using (var glowPen = new Pen(Color.FromArgb((int)(120 * alpha), robot.PrimaryColor), 18))
        using (var suckerBrush = new SolidBrush(Color.FromArgb((int)(220 * alpha), Color.White)))
        {
            armPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            armPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            armPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            var r = new Random();
            float dist = (float)Math.Sqrt(Math.Pow(tx - cx, 2) + Math.Pow(ty - cy, 2));
            float angle = (float)Math.Atan2(ty - cy, tx - cx);

            for (int j = 0; j < 2; j++)
            {
                float sideAngle = angle + (j == 0 ? -0.5f : 0.5f);
                float startX = cx + (float)Math.Cos(sideAngle) * 12;
                float startY = cy + (float)Math.Sin(sideAngle) * 12;

                PointF[] pts = new PointF[4];
                pts[0] = new PointF(startX, startY);

                float wave = (float)Math.Sin(DateTime.Now.Millisecond * 0.01 + j) * 35;
                float midX = cx + (tx - cx) * 0.5f + (float)Math.Cos(angle + Math.PI/2) * wave;
                float midY = cy + (ty - cy) * 0.5f + (float)Math.Sin(angle + Math.PI/2) * wave;

                pts[1] = new PointF(midX, midY);
                pts[2] = new PointF(tx + (float)r.Next(-15, 15), ty + (float)r.Next(-15, 15));
                pts[3] = new PointF(tx, ty);

                g.DrawCurve(glowPen, pts);
                g.DrawCurve(armPen, pts);

                // 吸盘
                for (int i = 1; i < pts.Length - 1; i++)
                {
                    g.FillEllipse(suckerBrush, pts[i].X - 5, pts[i].Y - 5, 10, 10);
                }

                g.FillEllipse(new SolidBrush(Color.FromArgb((int)(255 * alpha), robot.PrimaryColor)), tx-8, ty-8, 16, 16);
            }
        }
    }

    private static void DrawLaserAttack(Graphics g, Robot robot, float worldCenterX, float worldCenterY, float alpha = 1.0f)
    {
        if (robot.TargetRobot == null) return;

        // 限制坐标在有效范围内
        worldCenterX = Math.Clamp(worldCenterX, -10000, 10000);
        worldCenterY = Math.Clamp(worldCenterY, -10000, 10000);
        float targetX = Math.Clamp(robot.LaserTargetX, -10000, 10000);
        float targetY = Math.Clamp(robot.LaserTargetY, -10000, 10000);

        using (var laserPen = new Pen(Color.FromArgb((int)(200 * alpha), 255, 50, 50), 4))
        using (var corePen = new Pen(Color.FromArgb((int)(255 * alpha), 255, 255, 255), 2))
        {
            // 激光主体
            g.DrawLine(laserPen, worldCenterX, worldCenterY, targetX, targetY);
            // 激光核心
            g.DrawLine(corePen, worldCenterX, worldCenterY, targetX, targetY);

            // 起点发光效果
            using (var glowBrush = new SolidBrush(Color.FromArgb((int)(100 * alpha), 255, 100, 100)))
            {
                g.FillEllipse(glowBrush, worldCenterX - 8, worldCenterY - 8, 16, 16);
            }

            // 终点击中效果
            using (var hitBrush = new SolidBrush(Color.FromArgb((int)(150 * alpha), 255, 200, 200)))
            {
                g.FillEllipse(hitBrush, targetX - 6, targetY - 6, 12, 12);
            }
        }
    }

    private static void DrawDuelEffect(Graphics g, Robot robot, float worldCenterX, float worldCenterY, float alpha = 1.0f)
    {
        if (robot.DuelTarget == null) return;

        float targetCenterX = robot.DuelTarget.X + robot.DuelTarget.Size / 2;
        float targetCenterY = robot.DuelTarget.Y + robot.DuelTarget.Size / 2;

        // 绘制对决连线
        using (var duelPen = new Pen(Color.FromArgb((int)(180 * alpha), 255, 100, 100), 3))
        using (var sparkPen = new Pen(Color.FromArgb((int)(255 * alpha), 255, 255, 0), 1))
        {
            duelPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            g.DrawLine(duelPen, worldCenterX, worldCenterY, targetCenterX, targetCenterY);

            // 中心碰撞点
            float midX = (worldCenterX + targetCenterX) / 2;
            float midY = (worldCenterY + targetCenterY) / 2;

            // 火花效果
            var r = new Random();
            for (int i = 0; i < 8; i++)
            {
                float sx = midX + (float)(r.NextDouble() - 0.5) * 30;
                float sy = midY + (float)(r.NextDouble() - 0.5) * 30;
                g.DrawLine(sparkPen, midX, midY, sx, sy);
            }

            // 碰撞点发光
            using (var glowBrush = new SolidBrush(Color.FromArgb((int)(120 * alpha), 255, 200, 100)))
            {
                g.FillEllipse(glowBrush, midX - 10, midY - 10, 20, 20);
            }
        }
    }

    private static void DrawElectricArc(Graphics g, Random r, float x1, float y1, float x2, float y2, Pen mainPen, Pen corePen)
    {
        float curX = x1;
        float curY = y1;
        int segments = 8;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float jitter = r.Next(-25, 25) * (1-t/2);
            float nextX = x1 + (x2 - x1) * t + jitter;
            float nextY = y1 + (y2 - y1) * t + jitter;
            if (i == segments) { nextX = x2; nextY = y2; }

            g.DrawLine(mainPen, curX, curY, nextX, nextY);
            g.DrawLine(corePen, curX, curY, nextX, nextY);
            
            if (r.Next(100) < 40) g.FillRectangle(Brushes.White, nextX - 3, nextY - 3, 6, 6);

            curX = nextX;
            curY = nextY;
        }
    }

    private static void DrawChatBubble(Graphics g, Robot robot, float rx, float ry)
    {
        if (robot.ChatTimer <= 0 || string.IsNullOrEmpty(robot.ChatText)) return;

        using var font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
        float maxWidth = 150; // 气泡最大宽度
        
        // 测量带换行限制的尺寸
        var rawSize = g.MeasureString(robot.ChatText, font, (int)maxWidth);
        
        float bx = rx + robot.Size / 2;
        float by = ry - rawSize.Height - 30; // 根据文字高度动态调整位置
        
        RectangleF bubbleRect = new RectangleF(bx - rawSize.Width / 2 - 10, by - rawSize.Height / 2 - 5, rawSize.Width + 20, rawSize.Height + 10);
        
        // 绘制气泡背景
        using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        g.FillRoundedRectangle(shadowBrush, bubbleRect.X + 2, bubbleRect.Y + 2, bubbleRect.Width, bubbleRect.Height, 8);
        
        using var bgBrush = new SolidBrush(Color.White);
        g.FillRoundedRectangle(bgBrush, bubbleRect.X, bubbleRect.Y, bubbleRect.Width, bubbleRect.Height, 8);
        
        using var borderPen = new Pen(Color.FromArgb(200, 200, 200), 1);
        g.DrawRoundedRectangle(borderPen, bubbleRect.X, bubbleRect.Y, bubbleRect.Width, bubbleRect.Height, 8);
        
        using var textBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
        // 使用 StringFormat 处理自动换行
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(robot.ChatText, font, textBrush, bubbleRect, format);
        
        // 小尾巴
        PointF[] tail = {
            new PointF(bx - 5, bubbleRect.Bottom),
            new PointF(bx + 5, bubbleRect.Bottom),
            new PointF(bx, bubbleRect.Bottom + 6)
        };
        g.FillPolygon(Brushes.White, tail);
    }

    private static void DrawThinkingIndicator(Graphics g, Robot robot, float rx, float ry)
    {
        if (!robot.IsThinking) return;

        float bx = rx + robot.Size / 2 + 15;
        float by = ry + 10;
        
        int pulse = (int)(DateTime.Now.Millisecond / 333) % 3;
        using var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        
        for (int i = 0; i <= pulse; i++)
        {
            g.FillRectangle(brush, bx + i * 6, by, 3, 3);
        }
    }

    // 辅助方法：绘制圆角矩形
    public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    {
        using var path = GetRoundedRectPath(x, y, width, height, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, float x, float y, float width, float height, float radius)
    {
        using var path = GetRoundedRectPath(x, y, width, height, radius);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(float x, float y, float width, float height, float radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        float d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseAllFigures();
        return path;
    }

    private static void DrawEmojiBubble(Graphics g, Robot robot, float rx, float ry)
    {
        // 绘制情绪气泡（如果情绪不是平静）
        if (robot.CurrentEmotion != EmotionState.Neutral)
        {
            float bx = rx + robot.Size - 5;
            float by = ry - 25;

            // 情绪背景圆圈
            using var emotionBrush = new SolidBrush(robot.GetEmotionColor());
            g.FillEllipse(emotionBrush, bx - 5, by - 5, 22, 22);
            g.DrawEllipse(Pens.White, bx - 5, by - 5, 22, 22);

            using var font = new Font("Segoe UI Emoji", 10);
            g.DrawString(robot.GetEmotionEmoji(), font, Brushes.White, bx, by);
        }

        // 绘制表情气泡（如果有）
        if (robot.EmojiBubbleTimer > 0)
        {
            float bx = rx + robot.Size - 10;
            float by = ry - 45;

            using var font = new Font("Segoe UI Emoji", 14);
            g.DrawString(robot.CurrentEmoji, font, Brushes.White, bx, by);
        }
    }

    private static void DrawBlush(Graphics g, Robot robot, float cx, float cy, float alpha = 1.0f)
    {
        // 限制坐标在有效范围内
        cx = Math.Clamp(cx, -10000, 10000);
        cy = Math.Clamp(cy, -10000, 10000);
        using var blushBrush = new SolidBrush(Color.FromArgb((int)(150 * alpha), 255, 182, 193));
        g.FillEllipse(blushBrush, cx - 15, cy, 10, 6);
        g.FillEllipse(blushBrush, cx + 5, cy, 10, 6);
    }

    private static void DrawAlertBubble(Graphics g, Robot robot, float rx, float ry, float alpha = 1.0f)
    {
        if (!robot.IsWarning || string.IsNullOrEmpty(robot.AlertMessage)) return;

        // 浮动动画
        float floatOffset = (float)Math.Sin(robot.WarningTimer * 0.1) * 5;
        float bx = rx + robot.Size / 2;
        float by = ry - 40 + floatOffset;

        using var font = new Font("Consolas", 9, FontStyle.Bold);
        var size = g.MeasureString(robot.AlertMessage, font);

        // 气泡背景 (带圆角和阴影)
        RectangleF bubbleRect = new RectangleF(bx - size.Width / 2 - 8, by - size.Height / 2 - 4, size.Width + 16, size.Height + 8);

        using var shadowBrush = new SolidBrush(Color.FromArgb((int)(100 * alpha), 0, 0, 0));
        g.FillRectangle(shadowBrush, bubbleRect.X + 3, bubbleRect.Y + 3, bubbleRect.Width, bubbleRect.Height);

        // 颜色根据状态变化：Claude 确认显示黄色，错误显示红色
        Color bubbleColor = robot.StatusMessage == "WAITING" ? Color.Gold : Color.Red;
        using var bubbleBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), bubbleColor));
        g.FillRectangle(bubbleBrush, bubbleRect);

        using var textBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Black));
        g.DrawString(robot.AlertMessage, font, textBrush, bx, by, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

        // 连接宠物的小箭头
        PointF[] arrow = {
            new PointF(bx - 5, bubbleRect.Bottom),
            new PointF(bx + 5, bubbleRect.Bottom),
            new PointF(bx, bubbleRect.Bottom + 8)
        };
        g.FillPolygon(bubbleBrush, arrow);
    }

    private static void DrawTentacles(Graphics g, Robot robot, float cx, float cy, float scale, float alpha = 1.0f)
    {
        Color tColor = robot.IsDead ? Color.FromArgb((int)(100 * alpha), 100, 100, 100) : Color.FromArgb((int)(255 * alpha), robot.SecondaryColor);
        using var tentacleBrush = new SolidBrush(tColor);

        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(i * Math.PI / 4 + robot.TentacleOffsets[i] * 0.1);
            float wave = (float)Math.Sin(robot.TentacleOffsets[i] + i) * 5 * scale;

            float startX = cx + (float)Math.Cos(angle) * 15 * scale;
            float startY = cy + (float)Math.Sin(angle) * 15 * scale;

            float length = (20 + wave) * scale;
            float endX = startX + (float)Math.Cos(angle) * length;
            float endY = startY + (float)Math.Sin(angle) * length;

            DrawPixelLine(g, tentacleBrush, startX, startY, endX, endY, (int)Math.Max(1, 3 * scale));
            g.FillRectangle(tentacleBrush, endX - 2 * scale, endY - 2 * scale, 4 * scale, 4 * scale);
        }
    }

    private static void DrawBody(Graphics g, Robot robot, float cx, float cy, float scale, float alpha = 1.0f)
    {
        Color pColor = robot.IsDead ? Color.FromArgb((int)(130 * alpha), 130, 130, 130) : Color.FromArgb((int)(255 * alpha), robot.PrimaryColor);
        Color sColor = robot.IsDead ? Color.FromArgb((int)(90 * alpha), 90, 90, 90) : Color.FromArgb((int)(255 * alpha), robot.SecondaryColor);
        using var bodyBrush = new SolidBrush(pColor);
        using var bodyDarkBrush = new SolidBrush(sColor);

        float pSize = PIXEL_SIZE * scale;

        for (int dx = -12; dx <= 12; dx++)
        {
            for (int dy = -12; dy <= 12; dy++)
            {
                if (dx * dx + dy * dy <= 144)
                {
                    float px = cx + dx * pSize / 2;
                    float py = cy + dy * pSize / 2;

                    var brush = (dx * dx + dy * dy > 100) ? bodyDarkBrush : bodyBrush;
                    
                    // 受击红闪覆盖
                    if (robot.DamageFeedbackTimer > 0)
                    {
                        int localAlpha = Math.Min(255, robot.DamageFeedbackTimer * 4);
                        using var hitBrush = new SolidBrush(Color.FromArgb(localAlpha, Color.Red));
                        g.FillRectangle(brush, px - pSize / 2, py - pSize / 2, pSize, pSize);
                        g.FillRectangle(hitBrush, px - pSize / 2, py - pSize / 2, pSize, pSize);
                    }
                    else
                    {
                        g.FillRectangle(brush, px - pSize / 2, py - pSize / 2, pSize, pSize);
                    }
                }
            }
        }

        using var coreBrush = new SolidBrush(Color.FromArgb((int)(200 * alpha), 255, 255, 255));
        float corePulse = 1 + (float)Math.Sin(robot.AnimationFrame * Math.PI / 2) * 0.2f;
        float coreSize = 6 * corePulse * scale;
        g.FillRectangle(coreBrush, cx - coreSize / 2, cy - coreSize / 2 + 5 * scale, coreSize, coreSize);
    }

    private static void DrawEyes(Graphics g, Robot robot, float cx, float cy, float scale, float alpha = 1.0f)
    {
        float eyeY = cy - 5 * scale;
        float leftEyeX = cx - 8 * scale;
        float rightEyeX = cx + 8 * scale;

        int blinkFrame = robot.AnimationFrame;
        bool isBlinking = blinkFrame == 2 || robot.SpecialState == "SLEEPY";

        if (robot.SpecialState == "ANGRY")
        {
            DrawAngryEyes(g, robot, cx, cy, scale, alpha);
            return;
        }

        float eyeHeight = (isBlinking ? 2 : 8) * scale;
        float eyeWidth = 10 * scale;

        using var eyeWhiteBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.White));
        using var eyeBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), robot.EyeColor));
        using var pupilBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Black));
        using var heartBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.HotPink));

        // 左眼
        DrawPixelEllipse(g, eyeWhiteBrush, leftEyeX, eyeY, eyeWidth, eyeHeight);
        if (!isBlinking && !robot.IsDead)
        {
            if (robot.SpecialState == "HEART_EYES")
            {
                DrawHeart(g, heartBrush, leftEyeX, eyeY, 8 * scale);
            }
            else
            {
                DrawPixelEllipse(g, eyeBrush, leftEyeX + 1 * scale, eyeY, 6 * scale, 6 * scale);
                g.FillRectangle(pupilBrush, leftEyeX + 1 * scale, eyeY - 1 * scale, 2 * scale, 4 * scale);
            }
        }

        // 右眼
        DrawPixelEllipse(g, eyeWhiteBrush, rightEyeX, eyeY, eyeWidth, eyeHeight);
        if (!isBlinking && !robot.IsDead)
        {
            if (robot.SpecialState == "HEART_EYES")
            {
                DrawHeart(g, heartBrush, rightEyeX, eyeY, 8 * scale);
            }
            else
            {
                DrawPixelEllipse(g, eyeBrush, rightEyeX + 1 * scale, eyeY, 6 * scale, 6 * scale);
                g.FillRectangle(pupilBrush, rightEyeX + 1 * scale, eyeY - 1 * scale, 2 * scale, 4 * scale);
            }
        }
    }

    private static void DrawAngryEyes(Graphics g, Robot robot, float cx, float cy, float scale, float alpha = 1.0f)
    {
        using var eyeBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.White));
        using var pupilBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Red));
        float eyeY = cy - 5 * scale;
        float leftEyeX = cx - 8 * scale;
        float rightEyeX = cx + 8 * scale;

        DrawPixelEllipse(g, eyeBrush, leftEyeX, eyeY, 10 * scale, 8 * scale);
        DrawPixelEllipse(g, pupilBrush, leftEyeX, eyeY, 6 * scale, 6 * scale);

        DrawPixelEllipse(g, eyeBrush, rightEyeX, eyeY, 10 * scale, 8 * scale);
        DrawPixelEllipse(g, pupilBrush, rightEyeX, eyeY, 6 * scale, 6 * scale);
    }

    private static void DrawAntennas(Graphics g, Robot robot, float cx, float cy, float scale, float alpha = 1.0f)
    {
        float antennaHeight = 12 * scale;
        float spread = 6 * scale;

        Color aColor = robot.IsDead ? Color.FromArgb((int)(100 * alpha), 100, 100, 100) : Color.FromArgb((int)(255 * alpha), robot.SecondaryColor);
        using var antennaBrush = new SolidBrush(aColor);

        DrawPixelLine(g, antennaBrush, cx - spread, cy - antennaHeight / 2, cx - spread / 2, cy - antennaHeight, (int)(2 * scale));
        DrawPixelLine(g, antennaBrush, cx + spread, cy - antennaHeight / 2, cx + spread / 2, cy - antennaHeight, (int)(2 * scale));

        g.FillRectangle(antennaBrush, cx - spread / 2 - 2 * scale, cy - antennaHeight - 2 * scale, 4 * scale, 4 * scale);
        g.FillRectangle(antennaBrush, cx + spread / 2 - 2 * scale, cy - antennaHeight - 2 * scale, 4 * scale, 4 * scale);
    }

    private static void DrawChatBubble(Graphics g, Robot robot, float rx, float ry, float alpha = 1.0f)
    {
        if (robot.ChatTimer <= 0 || string.IsNullOrEmpty(robot.ChatMessage)) return;

        using var font = new Font("Microsoft YaHei", 9);
        var size = g.MeasureString(robot.ChatMessage, font);

        float bx = rx + robot.Size / 2;
        float by = ry - 50;

        // 气泡背景
        RectangleF bubbleRect = new RectangleF(bx - size.Width / 2 - 8, by - size.Height / 2 - 4, size.Width + 16, size.Height + 8);

        using var shadowBrush = new SolidBrush(Color.FromArgb((int)(100 * alpha), 0, 0, 0));
        g.FillRectangle(shadowBrush, bubbleRect.X + 3, bubbleRect.Y + 3, bubbleRect.Width, bubbleRect.Height);

        using var bubbleBrush = new SolidBrush(Color.FromArgb((int)(230 * alpha), 240, 240, 240));
        g.FillRectangle(bubbleBrush, bubbleRect);

        using var textBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Black));
        g.DrawString(robot.ChatMessage, font, textBrush, bx, by, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }

    private static void DrawEmojiBubble(Graphics g, Robot robot, float rx, float ry, float alpha = 1.0f)
    {
        if (robot.EmojiBubbleTimer <= 0 || string.IsNullOrEmpty(robot.EmojiBubble)) return;

        using var font = new Font("Segoe UI Emoji", 12);
        float bx = rx + robot.Size / 2;
        float by = ry - 35;

        using var textBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Black));
        g.DrawString(robot.EmojiBubble, font, textBrush, bx, by, new StringFormat { Alignment = StringAlignment.Center });
    }

    private static void DrawThinkingIndicator(Graphics g, Robot robot, float rx, float ry, float alpha = 1.0f)
    {
        if (!robot.IsAiSpeaking) return;

        float bx = rx + robot.Size / 2;
        float by = ry - 30;

        // 三个跳动的小点
        for (int i = 0; i < 3; i++)
        {
            float offset = (float)Math.Sin((Environment.TickCount / 200.0) + i * 1.5) * 3;
            using var brush = new SolidBrush(Color.FromArgb((int)(200 * alpha), 100, 200, 255));
            g.FillEllipse(brush, bx - 8 + i * 8, by + offset, 6, 6);
        }
    }
    private static void DrawHeart(Graphics g, Brush brush, float x, float y, float size)
    {
        float s = size / 2;
        PointF[] points = {
            new PointF(x, y + s/2),
            new PointF(x - s, y - s/2),
            new PointF(x - s/2, y - s),
            new PointF(x, y - s/2),
            new PointF(x + s/2, y - s),
            new PointF(x + s, y - s/2)
        };
        g.FillPolygon(brush, points);
    }

    private static void DrawAngryEyes(Graphics g, Robot robot, float cx, float cy, float scale)
    {
        using var eyeBrush = new SolidBrush(Color.Red);
        using var pen = new Pen(eyeBrush, 3 * scale);
        
        // 愤怒的 V 型眼
        g.DrawLine(pen, cx - 12 * scale, cy - 10 * scale, cx - 4 * scale, cy - 4 * scale);
        g.DrawLine(pen, cx - 12 * scale, cy - 4 * scale, cx - 4 * scale, cy - 10 * scale); // 左眼 X
        
        g.DrawLine(pen, cx + 4 * scale, cy - 10 * scale, cx + 12 * scale, cy - 4 * scale);
        g.DrawLine(pen, cx + 4 * scale, cy - 4 * scale, cx + 12 * scale, cy - 10 * scale); // 右眼 X
    }

    private static void DrawName(Graphics g, Robot robot, float rx, float ry, float alpha = 1.0f)
    {
        if (string.IsNullOrEmpty(robot.Name)) return;

        using var font = new Font("Consolas", 8, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.White));
        using var shadowBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Black));
        var personalityColor = robot.GetPersonalityColor();
        using var personalityBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), personalityColor));

        float textX = rx + robot.Size / 2;
        float textY = ry - 20;

        // 绘制名称阴影
        g.DrawString(robot.Name, font, shadowBrush, textX + 1, textY + 1,
            new StringFormat { Alignment = StringAlignment.Center });
        g.DrawString(robot.Name, font, brush, textX, textY,
            new StringFormat { Alignment = StringAlignment.Center });

        // 绘制个性标签
        using var smallFont = new Font("Microsoft YaHei", 7);
        string personalityLabel = $"{robot.GetPersonalityEmoji()} {robot.GetPersonalityName()}";
        g.DrawString(personalityLabel, smallFont, shadowBrush, textX + 1, textY + 13,
            new StringFormat { Alignment = StringAlignment.Center });
        g.DrawString(personalityLabel, smallFont, personalityBrush, textX, textY + 12,
            new StringFormat { Alignment = StringAlignment.Center });
    }

    private static void DrawHealthBar(Graphics g, Robot robot, float rx, float ry, float alpha = 1.0f)
    {
        if (robot.IsDead) return;
        float barWidth = robot.Size * 0.8f;
        float barHeight = 4;
        float bx = rx + (robot.Size - barWidth) / 2;
        float by = ry - 8;

        // 确保值在有效范围内，防止溢出
        bx = Math.Clamp(bx, -10000, 10000);
        by = Math.Clamp(by, -10000, 10000);
        barWidth = Math.Clamp(barWidth, 0, 10000);
        barHeight = Math.Clamp(barHeight, 0, 1000);

        // 背景
        using var bgBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Gray));
        g.FillRectangle(bgBrush, bx, by, barWidth, barHeight);

        // 血条
        float hpPercent = (float)robot.HP / robot.MaxHP;
        hpPercent = Math.Clamp(hpPercent, 0, 1);
        Color hpColor = hpPercent > 0.5 ? Color.Lime : (hpPercent > 0.2 ? Color.Yellow : Color.Red);
        using var hpBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), hpColor));
        g.FillRectangle(hpBrush, bx, by, barWidth * hpPercent, barHeight);

        // 边框
        using var borderPen = new Pen(Color.FromArgb((int)(255 * alpha), Color.Black));
        g.DrawRectangle(borderPen, bx, by, barWidth, barHeight);
    }

    private static void DrawDamageText(Graphics g, Robot robot, float rx, float ry, float alpha = 1.0f)
    {
        if (robot.DamageTextTimer <= 0) return;

        float localAlpha = Math.Min(1.0f, robot.DamageTextTimer / 30f);
        using var font = new Font("Impact", 14, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb((int)(localAlpha * 255), Color.OrangeRed));

        float floatOffset = (45 - robot.DamageTextTimer) * 1.5f;
        float tx = rx + robot.Size / 2;
        float ty = ry - 20 - floatOffset;

        using var shadowBrush = new SolidBrush(Color.FromArgb((int)(localAlpha * 255), Color.Black));
        g.DrawString(robot.LastDamageText, font, shadowBrush, tx + 1, ty + 1, new StringFormat { Alignment = StringAlignment.Center });
        g.DrawString(robot.LastDamageText, font, brush, tx, ty, new StringFormat { Alignment = StringAlignment.Center });
    }

    private static void DrawPixelLine(Graphics g, Brush brush, float x1, float y1, float x2, float y2, int thickness)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float steps = Math.Max(Math.Abs(dx), Math.Abs(dy));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / steps;
            float x = x1 + dx * t;
            float y = y1 + dy * t;
            g.FillRectangle(brush, x - thickness / 2, y - thickness / 2, thickness, thickness);
        }
    }

    private static void DrawPixelEllipse(Graphics g, Brush brush, float cx, float cy, float w, float h)
    {
        int steps = 16;
        for (int i = 0; i < steps; i++)
        {
            float angle = (float)(i * 2 * Math.PI / steps);
            float x = cx + (float)Math.Cos(angle) * w / 2;
            float y = cy + (float)Math.Sin(angle) * h / 2;
            g.FillRectangle(brush, x - 2, y - 2, 4, 4);
        }
    }

    /// <summary>
    /// 绘制摸鱼模式伪装界面 - 根据主题类型
    /// </summary>
    public static void DrawBossModeIndicator(Graphics g, int screenWidth, int screenHeight, BossModeTheme theme)
    {
        switch (theme)
        {
            case BossModeTheme.None:
                // 无 - 不绘制任何伪装界面，只隐藏机器人
                break;
            case BossModeTheme.CodeEditor:
                DrawCodeEditorTheme(g, screenWidth, screenHeight);
                break;
            case BossModeTheme.Terminal:
                DrawTerminalTheme(g, screenWidth, screenHeight);
                break;
            case BossModeTheme.Word:
                DrawWordTheme(g, screenWidth, screenHeight);
                break;
            case BossModeTheme.Excel:
            default:
                DrawExcelTheme(g, screenWidth, screenHeight);
                break;
        }
    }

    /// <summary>
    /// 绘制Excel表格风格
    /// </summary>
    private static void DrawExcelTheme(Graphics g, int screenWidth, int screenHeight)
    {
        int width = Math.Min(400, screenWidth / 3);
        int height = Math.Min(300, screenHeight / 3);
        int x = screenWidth - width - 20;
        int y = screenHeight - height - 40;

        using (var bgBrush = new SolidBrush(Color.FromArgb(245, 255, 245)))
        using (var headerBrush = new SolidBrush(Color.FromArgb(34, 139, 34)))
        using (var gridPen = new Pen(Color.FromArgb(200, 200, 200), 1))
        using (var textBrush = new SolidBrush(Color.FromArgb(50, 50, 50)))
        using (var headerFont = new Font("Microsoft YaHei", 11, FontStyle.Bold))
        using (var cellFont = new Font("Consolas", 9))
        {
            g.FillRectangle(bgBrush, x, y, width, height);
            g.FillRectangle(headerBrush, x, y, width, 28);
            g.DrawString("Microsoft Excel - 工作表1", headerFont, Brushes.White, x + 8, y + 5);

            int rowHeight = 22;
            int colWidth = 80;
            int startY = y + 50;

            for (int col = 0; col < (width - 40) / colWidth; col++)
            {
                char colLetter = (char)('A' + col);
                int colX = x + 40 + col * colWidth;
                g.FillRectangle(Brushes.LightGray, colX, startY - 22, colWidth - 1, 20);
                g.DrawString(colLetter.ToString(), cellFont, textBrush, colX + colWidth / 2 - 5, startY - 20);
            }

            Random rand = new Random(42);
            string[] fakeData = { "1205", "3842", "9527", "4631", "7890", "2156", "6309" };
            string[] fakeFormulas = { "=SUM(A1:A10)", "=AVERAGE(B2:B8)", "=MAX(C1:C5)", "=COUNT(D:D)" };

            for (int row = 0; row < (height - 80) / rowHeight && row < 10; row++)
            {
                int rowY = startY + row * rowHeight;
                g.FillRectangle(Brushes.LightGray, x + 5, rowY, 32, rowHeight - 1);
                g.DrawString((row + 1).ToString(), cellFont, textBrush, x + 15, rowY + 3);

                for (int col = 0; col < (width - 40) / colWidth && col < 5; col++)
                {
                    int colX = x + 40 + col * colWidth;
                    if (rand.Next(100) < 60)
                    {
                        string data = col == 0 ? fakeData[rand.Next(fakeData.Length)] :
                                       col == 1 ? $"{rand.Next(100)}%" :
                                       col == 2 ? $"¥{rand.Next(10000)}" :
                                       fakeFormulas[rand.Next(fakeFormulas.Length)];
                        g.DrawString(data, cellFont, textBrush, colX + 5, rowY + 3);
                    }
                    g.DrawRectangle(gridPen, colX, rowY, colWidth - 1, rowHeight - 1);
                }
                g.DrawLine(gridPen, x + 5, rowY + rowHeight - 1, x + width - 5, rowY + rowHeight - 1);
            }

            int selCol = 1;
            int selRow = 2;
            int selX = x + 40 + selCol * colWidth;
            int selY = startY + selRow * rowHeight;
            using (var selPen = new Pen(Color.FromArgb(0, 112, 192), 2))
            {
                g.DrawRectangle(selPen, selX, selY, colWidth - 2, rowHeight - 2);
            }

            using (var formulaBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
            using (var formulaFont = new Font("Consolas", 10))
            {
                g.FillRectangle(formulaBrush, x, y + height - 25, width, 25);
                g.DrawString("fx  =SUM(B3:D7)", formulaFont, textBrush, x + 10, y + height - 22);
            }

            using (var borderPen = new Pen(Color.FromArgb(180, 180, 180), 2))
            {
                g.DrawRectangle(borderPen, x, y, width, height);
            }
        }
    }

    /// <summary>
    /// 绘制VS Code风格代码编辑器
    /// </summary>
    private static void DrawCodeEditorTheme(Graphics g, int screenWidth, int screenHeight)
    {
        int width = Math.Min(450, screenWidth / 3);
        int height = Math.Min(320, screenHeight / 3);
        int x = screenWidth - width - 20;
        int y = screenHeight - height - 40;

        using (var bgBrush = new SolidBrush(Color.FromArgb(30, 30, 30)))
        using (var sidebarBrush = new SolidBrush(Color.FromArgb(37, 37, 38)))
        using (var headerBrush = new SolidBrush(Color.FromArgb(45, 45, 45)))
        using (var textBrush = new SolidBrush(Color.FromArgb(212, 212, 212)))
        using (var keywordBrush = new SolidBrush(Color.FromArgb(86, 156, 214)))
        using (var stringBrush = new SolidBrush(Color.FromArgb(206, 145, 120)))
        using (var commentBrush = new SolidBrush(Color.FromArgb(106, 153, 85)))
        using (var funcBrush = new SolidBrush(Color.FromArgb(220, 220, 170)))
        using (var headerFont = new Font("Microsoft YaHei", 10, FontStyle.Regular))
        using (var codeFont = new Font("Consolas", 10))
        {
            g.FillRectangle(bgBrush, x, y, width, height);
            g.FillRectangle(sidebarBrush, x, y, 40, height);
            g.FillRectangle(headerBrush, x, y, width, 30);
            g.DrawString("Program.cs - CockroachPet", headerFont, textBrush, x + 50, y + 6);

            // 文件图标
            g.FillRectangle(new SolidBrush(Color.FromArgb(208, 174, 120)), x + 12, y + 45, 16, 20);

            // 代码行
            var codeLines = new[] {
                ("// 机器人核心控制系统", commentBrush),
                ("public class Robot", keywordBrush),
                ("{", textBrush),
                ("    private void Update()", funcBrush),
                ("    {", textBrush),
                ("        // 处理移动逻辑", commentBrush),
                ("        X += Dx * speed;", textBrush),
                ("        if (isBusy) return;", keywordBrush),
                ("        ProcessAI();", textBrush),
                ("    }", textBrush),
                ("}", textBrush)
            };

            int lineHeight = 18;
            int startY = y + 50;
            for (int i = 0; i < codeLines.Length && i < 12; i++)
            {
                g.DrawString((i + 1).ToString(), codeFont, Brushes.Gray, x + 50, startY + i * lineHeight);
                g.DrawString(codeLines[i].Item1, codeFont, codeLines[i].Item2, x + 75, startY + i * lineHeight);
            }

            // 光标
            int cursorY = startY + 5 * lineHeight;
            using (var cursorBrush = new SolidBrush(Color.FromArgb(200, 200, 200)))
            {
                g.FillRectangle(cursorBrush, x + 75 + 120, cursorY, 8, 14);
            }

            using (var borderPen = new Pen(Color.FromArgb(80, 80, 80), 2))
            {
                g.DrawRectangle(borderPen, x, y, width, height);
            }
        }
    }

    /// <summary>
    /// 绘制终端命令行风格
    /// </summary>
    private static void DrawTerminalTheme(Graphics g, int screenWidth, int screenHeight)
    {
        int width = Math.Min(400, screenWidth / 3);
        int height = Math.Min(280, screenHeight / 3);
        int x = screenWidth - width - 20;
        int y = screenHeight - height - 40;

        using (var bgBrush = new SolidBrush(Color.FromArgb(12, 12, 12)))
        using (var headerBrush = new SolidBrush(Color.FromArgb(50, 50, 50)))
        using (var greenBrush = new SolidBrush(Color.FromArgb(0, 255, 0)))
        using (var whiteBrush = new SolidBrush(Color.FromArgb(220, 220, 220)))
        using (var yellowBrush = new SolidBrush(Color.FromArgb(255, 255, 0)))
        using (var cyanBrush = new SolidBrush(Color.FromArgb(0, 255, 255)))
        using (var font = new Font("Consolas", 10))
        {
            g.FillRectangle(bgBrush, x, y, width, height);
            g.FillRectangle(headerBrush, x, y, width, 25);
            g.DrawString("CMD - C:\\Projects\\CockroachPet", new Font("Microsoft YaHei", 9), whiteBrush, x + 10, y + 4);

            string[][] lines = {
                new[] { "C:\\Projects\\CockroachPet> ", "green", "dotnet build" },
                new[] { "", "white", "  正在确定要还原的项目..." },
                new[] { "", "green", "  成功生成 1 个项目" },
                new[] { "", "white", "" },
                new[] { "C:\\Projects\\CockroachPet> ", "green", "git status" },
                new[] { "", "yellow", "  修改: Models/Robot.cs" },
                new[] { "", "yellow", "  修改: UI/PetForm.cs" },
                new[] { "", "white", "" },
                new[] { "C:\\Projects\\CockroachPet> ", "green", "_" }
            };

            int lineHeight = 16;
            int startY = y + 35;
            for (int i = 0; i < lines.Length; i++)
            {
                Brush brush = lines[i][1] switch {
                    "green" => greenBrush,
                    "yellow" => yellowBrush,
                    "cyan" => cyanBrush,
                    _ => whiteBrush
                };

                float offset = 0;
                if (lines[i][0].Length > 0)
                {
                    g.DrawString(lines[i][0], font, greenBrush, x + 10, startY + i * lineHeight);
                    offset = g.MeasureString(lines[i][0], font).Width;
                }

                if (lines[i][2].Length > 0)
                {
                    g.DrawString(lines[i][2], font, brush, x + 10 + offset, startY + i * lineHeight);
                }

                // 闪烁光标
                if (i == lines.Length - 1)
                {
                    if (DateTime.Now.Millisecond % 1000 < 500)
                    {
                        g.FillRectangle(greenBrush, x + 10 + offset + 8, startY + i * lineHeight + 2, 8, 12);
                    }
                }
            }

            using (var borderPen = new Pen(Color.FromArgb(100, 100, 100), 2))
            {
                g.DrawRectangle(borderPen, x, y, width, height);
            }
        }
    }

    /// <summary>
    /// 绘制Word文档风格
    /// </summary>
    private static void DrawWordTheme(Graphics g, int screenWidth, int screenHeight)
    {
        int width = Math.Min(420, screenWidth / 3);
        int height = Math.Min(340, screenHeight / 3);
        int x = screenWidth - width - 20;
        int y = screenHeight - height - 40;

        using (var paperBrush = new SolidBrush(Color.White))
        using (var headerBrush = new SolidBrush(Color.FromArgb(43, 87, 154)))
        using (var titleBrush = new SolidBrush(Color.FromArgb(33, 33, 33)))
        using (var textBrush = new SolidBrush(Color.FromArgb(66, 66, 66)))
        using (var headerFont = new Font("Microsoft YaHei", 11, FontStyle.Regular))
        using (var titleFont = new Font("Microsoft YaHei", 14, FontStyle.Bold))
        using (var bodyFont = new Font("Microsoft YaHei", 10))
        {
            // 纸张背景
            g.FillRectangle(paperBrush, x, y, width, height);
            g.FillRectangle(headerBrush, x, y, width, 28);
            g.DrawString("文档1 - Word", headerFont, Brushes.White, x + 10, y + 5);

            // 模拟纸张阴影
            using (var shadowPen = new Pen(Color.FromArgb(200, 200, 200), 1))
            {
                g.DrawLine(shadowPen, x + width - 1, y + 28, x + width - 1, y + height - 1);
                g.DrawLine(shadowPen, x, y + height - 1, x + width - 1, y + height - 1);
            }

            int margin = 25;
            int contentX = x + margin;
            int contentY = y + 45;

            // 标题
            g.DrawString("项目进度报告", titleFont, titleBrush, contentX, contentY);
            contentY += 35;

            // 正文段落
            string[] paragraphs = {
                "一、本周工作总结",
                "1. 完成了核心功能模块的开发与测试工作",
                "2. 修复了 12 个已知问题，优化了性能",
                "3. 编写了相关技术文档",
                "",
                "二、下周工作计划",
                "1. 继续推进 UI 界面优化",
                "2. 完成系统集成测试",
                "3. 准备版本发布事宜"
            };

            foreach (var para in paragraphs)
            {
                if (string.IsNullOrEmpty(para))
                {
                    contentY += 10;
                    continue;
                }

                bool isHeader = para.StartsWith("一、") || para.StartsWith("二、");
                var font = isHeader ? new Font("Microsoft YaHei", 11, FontStyle.Bold) : bodyFont;
                var brush = isHeader ? titleBrush : textBrush;

                g.DrawString(para, font, brush, contentX, contentY);
                contentY += isHeader ? 22 : 18;
            }

            // 页码
            g.DrawString("- 1 -", bodyFont, textBrush, x + width / 2 - 15, y + height - 25);

            using (var borderPen = new Pen(Color.FromArgb(180, 180, 180), 2))
            {
                g.DrawRectangle(borderPen, x, y, width, height);
            }
        }
    }

    [System.Obsolete("Use DrawBossModeIndicator with theme parameter")]
    public static void DrawBossModeIndicator(Graphics g, int screenWidth, int screenHeight)
    {
        DrawBossModeIndicator(g, screenWidth, screenHeight, BossModeTheme.Excel);
    }
}
