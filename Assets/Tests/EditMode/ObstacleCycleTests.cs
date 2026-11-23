using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 장애물 사이클 해석·검증 규칙 (S-301).
/// </summary>
public class ObstacleCycleTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(23)]
    [TestCase(31)]
    public void IsValid_AcceptsPassableCycles(int cycle)
    {
        Assert.IsTrue(ObstacleCycle.IsValid(cycle));
    }

    [TestCase(123, Description = "세 레일 모두 차단")]
    [TestCase(321, Description = "세 레일 모두 차단 (순서 무관)")]
    [TestCase(-1, Description = "음수")]
    [TestCase(4, Description = "없는 레일")]
    [TestCase(10, Description = "0번 레일 포함")]
    [TestCase(11, Description = "중복 레일")]
    public void IsValid_RejectsInvalidCycles(int cycle)
    {
        Assert.IsFalse(ObstacleCycle.IsValid(cycle));
    }

    [Test]
    public void TryGetLanes_ReturnsEachLaneOnce()
    {
        var lanes = new List<int>();

        Assert.IsTrue(ObstacleCycle.TryGetLanes(13, lanes));
        CollectionAssert.AreEquivalent(new[] { 1, 3 }, lanes);
    }

    [Test]
    public void TryGetLanes_EmptyCycleHasNoLanes()
    {
        var lanes = new List<int> { 99 };

        Assert.IsTrue(ObstacleCycle.TryGetLanes(0, lanes));
        Assert.IsEmpty(lanes);
    }

    [Test]
    public void TryGetLanes_InvalidCycleClearsLanes()
    {
        var lanes = new List<int> { 99 };

        Assert.IsFalse(ObstacleCycle.TryGetLanes(123, lanes));
        Assert.IsEmpty(lanes);
    }

    [TestCase(1, -4f)]
    [TestCase(2, 0f)]
    [TestCase(3, 4f)]
    public void GetLaneX_MapsLaneToRailCenter(int lane, float expectedX)
    {
        Assert.AreEqual(expectedX, ObstacleCycle.GetLaneX(lane, 0f, 4f));
    }
}
