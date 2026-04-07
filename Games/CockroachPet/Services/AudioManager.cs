using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace PureBattleGame.Games.CockroachPet;

/// <summary>
/// 音效管理器 - 使用多种合成技术生成丰富的游戏音效
/// </summary>
public static class AudioManager
{
    private static bool _isInitialized = false;
    private static bool _isEnabled = true;
    private static readonly Random _rand = new();

    // 音效缓存 - 每个音效存储多个变体
    private static readonly Dictionary<string, List<byte[]>> SoundCache = new();

    public static bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public static void Initialize()
    {
        if (_isInitialized) return;
        GenerateAllSounds();
        _isInitialized = true;
    }

    #region Public Sound APIs

    public static void PlayHitSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("hit");
    }

    public static void PlayShootSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("shoot");
    }

    public static void PlayExplosionSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("explosion");
    }

    public static void PlayLaserSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("laser");
    }

    public static void PlayElectricSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("electric");
    }

    public static void PlayBounceSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("bounce");
    }

    public static void PlayDeathSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("death");
    }

    public static void PlayMonsterHitSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("monster_hit");
    }

    public static void PlayMonsterDeathSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("monster_death");
    }

    public static void PlayClashSound()
    {
        if (!_isEnabled) return;
        PlayRandomVariant("clash");
    }

    public static void PlayProjectileSound(string projectileType)
    {
        if (!_isEnabled) return;

        switch (projectileType)
        {
            case "ROCKET": PlayRandomVariant("rocket_launch"); break;
            case "LIGHTNING": PlayRandomVariant("electric"); break;
            case "CANNON": PlayRandomVariant("cannon"); break;
            case "PLASMA": PlayRandomVariant("plasma"); break;
            case "SPIT":
            case "INK": PlayRandomVariant("splash"); break;
            default: PlayRandomVariant("shoot"); break;
        }
    }

    public static void PlayProjectileHitSound(string projectileType)
    {
        if (!_isEnabled) return;

        switch (projectileType)
        {
            case "ROCKET":
            case "CANNON": PlayRandomVariant("explosion"); break;
            case "LIGHTNING": PlayRandomVariant("electric"); break;
            case "PLASMA": PlayRandomVariant("plasma_hit"); break;
            case "SPIT":
            case "INK": PlayRandomVariant("splash_hit"); break;
            default: PlayRandomVariant("hit"); break;
        }
    }

    #endregion

    #region Sound Generation

    private static void PlayRandomVariant(string soundName)
    {
        if (!SoundCache.TryGetValue(soundName, out var variants)) return;
        if (variants.Count == 0) return;

        var soundData = variants[_rand.Next(variants.Count)];

        Task.Run(() =>
        {
            try
            {
                using var stream = new MemoryStream(soundData);
                using var player = new SoundPlayer(stream);
                player.PlaySync();
            }
            catch { /* 忽略音效播放错误 */ }
        });
    }

    private static void GenerateAllSounds()
    {
        // 为每个音效生成3-4个变体，增加多样性
        SoundCache["hit"] = GenerateVariants(GenerateRichHitSound, 4);
        SoundCache["shoot"] = GenerateVariants(GenerateRichShootSound, 4);
        SoundCache["explosion"] = GenerateVariants(GenerateRichExplosionSound, 3);
        SoundCache["laser"] = GenerateVariants(GenerateRichLaserSound, 4);
        SoundCache["electric"] = GenerateVariants(GenerateRichElectricSound, 4);
        SoundCache["bounce"] = GenerateVariants(GenerateRichBounceSound, 3);
        SoundCache["death"] = GenerateVariants(GenerateRichDeathSound, 3);
        SoundCache["monster_hit"] = GenerateVariants(GenerateRichMonsterHitSound, 3);
        SoundCache["monster_death"] = GenerateVariants(GenerateRichMonsterDeathSound, 3);
        SoundCache["clash"] = GenerateVariants(GenerateRichClashSound, 4);
        SoundCache["rocket_launch"] = GenerateVariants(GenerateRichRocketLaunchSound, 3);
        SoundCache["cannon"] = GenerateVariants(GenerateRichCannonSound, 3);
        SoundCache["plasma"] = GenerateVariants(GenerateRichPlasmaSound, 4);
        SoundCache["plasma_hit"] = GenerateVariants(GenerateRichPlasmaHitSound, 3);
        SoundCache["splash"] = GenerateVariants(GenerateRichSplashSound, 3);
        SoundCache["splash_hit"] = GenerateVariants(GenerateRichSplashHitSound, 3);
    }

    private static List<byte[]> GenerateVariants(Func<int, byte[]> generator, int count)
    {
        var variants = new List<byte[]>();
        for (int i = 0; i < count; i++)
        {
            variants.Add(generator(i));
        }
        return variants;
    }

    // 波形生成器枚举
    private enum Waveform { Sine, Square, Sawtooth, Triangle, Noise }

    private static float GenerateWave(Waveform wave, float phase)
    {
        phase = phase % 1.0f;
        if (phase < 0) phase += 1.0f;

        return wave switch
        {
            Waveform.Sine => (float)Math.Sin(phase * 2 * Math.PI),
            Waveform.Square => phase < 0.5f ? 1.0f : -1.0f,
            Waveform.Sawtooth => 2.0f * phase - 1.0f,
            Waveform.Triangle => phase < 0.5f ? (4.0f * phase - 1.0f) : (3.0f - 4.0f * phase),
            Waveform.Noise => (float)(_rand.NextDouble() * 2 - 1),
            _ => 0
        };
    }

    private static float GenerateWave(Waveform wave, float freq, float t)
    {
        return GenerateWave(wave, freq * t);
    }

    // FM合成 - 频率调制，产生更丰富的音色
    private static float FMSynthesize(float t, float carrierFreq, float modFreq, float modIndex, Waveform carrierWave = Waveform.Sine)
    {
        float modulator = (float)Math.Sin(2 * Math.PI * modFreq * t);
        float modulatedPhase = carrierFreq * t + modIndex * modulator;
        return GenerateWave(carrierWave, modulatedPhase);
    }

    // 带通滤波器模拟（简单版本）
    private static float ApplyLowPass(float input, ref float lastOutput, float cutoff)
    {
        lastOutput = lastOutput + cutoff * (input - lastOutput);
        return lastOutput;
    }

    private static byte[] GenerateWavHeader(int dataLength, int sampleRate = 44100, int channels = 1, int bitsPerSample = 8)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write((short)bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        return ms.ToArray();
    }

    #endregion

    #region Rich Sound Generators

    private static byte[] GenerateRichHitSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 80 + variant * 20;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        // 基础频率随变体变化
        float baseFreq = 600 + variant * 150;
        float harmonic2 = baseFreq * 2.5f;
        float harmonic3 = baseFreq * 4.2f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * (15 + variant * 5));

            // 多层谐波
            float sample = GenerateWave(Waveform.Sine, baseFreq * t) * 0.5f;
            sample += GenerateWave(Waveform.Square, harmonic2 * t) * 0.25f * decay;
            sample += GenerateWave(Waveform.Sawtooth, harmonic3 * t) * 0.15f * decay;

            // 噪声冲击
            if (t < 0.02f)
            {
                sample += (float)(_rand.NextDouble() * 2 - 1) * (1 - t * 50) * 0.4f;
            }

            sample *= decay * 120;
            sample = ApplyLowPass(sample, ref lastOut, 0.3f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichShootSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 60 + variant * 15;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        float startFreq = 2000 - variant * 200;
        float endFreq = 400 + variant * 100;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / (duration / 1000f);
            float decay = (float)Math.Exp(-t * (20 + variant * 5));

            // 频率滑降
            float freq = startFreq + (endFreq - startFreq) * progress;

            // FM合成增加金属感
            float sample = FMSynthesize(t, freq, freq * 0.5f, 2.0f * decay, Waveform.Square);
            sample += GenerateWave(Waveform.Sawtooth, freq * 1.5f * t) * 0.3f * decay;

            // 白噪声层
            sample += (float)(_rand.NextDouble() * 2 - 1) * 0.2f * decay;

            sample *= 100 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.4f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichExplosionSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 300 + variant * 100;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut1 = 0, lastOut2 = 0;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * (3 + variant));

            // 粉红噪声（低频更多）
            float noise = (float)(_rand.NextDouble() * 2 - 1);
            noise = ApplyLowPass(noise, ref lastOut1, 0.5f);

            // 低频隆隆声
            float rumble = GenerateWave(Waveform.Sine, 60 + variant * 20, t);
            rumble += GenerateWave(Waveform.Sawtooth, 120, t) * 0.5f;

            // 冲击波
            float impact = 0;
            if (t < 0.05f)
            {
                impact = (float)Math.Sin(t * 628) * (1 - t * 20) * 0.8f;
            }

            float sample = (noise * 0.4f + rumble * 0.4f + impact) * 120 * decay;
            sample = ApplyLowPass(sample, ref lastOut2, 0.6f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichLaserSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 120 + variant * 30;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        float startFreq = 800 + variant * 200;
        float endFreq = 3000 + variant * 500;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / (duration / 1000f);
            float decay = (float)Math.Exp(-t * 8);

            // 上升音调
            float freq = startFreq + (endFreq - startFreq) * progress;

            // 锯齿波基础 + 正弦和声
            float sample = GenerateWave(Waveform.Sawtooth, freq, t) * 0.6f;
            sample += GenerateWave(Waveform.Sine, freq * 2, t) * 0.3f;

            // 颤音效果
            float vibrato = 1 + (float)Math.Sin(t * 50) * 0.05f;
            sample *= vibrato * 100 * decay;

            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichElectricSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 150 + variant * 50;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        float baseFreq = 800 + variant * 200;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * (10 + variant * 3));

            // 高频锯齿波
            float sample = GenerateWave(Waveform.Sawtooth, baseFreq, t) * 0.5f;

            // 快速的频率调制（电弧效果）
            float arcMod = (float)Math.Sin(t * 200 + _rand.NextDouble() * 10);
            sample += GenerateWave(Waveform.Square, baseFreq * 1.5f + arcMod * 100, t) * 0.3f;

            // 爆裂噪声
            if (_rand.Next(100) < 30)
            {
                sample += (float)(_rand.NextDouble() * 2 - 1) * 0.4f * decay;
            }

            sample *= 120 * decay;
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichBounceSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 100 + variant * 20;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        float freq = 300 + variant * 100;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 15);

            // 弹簧效果 - 频率轻微下降
            float currentFreq = freq * (1 - t * 2);

            float sample = GenerateWave(Waveform.Sine, currentFreq, t) * 0.6f;
            sample += GenerateWave(Waveform.Triangle, currentFreq * 2.5f, t) * 0.3f * decay;

            sample *= 100 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.5f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichDeathSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 400 + variant * 100;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        float startFreq = 400 + variant * 100;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / (duration / 1000f);
            float decay = (float)Math.Exp(-t * 2.5f);

            // 下降音调 + 抖动
            float wobble = (float)Math.Sin(t * 30) * 20;
            float freq = startFreq - progress * 350 + wobble;
            if (freq < 50) freq = 50;

            // 方波增加机械感
            float sample = GenerateWave(Waveform.Square, freq, t) * 0.5f;
            sample += GenerateWave(Waveform.Sawtooth, freq * 0.5f, t) * 0.3f * decay;

            // 故障噪声
            if (_rand.Next(100) < 10)
            {
                sample += (float)(_rand.NextDouble() * 2 - 1) * 0.3f;
            }

            sample *= 100 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.4f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichMonsterHitSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 120 + variant * 30;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        float baseFreq = 150 + variant * 50;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 10);

            // 低沉的轰鸣
            float sample = GenerateWave(Waveform.Sawtooth, baseFreq, t) * 0.5f;
            sample += GenerateWave(Waveform.Square, baseFreq * 1.5f, t) * 0.25f * decay;

            // 冲击噪声
            if (t < 0.03f)
            {
                sample += (float)(_rand.NextDouble() * 2 - 1) * (1 - t * 33) * 0.5f;
            }

            sample *= 120 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.3f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichMonsterDeathSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 600 + variant * 150;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / (duration / 1000f);
            float decay = (float)Math.Exp(-t * 1.5f);

            // 低沉的哀鸣
            float freq = 200 - progress * 180;
            if (freq < 40) freq = 40;

            float sample = GenerateWave(Waveform.Sawtooth, freq, t) * 0.4f;
            sample += GenerateWave(Waveform.Sine, freq * 0.5f, t) * 0.4f * decay;

            // 咕嘟咕嘟的气泡声
            if (_rand.Next(100) < 20)
            {
                sample += (float)(_rand.NextDouble() * 2 - 1) * 0.3f * decay;
            }

            sample *= 100 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.35f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichClashSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 180 + variant * 40;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        float metalFreq = 800 + variant * 200;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 8);

            // 金属共振 - 不和谐音程
            float sample = GenerateWave(Waveform.Square, metalFreq, t) * 0.3f;
            sample += GenerateWave(Waveform.Triangle, metalFreq * 1.41f, t) * 0.25f * decay; // 增四度
            sample += GenerateWave(Waveform.Sine, metalFreq * 2.0f, t) * 0.2f * decay;

            // 高频铃声
            sample += GenerateWave(Waveform.Sine, metalFreq * 4, t) * 0.15f * decay * decay;

            // 冲击噪声
            if (t < 0.02f)
            {
                sample += (float)(_rand.NextDouble() * 2 - 1) * (1 - t * 50) * 0.6f;
            }

            sample *= 100 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.5f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichRocketLaunchSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 250 + variant * 50;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / (duration / 1000f);
            float decay = (float)Math.Exp(-t * 4);

            // 上升的呼啸声
            float freq = 150 + progress * 300;

            float sample = FMSynthesize(t, freq, 20, 3.0f, Waveform.Sawtooth);
            sample += GenerateWave(Waveform.Noise, 0, 0) * 0.3f * decay;

            // 隆隆低音
            sample += GenerateWave(Waveform.Sine, 60, t) * 0.4f * decay;

            sample *= 100 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.4f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichCannonSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 400 + variant * 100;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut1 = 0, lastOut2 = 0;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 4);

            // 初始爆震
            float impact = 0;
            if (t < 0.1f)
            {
                impact = (float)Math.Sin(t * 100) * (1 - t * 10) * 0.8f;
            }

            // 噪声冲击波
            float noise = (float)(_rand.NextDouble() * 2 - 1);
            noise = ApplyLowPass(noise, ref lastOut1, 0.3f);

            // 极低频隆隆
            float rumble = GenerateWave(Waveform.Sawtooth, 50 + variant * 10, t) * 0.6f;
            rumble += GenerateWave(Waveform.Sine, 30, t) * 0.4f;

            float sample = (impact + noise * 0.5f + rumble * 0.6f) * 120 * decay;
            sample = ApplyLowPass(sample, ref lastOut2, 0.5f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichPlasmaSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 130 + variant * 30;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        float baseFreq = 500 + variant * 150;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 10);

            // 快速颤音
            float vibrato = (float)Math.Sin(t * 80) * 0.1f;

            // 锯齿波 + FM调制
            float sample = FMSynthesize(t, baseFreq * (1 + vibrato), baseFreq * 0.25f, 2.0f, Waveform.Sawtooth);
            sample += GenerateWave(Waveform.Sine, baseFreq * 2.5f, t) * 0.3f * decay;

            // 能量爆裂
            if (_rand.Next(100) < 15)
            {
                sample += (float)(_rand.NextDouble() * 2 - 1) * 0.3f * decay;
            }

            sample *= 100 * decay;
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichPlasmaHitSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 180 + variant * 40;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        float startFreq = 1200 + variant * 200;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / (duration / 1000f);
            float decay = (float)Math.Exp(-t * 8);

            // 快速下降的高频
            float freq = startFreq * (1 - progress * 0.9f);

            float sample = GenerateWave(Waveform.Sawtooth, freq, t) * 0.5f;
            sample += GenerateWave(Waveform.Sine, freq * 1.5f, t) * 0.3f * decay;

            // 嘶嘶声
            if (t < 0.1f)
            {
                sample += (float)(_rand.NextDouble() * 2 - 1) * (0.1f - t) * 5f * 0.3f;
            }

            sample *= 100 * decay;
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichSplashSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 130 + variant * 30;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 12);

            // 气泡噪声
            float noise = (float)(_rand.NextDouble() * 2 - 1);

            // 低频液体涌动
            float liquid = GenerateWave(Waveform.Sine, 80 + variant * 20, t) * 0.3f * decay;

            float sample = (noise * 0.5f + liquid) * 100 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.4f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    private static byte[] GenerateRichSplashHitSound(int variant)
    {
        int sampleRate = 44100;
        int duration = 220 + variant * 50;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        float lastOut = 0;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 8);

            // 湿润的飞溅声
            float noise = (float)(_rand.NextDouble() * 2 - 1);

            // 随机脉冲模拟水滴
            float droplets = 0;
            if (_rand.Next(100) < 5)
            {
                droplets = (float)(_rand.NextDouble()) * 0.5f * decay;
            }

            float sample = (noise * 0.4f + droplets) * 100 * decay;
            sample = ApplyLowPass(sample, ref lastOut, 0.5f);
            data[i] = (byte)(128 + Math.Clamp((int)sample, -128, 127));
        }

        return GenerateWavHeader(data.Length, sampleRate).Concat(data).ToArray();
    }

    #endregion
}
