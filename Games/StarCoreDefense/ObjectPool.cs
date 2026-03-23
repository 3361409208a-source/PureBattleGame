using System;
using System.Collections.Generic;

namespace PureBattleGame.Games.StarCoreDefense;

/// <summary>
/// 通用对象池 - 减少GC压力，重用临时对象
/// </summary>
public class ObjectPool<T> where T : class, new()
{
    private readonly Queue<T> _pool = new Queue<T>();
    private readonly int _maxSize;
    private readonly Action<T>? _resetAction;
    private int _activeCount;

    public ObjectPool(int initialCapacity = 50, int maxSize = 500, Action<T>? resetAction = null)
    {
        _maxSize = maxSize;
        _resetAction = resetAction;

        // 预创建对象
        for (int i = 0; i < initialCapacity; i++)
        {
            _pool.Enqueue(new T());
        }
    }

    public int PooledCount => _pool.Count;
    public int ActiveCount => _activeCount;

    public T Acquire()
    {
        T obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
        }
        else
        {
            obj = new T();
        }
        _activeCount++;
        return obj;
    }

    public void Release(T obj)
    {
        if (obj == null) return;

        _resetAction?.Invoke(obj);

        if (_pool.Count < _maxSize)
        {
            _pool.Enqueue(obj);
        }
        _activeCount--;
    }

    public void Clear()
    {
        _pool.Clear();
        _activeCount = 0;
    }
}

/// <summary>
/// 对象池管理器 - 集中管理游戏中所有对象池
/// </summary>
public static class GameObjectPools
{
    // 粒子池 - 最多1000个，游戏中最频繁创建的对象
    public static readonly ObjectPool<Particle> Particles = new ObjectPool<Particle>(
        initialCapacity: 200,
        maxSize: 1000,
        resetAction: p =>
        {
            p.X = 0; p.Y = 0;
            p.Dx = 0; p.Dy = 0;
            p.Color = default;
            p.Life = 0; p.MaxLife = 0;
            p.Size = 0;
            p.IsActive = false;
            p.Type = "NORMAL";
        }
    );

    // 投射物池 - 最多500个
    public static readonly ObjectPool<Projectile> Projectiles = new ObjectPool<Projectile>(
        initialCapacity: 100,
        maxSize: 500,
        resetAction: p =>
        {
            p.X = 0; p.Y = 0;
            p.OriginX = 0; p.OriginY = 0;
            p.Dx = 0; p.Dy = 0;
            p.TargetX = 0; p.TargetY = 0;
            p.Owner = null;
            p.TrackingTarget = null;
            p.TrackingMonster = null;
            p.ProjectileColor = default;
            p.IsActive = false;
            p.Type = "BULLET";
            p.LifeTime = 0;
            p.Damage = 0;
            p.Trail.Clear();
            p.IsCluster = false;
            p.ExplosionRadius = 0;
            p.Size = 0;
            p.IsMonsterProjectile = false;
            p.ChainCount = 0;
            p.PenetrationCount = 0;
            p.HitEntityIds.Clear();
        }
    );

    // 浮动文本池 - 最多200个
    public static readonly ObjectPool<FloatingText> FloatingTexts = new ObjectPool<FloatingText>(
        initialCapacity: 50,
        maxSize: 200,
        resetAction: ft =>
        {
            ft.X = 0; ft.Y = 0;
            ft.Dy = 0;
            ft.Text = "";
            ft.TextColor = default;
            ft.Life = 0; ft.MaxLife = 0;
        }
    );

    /// <summary>
    /// 清空所有对象池（用于游戏重置）
    /// </summary>
    public static void ClearAll()
    {
        Particles.Clear();
        Projectiles.Clear();
        FloatingTexts.Clear();
    }
}
