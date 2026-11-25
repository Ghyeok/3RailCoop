using NUnit.Framework;

/// <summary>
/// 진행·게임 오버·클리어 상태 전이 (S-302).
/// </summary>
public class StageRunTests
{
    private const float TARGET = 100f;

    [Test]
    public void StartsPlayingAtZeroDistance()
    {
        var run = new StageRun(TARGET);

        Assert.AreEqual(StageState.Playing, run.State);
        Assert.AreEqual(0f, run.Distance);
        Assert.IsTrue(run.IsPlaying);
    }

    [Test]
    public void SetTargetDistance_ClearsAtNewTarget()
    {
        // 무작위 모드는 클리어 지점이 정해지기 전까지 목표 거리를 무한대로 둔다 (S-502).
        var run = new StageRun(float.PositiveInfinity);
        run.Advance(500f);
        Assert.IsTrue(run.IsPlaying);

        run.SetTargetDistance(510f);
        run.Advance(9f);
        Assert.IsTrue(run.IsPlaying);
        run.Advance(1f);

        Assert.AreEqual(StageState.Cleared, run.State);
    }

    [Test]
    public void Collision_CausesSharedGameOver()
    {
        var run = new StageRun(TARGET);

        Assert.IsTrue(run.ReportCollision());

        Assert.AreEqual(StageState.GameOver, run.State);
        Assert.IsFalse(run.IsPlaying);
    }

    [Test]
    public void SecondCollision_IsIgnored()
    {
        var run = new StageRun(TARGET);
        run.ReportCollision();

        // 두 플레이어가 함께 부딪혀도 게임 오버는 한 번이다.
        Assert.IsFalse(run.ReportCollision());
        Assert.AreEqual(StageState.GameOver, run.State);
    }

    [Test]
    public void GameOver_IgnoresAdvance()
    {
        var run = new StageRun(TARGET);
        run.Advance(10f);
        run.ReportCollision();

        run.Advance(TARGET);

        Assert.AreEqual(10f, run.Distance);
        Assert.AreEqual(StageState.GameOver, run.State);
    }

    [Test]
    public void AcceptsInputOnlyWhilePlaying()
    {
        // StageController는 IsPlaying이 false인 동안 이동·분리 입력을 받지 않는다.
        var run = new StageRun(TARGET);
        Assert.IsTrue(run.IsPlaying);

        run.ReportCollision();
        Assert.IsFalse(run.IsPlaying, "게임 오버 중 입력 무시");

        run.Restart();
        Assert.IsTrue(run.IsPlaying);

        run.Advance(TARGET);
        Assert.IsFalse(run.IsPlaying, "클리어 후 입력 무시");
    }

    [Test]
    public void ReachingTargetDistance_Clears()
    {
        var run = new StageRun(TARGET);

        run.Advance(TARGET - 1f);
        Assert.AreEqual(StageState.Playing, run.State);

        run.Advance(1f);
        Assert.AreEqual(StageState.Cleared, run.State);
        Assert.IsFalse(run.IsPlaying);
    }

    [Test]
    public void Collision_AfterClear_IsIgnored()
    {
        var run = new StageRun(TARGET);
        run.Advance(TARGET);

        Assert.IsFalse(run.ReportCollision());
        Assert.AreEqual(StageState.Cleared, run.State);
    }

    [Test]
    public void Restart_FromGameOver_ResetsToStart()
    {
        var run = new StageRun(TARGET);
        run.Advance(30f);
        run.ReportCollision();

        Assert.IsTrue(run.Restart());

        Assert.AreEqual(StageState.Playing, run.State);
        Assert.AreEqual(0f, run.Distance);
    }

    [Test]
    public void Restart_FromClear_ResetsToStart()
    {
        var run = new StageRun(TARGET);
        run.Advance(TARGET);

        Assert.IsTrue(run.Restart());

        Assert.AreEqual(StageState.Playing, run.State);
        Assert.AreEqual(0f, run.Distance);
    }

    [Test]
    public void Restart_WhilePlaying_IsIgnored()
    {
        var run = new StageRun(TARGET);
        run.Advance(30f);

        Assert.IsFalse(run.Restart());
        Assert.AreEqual(30f, run.Distance);
    }
}
