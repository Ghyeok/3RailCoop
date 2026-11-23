using NUnit.Framework;

/// <summary>
/// 실시간 레일 이동과 이동 쿨타임 규칙 (S-103).
/// </summary>
public class LaneMovementTests
{
    private const int MIN_LANE = 0;
    private const int MAX_LANE = 2;
    private const float COOLDOWN = 0.2f;

    [Test]
    public void TryMove_ChangesLaneWhenReady()
    {
        var movement = new LaneMovement(1, MIN_LANE, MAX_LANE);

        Assert.IsTrue(movement.TryMove(1, 0f, COOLDOWN));
        Assert.AreEqual(2, movement.CurrentLane);
    }

    [Test]
    public void TryMove_IgnoresInputDuringCooldown()
    {
        var movement = new LaneMovement(1, MIN_LANE, MAX_LANE);
        movement.TryMove(1, 0f, COOLDOWN);

        Assert.IsFalse(movement.TryMove(-1, 0.1f, COOLDOWN));
        Assert.AreEqual(2, movement.CurrentLane);
    }

    [Test]
    public void TryMove_AllowsMoveAfterCooldownEnds()
    {
        var movement = new LaneMovement(1, MIN_LANE, MAX_LANE);
        movement.TryMove(1, 0f, COOLDOWN);

        Assert.IsTrue(movement.TryMove(-1, COOLDOWN, COOLDOWN));
        Assert.AreEqual(1, movement.CurrentLane);
    }

    [Test]
    public void TryMove_InputDuringCooldownIsNotExecutedLater()
    {
        var movement = new LaneMovement(1, MIN_LANE, MAX_LANE);
        movement.TryMove(1, 0f, COOLDOWN);
        movement.TryMove(-1, 0.1f, COOLDOWN);

        // 쿨타임이 끝난 시점에도 무시된 입력이 실행되지 않고, 새 입력만 처리한다.
        Assert.IsFalse(movement.IsCoolingDown(COOLDOWN));
        Assert.AreEqual(2, movement.CurrentLane);
    }

    [TestCase(MIN_LANE, -1)]
    [TestCase(MAX_LANE, 1)]
    public void TryMove_OutwardAtEdgeDoesNotStartCooldown(int edgeLane, int outward)
    {
        var movement = new LaneMovement(edgeLane, MIN_LANE, MAX_LANE);

        Assert.IsFalse(movement.TryMove(outward, 0f, COOLDOWN));
        Assert.AreEqual(edgeLane, movement.CurrentLane);
        Assert.IsFalse(movement.IsCoolingDown(0f));

        // 쿨타임이 시작되지 않았으므로 바로 안쪽으로 이동할 수 있다.
        Assert.IsTrue(movement.TryMove(-outward, 0.01f, COOLDOWN));
    }

    [Test]
    public void TryMove_ZeroCooldownAllowsImmediateMove()
    {
        var movement = new LaneMovement(1, MIN_LANE, MAX_LANE);
        movement.TryMove(1, 0f, 0f);

        Assert.IsTrue(movement.TryMove(-1, 0f, 0f));
    }

    [Test]
    public void TryMove_MovesOnlyOneLanePerInput()
    {
        var movement = new LaneMovement(MIN_LANE, MIN_LANE, MAX_LANE);

        Assert.IsTrue(movement.TryMove(2, 0f, COOLDOWN));
        Assert.AreEqual(MIN_LANE + 1, movement.CurrentLane);
    }

    [Test]
    public void Constructor_ClampsStartLane()
    {
        Assert.AreEqual(MAX_LANE, new LaneMovement(5, MIN_LANE, MAX_LANE).CurrentLane);
        Assert.AreEqual(MIN_LANE, new LaneMovement(-3, MIN_LANE, MAX_LANE).CurrentLane);
    }
}
