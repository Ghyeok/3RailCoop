using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// 무작위 모드의 난이도별 개수 계산·선택·섞기 규칙 (S-502, D-012, D-013).
/// </summary>
public class StagePatternPlannerTests
{
    private static readonly IReadOnlyList<int> Lane1 = new[] { 1 };
    private static readonly IReadOnlyList<int> Lane2 = new[] { 2 };
    private static readonly IReadOnlyList<int> Lane3 = new[] { 3 };
    private static readonly IReadOnlyList<int> Empty = new[] { 0, 0 };
    private static readonly IReadOnlyList<int> InvalidCycle = new[] { 2, 123 };

    [Test]
    public void AllocateCounts_ExactRatio()
    {
        CollectionAssert.AreEqual(new[] { 7, 3 }, StagePatternPlanner.AllocateCounts(10, new[] { 70f, 30f }));
    }

    [Test]
    public void AllocateCounts_Fraction_LargestRemainderGetsExtra()
    {
        // 6 × 70% = 4.2, 6 × 30% = 1.8 → 나머지가 큰 난이도 2가 1개 더 받는다.
        CollectionAssert.AreEqual(new[] { 4, 2 }, StagePatternPlanner.AllocateCounts(6, new[] { 70f, 30f }));
    }

    [Test]
    public void AllocateCounts_TiedRemainder_EarlierGetsExtra()
    {
        CollectionAssert.AreEqual(new[] { 2, 1 }, StagePatternPlanner.AllocateCounts(3, new[] { 50f, 50f }));
    }

    [Test]
    public void AllocateCounts_SumNot100_ScalesByRatio()
    {
        CollectionAssert.AreEqual(new[] { 3, 1 }, StagePatternPlanner.AllocateCounts(4, new[] { 90f, 30f }));
    }

    [Test]
    public void AllocateCounts_SumAlwaysTotal()
    {
        int[] counts = StagePatternPlanner.AllocateCounts(7, new[] { 33.3f, 33.3f, 33.4f });

        Assert.AreEqual(7, counts.Sum());
    }

    [Test]
    public void Create_PicksExactCountPerDifficulty()
    {
        var pool = Pool((Lane1, 1), (Lane3, 2));

        StagePatternPlanner.Plan plan = Create(10, Shares((1, 70f), (2, 30f)), pool, new Random(1));

        Assert.AreEqual(7, plan.PatternIndices.Count(i => i == 0));
        Assert.AreEqual(3, plan.PatternIndices.Count(i => i == 1));
        Assert.IsFalse(plan.PercentSumAdjusted);
        CollectionAssert.IsEmpty(plan.MissingDifficulties);
    }

    [Test]
    public void Create_OnlyPicksAllowedDifficulty()
    {
        var pool = Pool((Lane1, 1), (Lane2, 2), (Lane3, 3));

        StagePatternPlanner.Plan plan = Create(20, Shares((2, 100f)), pool, new Random(2));

        Assert.That(plan.PatternIndices, Is.All.EqualTo(1));
        Assert.AreEqual(20, plan.PatternIndices.Count);
    }

