namespace PureBattleGame.Games.StarCoreDefense;

// 简化版 AudioManager（静默，不播放声音）
public static class AudioManager
{
    public static void Initialize() { }

    public static void PlayShootSound() { }
    public static void PlayLaserSound() { }
    public static void PlayProjectileSound(string type) { }
    public static void PlayHitSound() { }
    public static void PlayDeathSound() { }
    public static void PlaySound(string effect) { }
}
