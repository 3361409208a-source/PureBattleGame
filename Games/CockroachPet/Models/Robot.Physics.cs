using System;
using System.Linq;
using System.Windows.Forms;

namespace PureBattleGame.Games.CockroachPet;

public partial class Robot
{
    // 物理互动 (捂拉、拉取、抛投)
    public Robot? PhysicalTarget { get; set; }
    public string PhysicalAction { get; set; } = "NONE";
    public int PhysicalTimer { get; set; } = 0;
    public Robot? PullingMe { get; set; }
    public bool IsBeingThrown { get; set; } = false;

    private void UpdatePhysicsConstraints()
    {
        if (PullingMe != null)
        {
            if (!PullingMe.IsActive || PullingMe.PhysicalTarget != this)
            {
                PullingMe = null;
            }
            else
            {
                float tx = PullingMe.X + PullingMe.Size / 2 - Size / 2;
                float ty = PullingMe.Y + PullingMe.Size / 2 - Size / 2;
                X = X * 0.7f + tx * 0.3f;
                Y = Y * 0.7f + ty * 0.3f;
            }
        }

        if (IsBeingThrown)
        {
            RotationAngle += 15;
            if (X <= 0 || Y <= 0 || X >= Screen.PrimaryScreen.Bounds.Width - Size || Y >= Screen.PrimaryScreen.Bounds.Height - Size)
            {
                IsBeingThrown = false;
                RotationAngle = 0;
            }

            Dx *= 0.98f;
            Dy *= 0.98f;
            if (Math.Abs(Dx) < 1 && Math.Abs(Dy) < 1)
            {
                IsBeingThrown = false;
                RotationAngle = 0;
            }
        }

        if (PhysicalTimer > 0) PhysicalTimer--;
        if (StunTimer > 0) StunTimer--;
        if (SlowTimer > 0) SlowTimer--;
        if (BlindTimer > 0) BlindTimer--;

        if (PhysicalTimer == 0 && PhysicalAction != "NONE")
        {
            if (PhysicalAction == "GRAB") PerformThrow();
            PhysicalAction = "NONE";
            PhysicalTarget = null;
        }

        if (BlindTimer == 1) StatusMessage = "IDLE";
    }
    public void PerformRandomMeleeAction(Robot other)
    {
        if (other == null || !other.IsActive || other.IsDead || IsDead) return;

        var allMelee = new[] { "PUSH", "PULL", "GRAB", "THROW", "DUEL", "SLAM", "KICK", "SUPLEX", "HEADBUTT", "TORNADO" };
        // 优先使用角色专属技能池，否则回退到全局设置
        string[] allowedMelee;
        if (PersonalWeapons.Count > 0)
        {
            allowedMelee = allMelee.Where(w => PersonalWeapons.Contains(w)).ToArray();
            if (allowedMelee.Length == 0) allowedMelee = allMelee.Where(w => Core.SettingsManager.Current.EnabledWeapons.Contains(w)).ToArray();
        }
        else
        {
            allowedMelee = allMelee.Where(w => Core.SettingsManager.Current.EnabledWeapons.Contains(w)).ToArray();
        }
        if (allowedMelee.Length == 0) allowedMelee = new[] { "PUSH" };

        string selected = allowedMelee[Rand.Next(allowedMelee.Length)];

        switch (selected)
        {
            case "PUSH": PerformPush(other); break;
            case "PULL": PerformPull(other); break;
            case "GRAB": PerformGrab(other); break;
            case "THROW": PerformGrab(other); break; // THROW is triggered after GRAB
            case "DUEL": StartDuel(other); break;
            case "SLAM": PerformSlam(other); break;
            case "KICK": PerformKick(other); break;
            case "SUPLEX": PerformSuplex(other); break;
            case "HEADBUTT": PerformHeadbutt(other); break;
            case "TORNADO": PerformTornado(other); break;
            default: PerformPush(other); break;
        }
    }

    private void PerformSlam(Robot other)
    {
        SetBark("泰山压顶！", 60);
        PhysicalTarget = other;
        PhysicalAction = "SLAM";
        PhysicalTimer = 40;
        
        other.SpecialState = "SHAKING";
        other.SpecialStateTimer = 40;
        other.StunTimer = 40;
        other.ApplyAttackEffect(20, Name);
    }

