using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 잘못된 장애물 패턴을 유효한 패턴으로 대체하는 규칙 (S-303, D-009).
/// </summary>
public class ObstaclePatternResolverTests
{
    private const int NONE = ObstaclePatternResolver.NO_REPLACEMENT;

    private static readonly IReadOnlyList<int> ValidA = new[] { 12, 12 };
    private static readonly IReadOnlyList<int> ValidB = new[] { 13, 0, 2 };
    private static readonly IReadOnlyList<int> Empty = new[] { 0, 0, 0 };
    private static readonly IReadOnlyList<int> NoCycles = new int[0];
    private static readonly IReadOnlyList<int> InvalidCycle = new[] { 2, 123 };

    [Test]
    public void Resolve_AllValid_KeepsOrder()
    {
        int[] resolved = Resolve(new[] { ValidA, Empty, ValidB }, _ => 0);

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, resolved);
    }

    [Test]
    public void Resolve_NullReference_ReplacedByValidPattern()
    {
        int[] resolved = Resolve(new[] { null, ValidA }, _ => 0);

        CollectionAssert.AreEqual(new[] { 1, 1 }, resolved);
    }

    [Test]
    public void Resolve_InvalidCycle_ReplacedByValidPattern()
    {
        int[] resolved = Resolve(new[] { ValidA, InvalidCycle }, _ => 0);

        CollectionAssert.AreEqual(new[] { 0, 0 }, resolved);
    }

    [Test]
    public void Resolve_PicksAmongCandidatesWithObstacles()
    {
        // 후보는 장애물이 있는 유효한 패턴(0, 3)뿐이다. 빈 패턴(1)·사이클 없는 패턴(4)·잘못된 패턴(2)은 제외된다.
        var counts = new List<int>();
        int[] resolved = Resolve(new[] { ValidA, Empty, InvalidCycle, ValidB, NoCycles }, count =>
        {
            counts.Add(count);
            return 1;
        });

        CollectionAssert.AreEqual(new[] { 2 }, counts);
        CollectionAssert.AreEqual(new[] { 0, 1, 3, 3, 4 }, resolved);
    }

    [Test]
    public void Resolve_PicksEachInvalidSlotSeparately()
    {
        var picks = new Queue<int>(new[] { 0, 1 });
        int[] resolved = Resolve(new[] { null, ValidA, InvalidCycle, ValidB }, _ => picks.Dequeue());

        CollectionAssert.AreEqual(new[] { 1, 1, 3, 3 }, resolved);
    }

    [Test]
    public void Resolve_NoCandidates_MarksNoReplacement()
    {
        int[] resolved = Resolve(new[] { null, Empty, InvalidCycle }, _ =>
        {
            Assert.Fail("후보가 없으면 고르지 않는다");
            return 0;
        });

        CollectionAssert.AreEqual(new[] { NONE, 1, NONE }, resolved);
    }

    [Test]
    public void IsValid_RejectsNullAndInvalidCycle()
    {
        Assert.IsFalse(ObstaclePatternResolver.IsValid(null));
        Assert.IsFalse(ObstaclePatternResolver.IsValid(InvalidCycle));
        Assert.IsTrue(ObstaclePatternResolver.IsValid(Empty));
        Assert.IsTrue(ObstaclePatternResolver.IsValid(ValidB));
    }

    private static int[] Resolve(IReadOnlyList<int>[] patterns, System.Func<int, int> pick)
    {
        return ObstaclePatternResolver.Resolve(patterns, pick);
    }
}
