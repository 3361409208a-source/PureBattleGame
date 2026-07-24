using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PureBattleGame.Games.CockroachPet.Services
{
    public class PromoRecorder
    {
        private static PromoRecorder? _instance;
        public static PromoRecorder Instance => _instance ??= new PromoRecorder();

        public bool IsRecording { get; private set; }
        public string RecordMode { get; private set; } = "CUSTOM_BG"; // "DESKTOP" or "CUSTOM_BG"
        public Color BgColor { get; private set; } = Color.FromArgb(0, 255, 0); // Default Green Screen
        public string CurrentFolder { get; private set; } = "";
        public int FrameCount { get; private set; }
        public DateTime StartTime { get; private set; }

        private System.Windows.Forms.Timer? _recordTimer;
        private readonly object _lockObj = new object();

        private PromoRecorder() { }

        public bool StartRecording(string mode, string hexColor)
        {
            lock (_lockObj)
            {
                if (IsRecording) return false;

                RecordMode = mode == "DESKTOP" ? "DESKTOP" : "CUSTOM_BG";
                try
                {
                    if (!string.IsNullOrWhiteSpace(hexColor))
                    {
                        BgColor = ColorTranslator.FromHtml(hexColor);
                    }
                }
                catch
                {
                    BgColor = Color.FromArgb(0, 255, 0); // Green Screen
                }

                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recordings");
                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

                CurrentFolder = Path.Combine(baseDir, $"Record_{DateTime.Now:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(CurrentFolder);

                FrameCount = 0;
                StartTime = DateTime.Now;
                IsRecording = true;

                if (PetForm.Instance != null && !PetForm.Instance.IsDisposed)
                {
                    PetForm.Instance.BeginInvoke(new Action(() =>
                    {
                        PetForm.Instance.SetRecordingState(true, RecordMode == "CUSTOM_BG", BgColor);
                    }));
                }

                _recordTimer = new System.Windows.Forms.Timer
                {
                    Interval = 33 // ~30 FPS
                };
                _recordTimer.Tick += CaptureFrame;
                _recordTimer.Start();

                return true;
            }
        }

        private void CaptureFrame(object? sender, EventArgs e)
        {
            if (!IsRecording || PetForm.Instance == null || PetForm.Instance.IsDisposed) return;

            try
            {
                int width = Screen.PrimaryScreen?.Bounds.Width ?? 1920;
                int height = Screen.PrimaryScreen?.Bounds.Height ?? 1080;

                using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    if (RecordMode == "DESKTOP")
                    {
                        g.CopyFromScreen(0, 0, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                    }
                    else
                    {
                        g.Clear(BgColor);
                        PetForm.Instance.RenderToBitmap(g);
                    }
                }

                FrameCount++;
                string fileName = Path.Combine(CurrentFolder, $"frame_{FrameCount:D5}.png");
                bmp.Save(fileName, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PromoRecorder] Frame capture error: {ex.Message}");
            }
        }

        public object StopRecording()
        {
            lock (_lockObj)
            {
                if (!IsRecording) return new { success = false, message = "未在录制中" };

                IsRecording = false;
                if (_recordTimer != null)
                {
                    _recordTimer.Stop();
                    _recordTimer.Dispose();
                    _recordTimer = null;
                }

                if (PetForm.Instance != null && !PetForm.Instance.IsDisposed)
                {
                    PetForm.Instance.BeginInvoke(new Action(() =>
                    {
                        PetForm.Instance.SetRecordingState(false, false, Color.Transparent);
                    }));
                }

                double durationSec = (DateTime.Now - StartTime).TotalSeconds;
                string folder = CurrentFolder;

                // 打开输出目录
                try
                {
                    if (Directory.Exists(folder))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", folder);
                    }
                }
                catch { }

                return new
                {
                    success = true,
                    folderPath = folder,
                    frameCount = FrameCount,
                    durationSeconds = Math.Round(durationSec, 1)
                };
            }
        }

        public object GetStatus()
        {
            return new
            {
                isRecording = IsRecording,
                mode = RecordMode,
                frameCount = FrameCount,
                durationSeconds = IsRecording ? Math.Round((DateTime.Now - StartTime).TotalSeconds, 1) : 0,
                folderPath = CurrentFolder
            };
        }
    }
}