    private void PerformKick(Robot other)
    {
        SetBark("吃我一记飞踢！", 40);
        PhysicalTarget = other;
        PhysicalAction = "KICK";
        PhysicalTimer = 20;

        float ox = other.X - X;
        float oy = other.Y - Y;
        float odist = (float)Math.Max(1, Math.Sqrt(ox * ox + oy * oy));
        other.Dx = (ox / odist) * 25 * other.SpeedMultiplier;
        other.Dy = (oy / odist) * 25 * other.SpeedMultiplier;
        other.ApplyAttackEffect(15, Name);
    }

    private void PerformSuplex(Robot other)
    {
        SetBark("过肩摔！", 60);
        PhysicalTarget = other;
        PhysicalAction = "SUPLEX";
        PhysicalTimer = 60;
        other.PullingMe = this;
        other.IsBeingThrown = true;
        other.ApplyAttackEffect(30, Name);
    }

    private void PerformHeadbutt(Robot other)
    {
        SetBark("铁头功！", 30);
        PhysicalTarget = other;
        PhysicalAction = "HEADBUTT";
        PhysicalTimer = 20;

        float ox = other.X - X;
        float oy = other.Y - Y;
        float odist = (float)Math.Max(1, Math.Sqrt(ox * ox + oy * oy));
        Dx = (ox / odist) * 10;
        Dy = (oy / odist) * 10;
        
        other.StunTimer = 60;
        other.SpecialState = "SHAKING";
        other.SpecialStateTimer = 60;
        other.ApplyAttackEffect(10, Name);
    }

    private void PerformTornado(Robot other)
    {
        SetBark("无敌旋风斩！", 60);
        PhysicalTarget = other;
        PhysicalAction = "TORNADO";
        PhysicalTimer = 60;

        var closeRobots = PetForm.Instance.GetRobots().Where(r => r != this && r.IsActive && !r.IsDead &&
            Math.Abs(r.X - X) < 150 && Math.Abs(r.Y - Y) < 150).ToList();

        foreach (var target in closeRobots)
        {
            float ox = target.X - X;
            float oy = target.Y - Y;
            float odist = (float)Math.Max(1, Math.Sqrt(ox * ox + oy * oy));
            target.Dx = (ox / odist) * 20 * target.SpeedMultiplier;
            target.Dy = (oy / odist) * 20 * target.SpeedMultiplier;
            target.ApplyAttackEffect(10, Name);
        }
    }

    private void PerformPush(Robot other)
    {
        SetBark("给我起开！", 60);
        PhysicalTarget = other;
        PhysicalAction = "PUSH";
        PhysicalTimer = 60;

        float ox = other.X - X;
        float oy = other.Y - Y;
        float odist = (float)Math.Max(1, Math.Sqrt(ox * ox + oy * oy));
        other.Dx = (ox / odist) * 12 * other.SpeedMultiplier;
        other.Dy = (oy / odist) * 12 * other.SpeedMultiplier;
        other.SpecialState = "SHAKING";
        other.SpecialStateTimer = 30;
    }

    private void PerformPull(Robot other)
    {
        SetBark("过来吧你！", 60);
        PhysicalTarget = other;
        PhysicalAction = "PULL";
        PhysicalTimer = 60;
        other.PullingMe = this;
    }

    private void PerformGrab(Robot other)
    {
        SetBark("抓到你了！", 40);
        PhysicalTarget = other;
        PhysicalAction = "GRAB";
        PhysicalTimer = 40;
        other.PullingMe = this;
    }

    private void PerformThrow()
    {
        if (PhysicalTarget == null || !PhysicalTarget.IsActive) return;

        SetBark("走你！", 60);

        Robot? thirdObj = PetForm.Instance.GetRobots()
            .Where(r => r != this && r != PhysicalTarget && r.IsVisible)
            .OrderBy(r => Math.Abs(r.X - X) + Math.Abs(r.Y - Y))
            .FirstOrDefault();

        float tx, ty;
        if (thirdObj != null)
        {
            tx = thirdObj.X - X;
            ty = thirdObj.Y - Y;
        }
        else
        {
            tx = Rand.Next(-200, 200);
            ty = Rand.Next(-200, 200);
        }

        float dist = (float)Math.Max(1, Math.Sqrt(tx * tx + ty * ty));
        PhysicalTarget.Dx = (tx / dist) * 25 * PhysicalTarget.SpeedMultiplier;
        PhysicalTarget.Dy = (ty / dist) * 25 * PhysicalTarget.SpeedMultiplier;
        PhysicalTarget.IsBeingThrown = true;
        PhysicalTarget.PullingMe = null;

        TerminalManagerForm.Instance.BroadcastToWorld(Name, $"🤼 将 {PhysicalTarget.Name} 像垃圾一样扔了出去！", System.Drawing.Color.Orange);
    }
}
