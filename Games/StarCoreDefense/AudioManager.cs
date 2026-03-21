using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Linq;

namespace PureBattleGame.Games.StarCoreDefense;

/// <summary>
/// 深度优化版音效管理器 - 基于 MCI 预热池
/// 解决战场混乱时频繁 Open/Close 导致的系统卡顿。
/// </summary>
public static class AudioManager
{
    [DllImport("winmm.dll")]
    private static extern long mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);

    public static bool IsMutedSFX { get; set; } = false;
    public static bool IsMutedBGM { get; set; } = false;
    private static int _currentBGMTrack = -1; // -1: none, 1: battle, 2: peace

    private static readonly Dictionary<string, string> _soundPaths = new Dictionary<string, string>();
    private static readonly Dictionary<string, int> _channelIndices = new Dictionary<string, int>();
    private static readonly Dictionary<string, long> _lastPlayTime = new Dictionary<string, long>();
    
    // 不同音效的独立通道数（并发池大小）
    private static readonly Dictionary<string, int> _channelCounts = new Dictionary<string, int>
    {
        { "shoot", 12 }, { "hit", 8 }, { "mine", 4 }, { "build", 4 },
        { "death", 6 }, { "overload", 4 }, { "leap", 4 }, { "whirlwind", 4 },
        { "level_up", 2 }, { "purchase", 2 }
    };

    private static string _baseAssetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Games", "StarCoreDefense", "Assets", "Sfx");

    public static void Initialize()
    {
        if (!Directory.Exists(_baseAssetPath)) Directory.CreateDirectory(_baseAssetPath);

        // 1. 自动生成缺失的基础音效 (Procedural Generation)
        EnsureProceduralSfx("shoot", 0.08, (t) => Math.Sin(2 * Math.PI * (1200 - t * 6000) * t) * (1-t)); 
        EnsureProceduralSfx("hit", 0.04, (t) => (new Random().NextDouble() - 0.5) * 1.5); 
        EnsureProceduralSfx("mine", 0.08, (t) => Math.Sin(2 * Math.PI * 440 * t) * Math.Sign(Math.Sin(2 * Math.PI * 20 * t))); 
        EnsureProceduralSfx("build", 0.1, (t) => Math.Sin(2 * Math.PI * (300 + t * 500) * t)); 
        EnsureProceduralSfx("death", 0.5, (t) => Math.Sin(2 * Math.PI * (150 - t * 300) * t) * Math.Pow(1 - t, 2)); 
        EnsureProceduralSfx("overload", 0.8, (t) => Math.Sin(2 * Math.PI * (40 + Math.Sin(t * 15) * 15) * t)); 
        EnsureProceduralSfx("leap", 0.2, (t) => Math.Sin(2 * Math.PI * (300 + t * 1000) * t) * (1 - t)); 
        EnsureProceduralSfx("whirlwind", 0.3, (t) => (new Random().NextDouble() - 0.5) * Math.Sin(2 * Math.PI * 30 * t)); 
        EnsureProceduralSfx("level_up", 0.4, (t) => Math.Sin(2 * Math.PI * (t < 0.2 ? 523 : 659) * t)); 
        EnsureProceduralSfx("purchase", 0.2, (t) => Math.Sin(2 * Math.PI * 880 * t) * (1 - t)); 

        // 2. 预热所有音效到 MCI 通道池
        foreach (var file in Directory.GetFiles(_baseAssetPath, "*.wav"))
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(file).ToLower();
                _soundPaths[name] = file;
                _channelIndices[name] = 0;
                _lastPlayTime[name] = 0;

                int count = _channelCounts.ContainsKey(name) ? _channelCounts[name] : 4;
                for (int i = 0; i < count; i++)
                {
                    string alias = $"{name}_{i}";
                    mciSendString($"close {alias}", null, 0, IntPtr.Zero);
                    mciSendString($"open \"{file}\" alias {alias}", null, 0, IntPtr.Zero);
                }
            }
            catch { }
        }
    }

    private static void EnsureProceduralSfx(string name, double duration, Func<double, double> waveFunc)
    {
        string path = Path.Combine(_baseAssetPath, $"{name}.wav");
        if (File.Exists(path)) return;
        try { GenerateWav(path, duration, waveFunc); } catch { }
    }

    public static void PlayShootSound() => PlaySound("shoot", 15);
    public static void PlayHitSound() => PlaySound("hit", 10);
    public static void PlayDeathSound() => PlaySound("death", 50);
    public static void PlayProjectileSound(string type) => PlaySound(type.ToLower(), 20);

    public static void PlaySound(string effect, int cooldownMs = 20)
    {
        if (IsMutedSFX) return; // 音效禁用

        string name = effect.ToLower();
        if (!_soundPaths.ContainsKey(name)) return;

        // 3. 频率限制 (Cooldown) 防止爆发性操作导致卡顿
        long now = Environment.TickCount64;
        if (now - _lastPlayTime[name] < cooldownMs) return;
        _lastPlayTime[name] = now;

        try
        {
            int count = _channelCounts.ContainsKey(name) ? _channelCounts[name] : 4;
            int idx = _channelIndices[name];
            string alias = $"{name}_{idx}";

            // 直接通过预开通道播放，无需 Open/Close
            mciSendString($"play {alias} from 0", null, 0, IntPtr.Zero);
            
            _channelIndices[name] = (idx + 1) % count;
        }
        catch { }
    }

    private static void GenerateWav(string path, double duration, Func<double, double> waveFunc)
    {
        int sampleRate = 22050;
        int totalSamples = (int)(sampleRate * duration);
        byte[] data = new byte[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            double sample = Math.Max(-1, Math.Min(1, waveFunc(t)));
            data[i] = (byte)(128 + sample * 127);
        }
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + data.Length);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); bw.Write((short)1); bw.Write((short)1);
            bw.Write(sampleRate); bw.Write(sampleRate); bw.Write((short)1); bw.Write((short)8);
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(data.Length); bw.Write(data);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern int GetShortPathName([MarshalAs(UnmanagedType.LPTStr)] string lpszLongPath, 
        [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszShortPath, int cchBuffer);

    private static bool _bgmInitialized = false;

    private static void InitializeBGM()
    {
        if (_bgmInitialized) return;
        _bgmInitialized = true;

        for (int i = 1; i <= 3; i++)
        {
            string fileName = $"{i}.mp3";
            string[] candidates = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Games", "StarCoreDefense", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Games", "StarCoreDefense", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(Environment.CurrentDirectory, "Games", "StarCoreDefense", fileName)
            };
            string finalPath = candidates.FirstOrDefault(File.Exists) ?? "";
            if (!string.IsNullOrEmpty(finalPath))
            {
                StringBuilder shortPath = new StringBuilder(255);
                GetShortPathName(finalPath, shortPath, shortPath.Capacity);
                mciSendString($"open \"{shortPath}\" type mpegvideo alias bgm{i}", null, 0, IntPtr.Zero);
            }
        }
    }

    public static void PlayBGM(int track) // 1: battle, 2: peace, 3: battle2
    {
        if (_currentBGMTrack == track) 
        {
            // 如果已经在播这一轨了，确保它是 Playing 状态（防止自然播放结束停掉）
            if (!IsMutedBGM) mciSendString($"play bgm{track} repeat", null, 0, IntPtr.Zero);
            return;
        }
        
        if (!_bgmInitialized) InitializeBGM();

        // 【断点续播】暂停旧轨道，而不是停止
        if (_currentBGMTrack != -1)
        {
            mciSendString($"pause bgm{_currentBGMTrack}", null, 0, IntPtr.Zero);
        }

        _currentBGMTrack = track;
        if (IsMutedBGM) return;

        // 【断点续播】恢复播放目标轨道
        // resume 命令会让它从之前暂停的地方继续
        mciSendString($"resume bgm{track}", null, 0, IntPtr.Zero);
        // 万一没播过，或者已经到底了(自然停止)，补一个从当前位置开始的 play
        mciSendString($"play bgm{track} repeat", null, 0, IntPtr.Zero);
    }

    public static string GetTrackStatus(int track)
    {
        StringBuilder sb = new StringBuilder(128);
        mciSendString($"status bgm{track} mode", sb, sb.Capacity, IntPtr.Zero);
        return sb.ToString().ToLower();
    }

    public static void UpdateBGMVolume()
    {
        int t = _currentBGMTrack;
        _currentBGMTrack = -1; 
        PlayBGM(t);
    }

    public static void StopAll() => mciSendString("close all", null, 0, IntPtr.Zero);
}
