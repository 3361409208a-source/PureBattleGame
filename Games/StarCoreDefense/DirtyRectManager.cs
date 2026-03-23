using System;
using System.Collections.Generic;
using System.Drawing;

namespace PureBattleGame.Games.StarCoreDefense;

/// <summary>
/// 脏矩形管理器 - 跟踪需要重绘的区域
/// 用于优化渲染性能，只重绘发生变化的部分
/// </summary>
public class DirtyRectManager
{
    private List<RectangleF> _dirtyRects = new List<RectangleF>();
    private float _mergeThreshold = 100f; // 合并阈值（像素）

    /// <summary>
    /// 添加脏矩形区域
    /// </summary>
    public void AddDirtyRect(float x, float y, float width, float height)
    {
        AddDirtyRect(new RectangleF(x, y, width, height));
    }

    /// <summary>
    /// 添加脏矩形区域
    /// </summary>
    public void AddDirtyRect(RectangleF rect)
    {
        // 确保矩形有效
        if (rect.Width <= 0 || rect.Height <= 0) return;

        // 尝试与现有的脏矩形合并
        for (int i = _dirtyRects.Count - 1; i >= 0; i--)
        {
            var existing = _dirtyRects[i];
            if (ShouldMerge(rect, existing))
            {
                rect = Merge(rect, existing);
                _dirtyRects.RemoveAt(i);
            }
        }

        _dirtyRects.Add(rect);

        // 限制脏矩形数量，避免过多
        if (_dirtyRects.Count > 50)
        {
            // 合并所有矩形
            var combined = _dirtyRects[0];
            for (int i = 1; i < _dirtyRects.Count; i++)
            {
                combined = Merge(combined, _dirtyRects[i]);
            }
            _dirtyRects.Clear();
            _dirtyRects.Add(combined);
        }
    }

    /// <summary>
    /// 检查矩形是否需要合并
    /// </summary>
    private bool ShouldMerge(RectangleF a, RectangleF b)
    {
        // 如果两个矩形相交或距离很近，则合并
        float expandedX = a.X - _mergeThreshold;
        float expandedY = a.Y - _mergeThreshold;
        float expandedW = a.Width + _mergeThreshold * 2;
        float expandedH = a.Height + _mergeThreshold * 2;

        return new RectangleF(expandedX, expandedY, expandedW, expandedH).IntersectsWith(b);
    }

    /// <summary>
    /// 合并两个矩形
    /// </summary>
    private RectangleF Merge(RectangleF a, RectangleF b)
    {
        float minX = Math.Min(a.X, b.X);
        float minY = Math.Min(a.Y, b.Y);
        float maxX = Math.Max(a.X + a.Width, b.X + b.Width);
        float maxY = Math.Max(a.Y + a.Height, b.Y + b.Height);
        return new RectangleF(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// 获取所有脏矩形（副本）
    /// </summary>
    public List<RectangleF> GetDirtyRects()
    {
        return new List<RectangleF>(_dirtyRects);
    }

    /// <summary>
    /// 检查矩形是否与任何脏矩形相交
    /// </summary>
    public bool IntersectsDirtyRect(RectangleF rect)
    {
        foreach (var dirty in _dirtyRects)
        {
            if (rect.IntersectsWith(dirty)) return true;
        }
        return false;
    }

    /// <summary>
    /// 清空所有脏矩形
    /// </summary>
    public void Clear()
    {
        _dirtyRects.Clear();
    }

    /// <summary>
    /// 获取合并后的单个脏矩形（如果数量不多）
    /// 返回是否成功合并为一个矩形
    /// </summary>
    public bool TryGetCombinedRect(out RectangleF combined)
    {
        if (_dirtyRects.Count == 0)
        {
            combined = RectangleF.Empty;
            return false;
        }

        if (_dirtyRects.Count == 1)
        {
            combined = _dirtyRects[0];
            return true;
        }

        // 如果脏矩形数量少，合并为一个
        if (_dirtyRects.Count <= 5)
        {
            combined = _dirtyRects[0];
            for (int i = 1; i < _dirtyRects.Count; i++)
            {
                combined = Merge(combined, _dirtyRects[i]);
            }
            return true;
        }

        combined = RectangleF.Empty;
        return false;
    }
}
