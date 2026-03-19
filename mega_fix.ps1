$path = "c:\Users\18229\.openclaw\workspace\PureBattleGame\Games\StarCoreDefense\BattleForm.cs"
$content = [System.IO.File]::ReadAllText($path)

# 1. UpdateFusion Call
$fusionCall = "            }`r`n`r`n            // Robot Fusion Logic`r`n            UpdateFusion();`r`n        }`r`n`r`n        // Monsters update"
$robotLoopEnd = '                if (robot.MonsterTarget != null && robot.MonsterTarget.IsActive)`r`n                    robot.MonsterTarget.AttackerCount++;`r`n            }`r`n        }'
$content = $content.Replace($robotLoopEnd, $fusionCall)

# 2. DrawMegaAppearance Dispatch
$drawRobotOld = '            switch (robot.ClassType)`r`n            {`r`n                case RobotClass.Worker: DrawWorkerAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                case RobotClass.Shooter: DrawShooterAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                case RobotClass.Healer: DrawHealerAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                case RobotClass.Guardian: DrawGuardianAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                default: DrawDefaultAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n            }'

$drawRobotNew = '            if (robot.Rank == RobotRank.Mega)`r`n            {`r`n                DrawMegaAppearance(g, robot, x, y, size, centerX, centerY);`r`n            }`r`n            else`r`n            {`r`n                switch (robot.ClassType)`r`n                {`r`n                    case RobotClass.Worker: DrawWorkerAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                    case RobotClass.Shooter: DrawShooterAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                    case RobotClass.Healer: DrawHealerAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                    case RobotClass.Guardian: DrawGuardianAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                    default: DrawDefaultAppearance(g, robot, x, y, size, centerX, centerY); break;`r`n                }`r`n            }'
$content = $content.Replace($drawRobotOld, $drawRobotNew)

# 3. Append Methods
$methods = @"
    private void UpdateFusion()
    {
        if (Environment.TickCount % 60 != 0) return;
        foreach (RobotClass ct in Enum.GetValues<RobotClass>()) {
            if (ct == RobotClass.Base) continue;
            var normals = _robots.Where(r => r.IsActive && !r.IsDead && r.ClassType == ct && r.Rank == RobotRank.Normal).ToList();
            if (normals.Count >= 5) {
                var group = normals.Take(5).ToList();
                float ax = group.Average(r => r.X), ay = group.Average(r => r.Y);
                foreach (var r in group) { r.HP = 0; r.IsDead = true; r.IsActive = false; }
                var mega = new Robot(++_robotNextId, "MEGA " + ct, ax, ay, ct, RobotRank.Mega);
                mega.ApplyClassProperties(); _robots.Add(mega);
                AddExplosion(ax + 10, ay + 10, mega.PrimaryColor, 30, "RING");
                AddExplosion(ax + 10, ay + 10, Color.White, 20, "FLASH");
                AddFloatingText(ax, ay - 30, "UNITS FUSED!", Color.Gold);
                AudioManager.PlaySound("POWERUP");
            }
        }
    }

    private void DrawMegaAppearance(Graphics g, Robot robot, float x, float y, float size, float cx, float cy)
    {
        using var pb = new SolidBrush(robot.PrimaryColor); using var sb = new SolidBrush(robot.SecondaryColor);
        switch (robot.ClassType) {
            case RobotClass.Worker:
                g.FillEllipse(sb, x - 5, y - 5, size + 10, size + 10); g.FillEllipse(pb, x, y, size, size);
                for (int i = 0; i < 3; i++) {
                    float r = (Environment.TickCount / 100f) + (float)(i * Math.PI * 2 / 3);
                    float dx = cx + (float)Math.Cos(r) * (size * 0.9f), dy = cy + (float)Math.Sin(r) * (size * 0.9f);
                    using var p = new Pen(Color.Gold, 4); g.DrawLine(p, cx, cy, dx, dy);
                    g.FillEllipse(Brushes.Yellow, dx - 5, dy - 5, 10, 10);
                }
                break;
            case RobotClass.Shooter:
                PointF[] pts = { new PointF(cx, y - 10), new PointF(x + size + 10, cy), new PointF(cx, y + size + 10), new PointF(x - 10, cy) };
                g.FillPolygon(pb, pts); g.DrawPolygon(Pens.White, pts);
                float ang = (float)Math.Atan2(robot.Dy, robot.Dx);
                if (robot.MonsterTarget != null) ang = (float)Math.Atan2(robot.MonsterTarget.Y - cy, robot.MonsterTarget.X - cx);
                for (int i = 0; i < 4; i++) {
                    float ba = ang + (i - 1.5f) * 0.25f, bx = cx + (float)Math.Cos(ba) * (size * 0.9f), by = cy + (float)Math.Sin(ba) * (size * 0.7f);
                    using var bp = new Pen(Color.Silver, 5); g.DrawLine(bp, cx, cy, bx, by);
                    g.FillEllipse(Brushes.Red, bx - 3, by - 3, 6, 6);
                }
                break;
            case RobotClass.Healer:
                g.FillEllipse(pb, x, y, size, size); g.FillEllipse(sb, x + 5, y + 5, size - 10, size - 10);
                using (var lb = new SolidBrush(Color.FromArgb(150, Color.LimeGreen))) g.FillEllipse(lb, x - 10, y - 10, size + 20, size + 20);
                using (var wb = new SolidBrush(Color.White)) { g.FillRectangle(wb, cx - 4, cy - 12, 8, 24); g.FillRectangle(wb, cx - 12, cy - 4, 24, 8); }
                for (int i = 0; i < 4; i++) {
                    float rot = (Environment.TickCount / 400f) + (float)(i * Math.PI / 2);
                    float sx = cx + (float)Math.Cos(rot) * (size * 1.1f), sy = cy + (float)Math.Sin(rot) * (size * 1.1f);
                    g.FillEllipse(Brushes.Cyan, sx - 4, sy - 4, 8, 8);
                }
                break;
            case RobotClass.Guardian:
                PointF[] hex = new PointF[6]; for (int i = 0; i < 6; i++) {
                    float a = i * (float)Math.PI / 3; hex[i] = new PointF(cx + (float)Math.Cos(a) * (size * 0.7f), cy + (float)Math.Sin(a) * (size * 0.7f));
                }
                g.FillPolygon(sb, hex); using (var sp = new Pen(Color.Gold, 3)) g.DrawPolygon(sp, hex);
                float gang = (float)Math.Atan2(robot.Dy, robot.Dx);
                for (int j = -1; j <= 1; j++) {
                    float sa = (float)(gang * 180 / Math.PI - 60 + j * 30);
                    using var shp = new Pen(Color.Cyan, 6);
                    g.DrawArc(shp, x - 15, y - 15, size + 30, size + 30, sa, 40);
                }
                break;
        }
        DrawEyes(g, robot, cx, cy, 6);
    }
}
"@

$lastBraceIndex = $content.LastIndexOf('}')
$newContent = $content.Substring(0, $lastBraceIndex) + $methods + "`r`n}"
[System.IO.File]::WriteAllText($path, $newContent)
Write-Host "File fixed successfully."
