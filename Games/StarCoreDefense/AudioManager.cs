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
    
    // 全局音效音量 (0~1000)，默认值设为 500 (即降低 50%)
    public static int SfxVolume { get; set; } = 500;

    private static int _currentBGMTrack = -1; // -1: none, 1: battle, 2: peace, 3: battle 2
    private static readonly float[] _targetVolumes = { 0, 1000, 1000, 1000 }; // 0: ignore, 1..3: tracks
    private static readonly float[] _currentVolumes = { 0, 0, 0, 0 };
    private const float FADE_SPEED = 10f; // 每帧音量变化步长 (约1.5秒线性淡入淡出)

    private static readonly Dictionary<string, string> _soundPaths = new Dictionary<string, string>();
    private static readonly Dictionary<string, int> _channelIndices = new Dictionary<string, int>();
    private static readonly Dictionary<string, long> _lastPlayTime = new Dictionary<string, long>();
    
    // 不同音效的独立通道数（并发池大小，大幅缩减以降低系统句柄上限）
    private static readonly Dictionary<string, int> _channelCounts = new Dictionary<string, int>
    {
        { "shoot", 3 }, { "bullet", 3 }, { "rocket", 2 }, { "plasma", 2 }, 
        { "lightning", 2 }, { "meteor", 1 }, { "black_hole", 1 }, { "death_ray", 1 },
        { "hit", 3 }, { "mine", 2 }, { "build", 2 }, { "death", 2 }, 
        { "overload", 1 }, { "leap", 1 }, { "whirlwind", 1 },
        { "level_up", 1 }, { "purchase", 1 }
    };

    private static string _baseAssetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Games", "StarCoreDefense", "Assets", "Sfx");

    public static void Initialize()
    {
        if (!Directory.Exists(_baseAssetPath)) Directory.CreateDirectory(_baseAssetPath);

        // 1. 自动生成缺失的基础音效 (Procedural Generation)
        // 柔和的激光子弹 (加了指数衰减包络线，消除刺耳短音)
        EnsureProceduralSfx("shoot", 0.08, (t) => Math.Sin(2 * Math.PI * (800 - t * 3000) * t) * Math.Pow(1 - t, 2) * 0.6); 
        EnsureProceduralSfx("bullet", 0.08, (t) => Math.Sin(2 * Math.PI * (800 - t * 3000) * t) * Math.Pow(1 - t, 2) * 0.6); 
        
        // 沉闷的火箭发射 (低频扰动 + 尾音)
        EnsureProceduralSfx("rocket", 0.2, (t) => (new Random().NextDouble() - 0.5) * Math.Pow(1 - t, 1.5) * Math.Sin(2 * Math.PI * (100 - t * 50) * t) * 0.8);
        
        // 科幻等离子炮 (高频震音 vibrato)
        EnsureProceduralSfx("plasma", 0.15, (t) => Math.Sin(2 * Math.PI * (1500 + Math.Sin(t * 100) * 200) * t) * Math.Pow(1 - t, 1.5) * 0.5);
        
        // 劈啪闪电 (高频脉冲夹杂噪音)
        EnsureProceduralSfx("lightning", 0.1, (t) => (Math.Sin(2 * Math.PI * 3000 * t) > 0 ? 1 : -1) * (new Random().NextDouble() * 0.5) * Math.Pow(1 - t, 2));
        
        // 陨石坠落低频轰鸣
        EnsureProceduralSfx("meteor", 0.4, (t) => Math.Sin(2 * Math.PI * (60 - t * 30) * t) * Math.Pow(1 - t, 0.5) * 0.8 + (new Random().NextDouble() - 0.5) * 0.2);
        
        // 黑洞长嗡鸣 (超低频调制)
        EnsureProceduralSfx("black_hole", 0.6, (t) => Math.Sin(2 * Math.PI * 40 * t) * Math.Sin(2 * Math.PI * (10 + t * 5) * t) * 0.6);
        
        // 死亡射线切割 (方波合成)
        EnsureProceduralSfx("death_ray", 0.2, (t) => Math.Sin(2 * Math.PI * 800 * t) * Math.Sign(Math.Sin(2 * Math.PI * 200 * t)) * 0.4 * (1 - t));

        // 受击音效柔和化 (大幅降低白噪音刺耳程度)
        EnsureProceduralSfx("hit", 0.05, (t) => (new Random().NextDouble() - 0.5) * Math.Pow(1 - t, 3) * 0.7); 
        
        EnsureProceduralSfx("mine", 0.08, (t) => Math.Sin(2 * Math.PI * 440 * t) * Math.Sign(Math.Sin(2 * Math.PI * 20 * t))); 
        EnsureProceduralSfx("build", 0.1, (t) => Math.Sin(2 * Math.PI * (300 + t * 500) * t)); 
        EnsureProceduralSfx("death", 0.3, (t) => Math.Sin(2 * Math.PI * (150 - t * 300) * t) * Math.Pow(1 - t, 2) * 0.8); 
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

    // 弹幕游戏音频最佳实践：通过大幅提升同类音效的冷却时间（CD），
    // 将满屏的受击/发射音频强制“量化”为规律的节奏点。
    // 这不仅解决重叠相位带来的刺耳底噪，还彻底释放了底层音频通道并发压力。
    public static void PlayShootSound() => PlaySound("shoot", 80);
    public static void PlayHitSound() => PlaySound("hit", 120);
    public static void PlayDeathSound() => PlaySound("death", 100);
    public static void PlayProjectileSound(string type) => PlaySound(type.ToLower(), 80);

    public static void PlaySound(string effect, int cooldownMs = 40)
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
            int count = _channelCounts.ContainsKey(name) ? _channelCounts[name] : 2;
            int idx = _channelIndices[name];
            string alias = $"{name}_{idx}";

            _channelIndices[name] = (idx + 1) % count;

            // 每次播放前强制根据当前用户的音量设置应用衰减
            mciSendString($"setaudio {alias} volume to {SfxVolume}", null, 0, IntPtr.Zero);
            
            // 恢复单线程播放。前面通道数上限已经削减完毕，
            // 且 MCI 原生异步无阻塞，再用从后台线程调反而会由于线程隔离导致没有声音。
            mciSendString($"play {alias} from 0", null, 0, IntPtr.Zero);
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
                
                // 设置初始音量为 0，不再同时 play
                mciSendString($"setaudio bgm{i} volume to 0", null, 0, IntPtr.Zero);
            }
        }
    }

    public static void PlayBGM(int track) // 1: battle, 2: peace, 3: battle 2
    {
        if (!_bgmInitialized) InitializeBGM();

        if (_currentBGMTrack == track) 
        {
            // 防止放着放着突然没声音：MCI 的 pause 经常会丢失 repeat 标签。
            // 当这首歌应该在放但其实已经停了时，重新激活它！
            if (!IsMutedBGM)
            {
                string currStatus = GetTrackStatus(track);
                if (currStatus.Contains("stopped") || string.IsNullOrEmpty(currStatus))
                {
                    mciSendString($"play bgm{track} from 0", null, 0, IntPtr.Zero);
                }
            }
            return;
        }

        _currentBGMTrack = track;

        // 设置音量目标：目标轨道淡入，其他轨道淡出
        for (int i = 1; i <= 3; i++)
        {
            _targetVolumes[i] = (i == track && !IsMutedBGM) ? 1000f : 0f;
        }

        // 目标轨道开始播放（虽然目前音量可能趋近 0，但 LERP 马上会拉升它）
        if (!IsMutedBGM)
        {
            mciSendString($"resume bgm{track}", null, 0, IntPtr.Zero);
            string status = GetTrackStatus(track);
            if (!status.Contains("playing")) 
            {
                mciSendString($"play bgm{track} repeat", null, 0, IntPtr.Zero);
            }
        }
        else
        {
            mciSendString("pause bgm1", null, 0, IntPtr.Zero);
            mciSendString("pause bgm2", null, 0, IntPtr.Zero);
            mciSendString("pause bgm3", null, 0, IntPtr.Zero);
        }
    }

    /// <summary>
    /// 每帧调用一次，处理淡入淡出
    /// </summary>
    public static void Update(float deltaTime)
    {
        if (!_bgmInitialized) return;

        for (int i = 1; i <= 3; i++)
        {
            float target = IsMutedBGM ? 0 : _targetVolumes[i];
            
            // 使用极小阈值避免浮点误差导致的无限微调
            if (Math.Abs(_currentVolumes[i] - target) > 0.5f)
            {
                // 线性插值趋近目标音量
                if (_currentVolumes[i] < target) _currentVolumes[i] = Math.Min(target, _currentVolumes[i] + FADE_SPEED);
                else _currentVolumes[i] = Math.Max(target, _currentVolumes[i] - FADE_SPEED);

                mciSendString($"setaudio bgm{i} volume to {(int)_currentVolumes[i]}", null, 0, IntPtr.Zero);
                
                // 如果音量彻底归零了，就暂停它，免除后台资源占用和不可预见的重叠Bug
                if (_currentVolumes[i] <= 0 && i != _currentBGMTrack)
                {
                    mciSendString($"pause bgm{i}", null, 0, IntPtr.Zero);
                }
            }
        }
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
