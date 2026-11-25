using System;
using System.Collections.Generic;

/// <summary>
/// 패턴 목록에서 잘못된 패턴(빈 참조, 유효하지 않은 사이클)을 같은 목록의 유효한 패턴으로 대체할 자리를 정한다 (D-009).
/// </summary>
public static class ObstaclePatternResolver
{
    public const int NO_REPLACEMENT = -1;

    /// <summary>
    /// 패턴이 유효한지 확인한다. 빈 참조(null)나 유효하지 않은 사이클이 있으면 false.
    /// </summary>
    public static bool IsValid(IReadOnlyList<int> cycles)
    {
        if (cycles == null) return false;

        foreach (int cycle in cycles)
        {
            if (!ObstacleCycle.IsValid(cycle)) return false;
        }

        return true;
    }

    /// <summary>
    /// 각 자리에서 생성할 패턴의 인덱스를 돌려준다. 유효한 자리는 자기 인덱스, 잘못된 자리는
    /// 장애물이 하나 이상 있는 유효한 패턴 중 pickIndex(후보 수)로 고른 인덱스, 후보가 없으면 NO_REPLACEMENT다.
    /// </summary>
    public static int[] Resolve(IReadOnlyList<IReadOnlyList<int>> patterns, Func<int, int> pickIndex)
    {
        var candidates = new List<int>();
        for (int i = 0; i < patterns.Count; i++)
        {
            if (IsValid(patterns[i]) && HasObstacle(patterns[i])) candidates.Add(i);
        }

        var resolved = new int[patterns.Count];
        for (int i = 0; i < patterns.Count; i++)
        {
            if (IsValid(patterns[i])) resolved[i] = i;
            else if (candidates.Count == 0) resolved[i] = NO_REPLACEMENT;
            else resolved[i] = candidates[pickIndex(candidates.Count)];
        }

        return resolved;
    }

    /// <summary>
    /// 패턴에 장애물이 있는 사이클(0이 아닌 사이클)이 하나 이상 있는지 확인한다.
    /// </summary>
    public static bool HasObstacle(IReadOnlyList<int> cycles)
    {
        foreach (int cycle in cycles)
        {
            if (cycle != 0) return true;
        }

        return false;
    }
}
