using NUnit.Framework;

/// <summary>
/// 실시간 합체·분리 규칙 (S-203, Game_Design §4). 레일 번호는 기획과 같은 1~3이다.
/// </summary>
public class CoopLanesTests
{
    private const float COOLDOWN = 0.2f;

    private static void AssertLanes(CoopLanes lanes, int expected1, int expected2)
    {
        Assert.AreEqual(expected1, lanes.Player1Lane, "Player1 레일");
        Assert.AreEqual(expected2, lanes.Player2Lane, "Player2 레일");
    }

    [Test]
    public void Constructor_RejectsSameStartLane()
    {
        Assert.Throws<System.ArgumentException>(() => new CoopLanes(2, 2));
    }

    [Test]
    public void Merge_WhenBothPlayersEndOnSameLane()
    {
        var lanes = new CoopLanes(1, 2);

        lanes.Step(0f, 0, -1, false, false, COOLDOWN);

        Assert.IsTrue(lanes.IsMerged);
        AssertLanes(lanes, 1, 1);
    }

    [Test]
    public void Split_Lane1Merge_EnteredPlayerReturnsToCenter()
    {
        var lanes = new CoopLanes(1, 2);
        lanes.Step(0f, 0, -1, false, false, COOLDOWN);

        lanes.Step(1f, 0, 0, true, false, COOLDOWN);

        Assert.IsFalse(lanes.IsMerged);
        AssertLanes(lanes, 1, 2);
    }

    [Test]
    public void Split_Lane3Merge_EnteredPlayerReturnsToCenter()
    {
        var lanes = new CoopLanes(2, 3);
        lanes.Step(0f, 1, 0, false, false, COOLDOWN);

        lanes.Step(1f, 0, 0, false, true, COOLDOWN);

        AssertLanes(lanes, 2, 3);
    }

    [TestCase(1, 2, 1, 0, 1, 3, Description = "Player1이 1번에서 중앙으로 진입")]
    [TestCase(2, 3, 0, -1, 1, 3, Description = "Player2가 3번에서 중앙으로 진입")]
    [TestCase(3, 2, -1, 0, 3, 1, Description = "Player1이 3번에서 중앙으로 진입")]
    public void Split_CenterMerge_EnteredReturnsAndOtherGoesOpposite(
        int start1, int start2, int move1, int move2, int expected1, int expected2)
    {
        var lanes = new CoopLanes(start1, start2);
        lanes.Step(0f, move1, move2, false, false, COOLDOWN);
        Assert.IsTrue(lanes.IsMerged);

        lanes.Step(1f, 0, 0, true, false, COOLDOWN);

        AssertLanes(lanes, expected1, expected2);
    }

    [Test]
    public void Split_CenterMergeFromBothSides_EachReturnsToOwnLane()
    {
        var lanes = new CoopLanes(1, 3);
        lanes.Step(0f, 1, -1, false, false, COOLDOWN);
        Assert.IsTrue(lanes.IsMerged);
        AssertLanes(lanes, 2, 2);

        lanes.Step(1f, 0, 0, true, false, COOLDOWN);

        AssertLanes(lanes, 1, 3);
    }

    [Test]
    public void SameFrameSwap_DoesNotMerge()
    {
        var lanes = new CoopLanes(1, 2);

        lanes.Step(0f, 1, -1, false, false, COOLDOWN);

        Assert.IsFalse(lanes.IsMerged);
        AssertLanes(lanes, 2, 1);
    }

    [Test]
    public void SameFrameMovesToDifferentLanes_DoesNotMerge()
    {
        // 기획 §4 예: 1P 2→1, 2P 3→2가 같은 주기면 최종 위치가 달라 합체하지 않는다.
        var lanes = new CoopLanes(2, 3);

        lanes.Step(0f, -1, -1, false, false, COOLDOWN);

        Assert.IsFalse(lanes.IsMerged);
        AssertLanes(lanes, 1, 2);
    }

    [Test]
    public void Merged_IgnoresMoveInput()
    {
        var lanes = new CoopLanes(1, 2);
        lanes.Step(0f, 0, -1, false, false, COOLDOWN);

        lanes.Step(1f, 1, 1, false, false, COOLDOWN);

        Assert.IsTrue(lanes.IsMerged);
        AssertLanes(lanes, 1, 1);
    }

    [Test]
    public void Split_ClearsRecord_NextMergeUsesNewRecord()
    {
        // 기획 §4 예: 1P가 2→1로 합체·분리 후, 2P가 1→2로 들어와 합체하면 2P는 1번, 1P는 3번으로 간다.
        var lanes = new CoopLanes(2, 1);
        lanes.Step(0f, -1, 0, false, false, COOLDOWN);
        lanes.Step(1f, 0, 0, true, false, COOLDOWN);
        AssertLanes(lanes, 2, 1);

        lanes.Step(2f, 0, 1, false, false, COOLDOWN);
        Assert.IsTrue(lanes.IsMerged);
        lanes.Step(3f, 0, 0, true, false, COOLDOWN);

        AssertLanes(lanes, 3, 1);
    }

    [Test]
    public void Split_AppliesCooldownToBothPlayers()
    {
        var lanes = new CoopLanes(1, 2);
        lanes.Step(0f, 0, -1, false, false, COOLDOWN);
        lanes.Step(1f, 0, 0, true, false, COOLDOWN);
        AssertLanes(lanes, 1, 2);

        // 그대로 남은 Player1도, 돌아간 Player2도 쿨타임 중이다.
        lanes.Step(1f + COOLDOWN / 2f, 1, 1, false, false, COOLDOWN);
        AssertLanes(lanes, 1, 2);

        lanes.Step(1f + COOLDOWN, 0, 1, false, false, COOLDOWN);
        AssertLanes(lanes, 1, 3);
    }

    [Test]
    public void SimultaneousSplit_HappensOnce()
    {
        var lanes = new CoopLanes(1, 2);
        lanes.Step(0f, 0, -1, false, false, COOLDOWN);

        lanes.Step(1f, 0, 0, true, true, COOLDOWN);

        Assert.IsFalse(lanes.IsMerged);
        AssertLanes(lanes, 1, 2);
    }

    [Test]
    public void SplitInput_IgnoredWhenNotMerged()
    {
        var lanes = new CoopLanes(1, 3);

        lanes.Step(0f, 0, 0, true, true, COOLDOWN);

        Assert.IsFalse(lanes.IsMerged);
        AssertLanes(lanes, 1, 3);
    }
}
