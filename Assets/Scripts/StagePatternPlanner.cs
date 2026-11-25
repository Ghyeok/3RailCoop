using System;
using System.Collections.Generic;

/// <summary>
/// 무작위 모드 스테이지에서 이번 시도에 생성할 패턴과 순서를 정한다 (D-012, D-013).
/// 난이도별 개수는 패턴 개수 × 비율로 정확히 맞추고, 각 난이도 안에서 무작위로 고른 뒤(반복 허용) 전체를 섞는다.
/// </summary>
public static class StagePatternPlanner
{
    private const float PERCENT_TOLERANCE = 0.01f;

    public class Plan
    {
        /// <summary>생성할 순서대로 놓인 후보 목록의 인덱스.</summary>
        public readonly List<int> PatternIndices = new List<int>();

        /// <summary>장애물이 있는 유효한 패턴이 아니라 후보에서 뺀 인덱스(빈 참조, 유효하지 않은 사이클, 장애물 없음).</summary>
        public readonly List<int> UnusablePoolIndices = new List<int>();

        /// <summary>비율을 줬지만 유효한 패턴이 없어 남은 난이도에 개수를 나눈 난이도 (D-013).</summary>
        public readonly List<int> MissingDifficulties = new List<int>();

        /// <summary>비율이 0보다 큰 난이도가 하나도 없어 생성할 패턴이 없는지.</summary>
        public bool NoAllowedDifficulty;

        /// <summary>비율 합이 100%가 아니어서 비율대로 다시 계산했는지.</summary>
        public bool PercentSumAdjusted;

        public float PercentSum;
    }

    /// <summary>
    /// 패턴 순서를 정한다. pool은 후보 패턴의 (사이클, 난이도) 목록이고 사이클이 null이면 빈 참조다.
    /// pickIndex(개수)는 0 이상 개수 미만의 정수를 돌려준다.
    /// </summary>
    public static Plan Create(
        int patternCount,
        IReadOnlyList<(int Difficulty, float Percent)> shares,
        IReadOnlyList<(IReadOnlyList<int> Cycles, int Difficulty)> pool,
        Func<int, int> pickIndex)
    {
        var plan = new Plan();

        var candidates = new Dictionary<int, List<int>>();
        for (int i = 0; i < pool.Count; i++)
        {
            IReadOnlyList<int> cycles = pool[i].Cycles;
            if (!ObstaclePatternResolver.IsValid(cycles) || !ObstaclePatternResolver.HasObstacle(cycles))
            {
                plan.UnusablePoolIndices.Add(i);
                continue;
            }

            if (!candidates.TryGetValue(pool[i].Difficulty, out List<int> list))
                candidates[pool[i].Difficulty] = list = new List<int>();
            list.Add(i);
        }

        // 비율이 0인 난이도는 허용하지 않은 것으로 보고, 재분배(D-013)에서도 받지 않는다.
        var allowedShares = new List<(int Difficulty, float Percent)>();
        foreach ((int Difficulty, float Percent) share in shares)
        {
            plan.PercentSum += share.Percent;
            if (share.Percent > 0f) allowedShares.Add(share);
        }

        plan.NoAllowedDifficulty = allowedShares.Count == 0;
        plan.PercentSumAdjusted = !plan.NoAllowedDifficulty && Math.Abs(plan.PercentSum - 100f) > PERCENT_TOLERANCE;
        if (plan.NoAllowedDifficulty) return plan;

        // 허용 난이도 전체의 비율로 개수를 나눈 뒤, 유효한 패턴이 없는 난이도의 개수를 남은 난이도에 비율대로 다시 나눈다 (D-013).
        int[] counts = AllocateCounts(patternCount, Percents(allowedShares));
        var usableShares = new List<(int Difficulty, float Percent)>();
        var usableCounts = new List<int>();
        int missingCount = 0;
        for (int i = 0; i < allowedShares.Count; i++)
        {
            if (candidates.ContainsKey(allowedShares[i].Difficulty))
            {
                usableShares.Add(allowedShares[i]);
                usableCounts.Add(counts[i]);
            }
            else
            {
                plan.MissingDifficulties.Add(allowedShares[i].Difficulty);
                missingCount += counts[i];
            }
        }

        if (usableShares.Count == 0) return plan;

        int[] redistributed = AllocateCounts(missingCount, Percents(usableShares));
        for (int i = 0; i < usableShares.Count; i++)
        {
            List<int> list = candidates[usableShares[i].Difficulty];
            for (int c = 0; c < usableCounts[i] + redistributed[i]; c++) plan.PatternIndices.Add(list[pickIndex(list.Count)]);
        }

        Shuffle(plan.PatternIndices, pickIndex);
        return plan;
    }

    /// <summary>
    /// 클리어 지점을 플레이어가 시작 위치부터 전진한 거리로 돌려준다. 마지막 장애물 Z보다 margin만큼 뒤다.
    /// </summary>
    public static float GetClearDistance(float playerStartZ, float lastObstacleZ, float margin)
    {
        return lastObstacleZ - playerStartZ + margin;
    }

    private static float[] Percents(List<(int Difficulty, float Percent)> shares)
    {
        var percents = new float[shares.Count];
        for (int i = 0; i < percents.Length; i++) percents[i] = shares[i].Percent;
        return percents;
    }

    /// <summary>
    /// total을 비율대로 나눈 정수 개수를 돌려준다. 소수점은 나머지가 큰 순서(같으면 앞 순서)로 1개씩 더해 합을 total로 맞춘다.
    /// 비율 합은 0보다 커야 한다.
    /// </summary>
    public static int[] AllocateCounts(int total, IReadOnlyList<float> weights)
    {
        var counts = new int[weights.Count];
        if (weights.Count == 0) return counts;

        double sum = 0;
        foreach (float weight in weights) sum += weight;

        var remainders = new double[weights.Count];
        int assigned = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            double exact = total * weights[i] / sum;
            counts[i] = (int)Math.Floor(exact);
            remainders[i] = exact - counts[i];
            assigned += counts[i];
        }

        var order = new List<int>();
        for (int i = 0; i < weights.Count; i++) order.Add(i);
        order.Sort((a, b) => remainders[a] != remainders[b] ? remainders[b].CompareTo(remainders[a]) : a.CompareTo(b));

        for (int k = 0; k < total - assigned; k++) counts[order[k]]++;
        return counts;
    }

    private static void Shuffle(List<int> items, Func<int, int> pickIndex)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = pickIndex(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