    [Test]
    public void Create_AllowsRepeatsAndPicksEveryCandidate()
    {
        var pool = Pool((Lane1, 1), (Lane2, 1), (Lane3, 1));

        StagePatternPlanner.Plan plan = Create(30, Shares((1, 100f)), pool, new Random(3));

        CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, plan.PatternIndices.Distinct());
        Assert.AreEqual(30, plan.PatternIndices.Count);
    }

    [Test]
    public void Create_ShufflesAcrossDifficulties()
    {
        // 섞지 않으면 난이도 1(인덱스 0) 5개 뒤에 난이도 2(인덱스 1) 5개가 온다.
        var pool = Pool((Lane1, 1), (Lane3, 2));
        var unshuffled = new[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 };
        var random = new Random(4);

        bool shuffled = Enumerable.Range(0, 20)
            .Any(_ => !Create(10, Shares((1, 50f), (2, 50f)), pool, random).PatternIndices.SequenceEqual(unshuffled));

        Assert.IsTrue(shuffled);
    }

    [Test]
    public void Create_EachCallPicksAgain()
    {
        // 재시도마다 새로 고르면 같은 설정에서도 순서가 달라진다.
        var pool = Pool((Lane1, 1), (Lane2, 1), (Lane3, 1));
        var random = new Random(5);
        var orders = new HashSet<string>();

        for (int attempt = 0; attempt < 10; attempt++)
            orders.Add(string.Join(",", Create(4, Shares((1, 100f)), pool, random).PatternIndices));

        Assert.Greater(orders.Count, 1);
    }

    [Test]
    public void Create_PercentSumNot100_AdjustsAndReports()
    {
        var pool = Pool((Lane1, 1), (Lane3, 2));

        StagePatternPlanner.Plan plan = Create(4, Shares((1, 90f), (2, 30f)), pool, new Random(6));

        Assert.IsTrue(plan.PercentSumAdjusted);
        Assert.AreEqual(120f, plan.PercentSum, 0.001f);
        Assert.AreEqual(3, plan.PatternIndices.Count(i => i == 0));
        Assert.AreEqual(1, plan.PatternIndices.Count(i => i == 1));
    }

    [Test]
    public void Create_UnusablePoolPatternsExcludedAndReported()
    {
        var pool = Pool((null, 1), (InvalidCycle, 1), (Empty, 1), (Lane2, 1));

        StagePatternPlanner.Plan plan = Create(5, Shares((1, 100f)), pool, new Random(7));

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, plan.UnusablePoolIndices);
        Assert.That(plan.PatternIndices, Is.All.EqualTo(3));
        Assert.AreEqual(5, plan.PatternIndices.Count);
    }

    [Test]
    public void Create_MissingDifficulty_RedistributesToRemainingByRatio()
    {
        // 난이도 2(20%)에 유효한 패턴이 없으면 10개를 난이도 1·3에 60:20 비율로 나눈다 → 7.5·2.5 → 8·2 (D-013).
        var pool = Pool((Lane1, 1), (InvalidCycle, 2), (Lane3, 3));

        StagePatternPlanner.Plan plan = Create(10, Shares((1, 60f), (2, 20f), (3, 20f)), pool, new Random(8));

        CollectionAssert.AreEqual(new[] { 2 }, plan.MissingDifficulties);
        Assert.AreEqual(8, plan.PatternIndices.Count(i => i == 0));
        Assert.AreEqual(2, plan.PatternIndices.Count(i => i == 2));
    }

    [Test]
    public void Create_MissingDifficulty_SplitsFullSharesFirstThenRedistributes()
    {
        // N=7, 50/30/20 → 3.5/2.1/1.4 → 4/2/1. 난이도 3의 1개를 50:30으로 나누면 0.625/0.375 → 난이도 1이 받는다 → 5/2 (D-013).
        // 남은 난이도만으로 한 번에 나누면 4.375/2.625 → 4/3이 되어 D-013과 다르다.
        var pool = Pool((Lane1, 1), (Lane2, 2));

        StagePatternPlanner.Plan plan = Create(7, Shares((1, 50f), (2, 30f), (3, 20f)), pool, new Random(14));

        Assert.AreEqual(5, plan.PatternIndices.Count(i => i == 0));
        Assert.AreEqual(2, plan.PatternIndices.Count(i => i == 1));
    }

    [Test]
    public void Create_NegativePercent_NotTreatedAsAllowed()
    {
        var pool = Pool((Lane1, 1), (Lane3, 2));

        StagePatternPlanner.Plan plan = Create(3, Shares((1, 50f), (2, -60f)), pool, new Random(15));

        Assert.IsFalse(plan.NoAllowedDifficulty);
        Assert.That(plan.PatternIndices, Is.All.EqualTo(0));
        Assert.AreEqual(3, plan.PatternIndices.Count);
    }

    [Test]
    public void Create_MissingDifficulty_ZeroPercentDifficultyDoesNotTakeOver()
    {
        // 0%인 난이도는 허용하지 않은 것이므로, 비율을 준 난이도에 패턴이 없어도 대신 나오지 않는다.
        var pool = Pool((Lane3, 2));

        StagePatternPlanner.Plan plan = Create(4, Shares((1, 100f), (2, 0f)), pool, new Random(9));

        CollectionAssert.AreEqual(new[] { 1 }, plan.MissingDifficulties);
        CollectionAssert.IsEmpty(plan.PatternIndices);
    }

    [Test]
    public void Create_ZeroPercentDifficulty_NotPicked()
    {
        var pool = Pool((Lane1, 1), (Lane3, 2));

        StagePatternPlanner.Plan plan = Create(10, Shares((1, 100f), (2, 0f)), pool, new Random(12));

        Assert.That(plan.PatternIndices, Is.All.EqualTo(0));
    }

    [TestCase(new float[0])]
    [TestCase(new[] { 0f, 0f })]
    public void Create_NoAllowedDifficulty_ReportsWithoutPercentAdjust(float[] percents)
    {
        var pool = Pool((Lane1, 1), (Lane3, 2));
        var shares = percents.Select((p, i) => (i + 1, p)).ToArray();

        StagePatternPlanner.Plan plan = Create(4, Shares(shares), pool, new Random(13));

        Assert.IsTrue(plan.NoAllowedDifficulty);
        Assert.IsFalse(plan.PercentSumAdjusted);
        CollectionAssert.IsEmpty(plan.MissingDifficulties);
        CollectionAssert.IsEmpty(plan.PatternIndices);
    }

    [Test]
    public void GetClearDistance_IsLastObstacleOffsetPlusMargin()
    {
        Assert.AreEqual(152f, StagePatternPlanner.GetClearDistance(10f, 160f, 2f), 0.001f);
    }

    [Test]
    public void Create_ZeroPercentDifficultyWithoutPatterns_NotReported()
    {
        var pool = Pool((Lane1, 1));

        StagePatternPlanner.Plan plan = Create(2, Shares((1, 100f), (2, 0f)), pool, new Random(10));

        CollectionAssert.IsEmpty(plan.MissingDifficulties);
        Assert.AreEqual(2, plan.PatternIndices.Count);
    }

    [Test]
    public void Create_NoCandidatesForAnyDifficulty_ReturnsEmptyPlan()
    {
        var pool = Pool((InvalidCycle, 1), (Lane2, 3));

        StagePatternPlanner.Plan plan = Create(5, Shares((1, 50f), (2, 50f)), pool, new Random(11));

        CollectionAssert.IsEmpty(plan.PatternIndices);
        CollectionAssert.AreEqual(new[] { 1, 2 }, plan.MissingDifficulties);
    }

    private static StagePatternPlanner.Plan Create(
        int count,
        List<(int Difficulty, float Percent)> shares,
        List<(IReadOnlyList<int> Cycles, int Difficulty)> pool,
        Random random)
    {
        return StagePatternPlanner.Create(count, shares, pool, random.Next);
    }

    private static List<(int Difficulty, float Percent)> Shares(params (int Difficulty, float Percent)[] shares)
    {
        return shares.ToList();
    }

    private static List<(IReadOnlyList<int> Cycles, int Difficulty)> Pool(params (IReadOnlyList<int> Cycles, int Difficulty)[] pool)
    {
        return pool.ToList();
    }
}
