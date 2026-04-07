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
