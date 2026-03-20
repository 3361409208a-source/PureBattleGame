using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Linq;

namespace PureBattleGame.Games.StarCoreDefense;

/// <summary>
/// 高级音效管理器 - 基于 Win32 MCI (Media Control Interface)
/// 核心特性：支持多通道播放，且在素材缺失时自动【生成程序化音效】保证开箱即用。
/// </summary>
public static class AudioManager
{
    [DllImport("winmm.dll")]
    private static extern long mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);

    private static readonly Dictionary<string, string> _soundPaths = new Dictionary<string, string>();
    private static int _channelCounter = 0;
    private const int MAX_CHANNELS = 12; // 增加频道数处理射弹场景

    private static string _baseAssetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Games", "StarCoreDefense", "Assets", "Sfx");

    public static void Initialize()
    {
        if (!Directory.Exists(_baseAssetPath)) Directory.CreateDirectory(_baseAssetPath);

        // 1. 自动生成缺失的基础音效 (Procedural Generation)
        EnsureProceduralSfx("shoot", 0.1, (t) => Math.Sin(2 * Math.PI * (800 - t * 4000) * t)); // 激光扫频
        EnsureProceduralSfx("hit", 0.05, (t) => (new Random().NextDouble() - 0.5) * 2); // 噪波打击
        EnsureProceduralSfx("mine", 0.08, (t) => Math.Sin(2 * Math.PI * 440 * t) * Math.Sign(Math.Sin(2 * Math.PI * 15 * t))); // 金属撞击
        EnsureProceduralSfx("build", 0.15, (t) => Math.Sin(2 * Math.PI * (200 + t * 400) * t)); // 修理声
        EnsureProceduralSfx("death", 0.4, (t) => Math.Sin(2 * Math.PI * (100 - t * 200) * t) * (1 - t)); // 沉重坠落
        EnsureProceduralSfx("overload", 0.6, (t) => Math.Sin(2 * Math.PI * (30 + Math.Sin(t * 10) * 10) * t)); // 低频震荡
        EnsureProceduralSfx("leap", 0.3, (t) => Math.Sin(2 * Math.PI * (200 + t * 800) * t) * Math.Pow(1 - t, 2)); // 喷火推进
        EnsureProceduralSfx("whirlwind", 0.4, (t) => (new Random().NextDouble() - 0.5) * Math.Sin(2 * Math.PI * 20 * t)); // 旋风切裂
        EnsureProceduralSfx("level_up", 0.5, (t) => Math.Sin(2 * Math.PI * (440 + (t < 0.25 ? 0 : 200)) * t)); // 升级提醒
        EnsureProceduralSfx("purchase", 0.2, (t) => Math.Sin(2 * Math.PI * 880 * t) * (1 - t)); // 购买反馈

        // 2. 注册预定义映射
        foreach (var file in Directory.GetFiles(_baseAssetPath, "*.wav"))
        {
            string name = Path.GetFileNameWithoutExtension(file).ToLower();
            _soundPaths[name] = file;
        }
    }

    private static void EnsureProceduralSfx(string name, double duration, Func<double, double> waveFunc)
    {
        string path = Path.Combine(_baseAssetPath, $"{name}.wav");
        if (File.Exists(path)) return; // 已有用户文件或已生成过

        try
        {
            GenerateWav(path, duration, waveFunc);
        }
        catch { /* 静默失败 */ }
    }

    public static void PlayShootSound() => PlaySound("shoot");
    public static void PlayLaserSound() => PlaySound("laser");
    public static void PlayProjectileSound(string type) => PlaySound(type.ToLower());
    public static void PlayHitSound() => PlaySound("hit");
    public static void PlayDeathSound() => PlaySound("death");

    public static void PlaySound(string effect)
    {
        string key = effect.ToLower();
        if (!_soundPaths.ContainsKey(key)) return;

        try
        {
            string path = _soundPaths[key];
            string alias = $"sfx_{key}_{_channelCounter}";
            
            mciSendString($"close {alias}", null, 0, IntPtr.Zero);
            if (mciSendString($"open \"{path}\" alias {alias}", null, 0, IntPtr.Zero) == 0)
            {
                mciSendString($"play {alias} from 0", null, 0, IntPtr.Zero);
                _channelCounter = (_channelCounter + 1) % MAX_CHANNELS;
            }
        }
        catch { }
    }

    /// <summary>
    /// 生成简单的 8-bit 44100Hz 单声道 WAV 文件
    /// </summary>
    private static void GenerateWav(string path, double duration, Func<double, double> waveFunc)
    {
        int sampleRate = 22050; // 降低采样率减小文件体积
        int totalSamples = (int)(sampleRate * duration);
        byte[] data = new byte[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            double sample = waveFunc(t);
            sample = Math.Max(-1, Math.Min(1, sample)); // Clamp
            data[i] = (byte)(128 + sample * 127); // Convert to 8-bit PCM (0-255, 128 is center)
        }

        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            // RIFF header
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + data.Length);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // Chunk size
            bw.Write((short)1); // Audio format (PCM)
            bw.Write((short)1); // Channels
            bw.Write(sampleRate);
            bw.Write(sampleRate * 1); // Byte rate
            bw.Write((short)1); // Block align
            bw.Write((short)8); // Bits per sample

            // data chunk
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(data.Length);
            bw.Write(data);
        }
    }

    public static void StopAll() => mciSendString("close all", null, 0, IntPtr.Zero);
}
