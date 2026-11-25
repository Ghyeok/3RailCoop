using NUnit.Framework;

/// <summary>
/// 지나간 장애물 정리 기준 (S-305, D-014).
/// </summary>
public class ObstacleDespawnTests
{
    [TestCase(12f, 18f, 6f, ExpectedResult = true, TestName = "IsPassed_ExactlyDistanceBehind_True")]
    [TestCase(12f, 20f, 6f, ExpectedResult = true, TestName = "IsPassed_FurtherBehind_True")]
    [TestCase(12f, 17.9f, 6f, ExpectedResult = false, TestName = "IsPassed_JustShortOfDistance_False")]
    [TestCase(12f, 10f, 6f, ExpectedResult = false, TestName = "IsPassed_AheadOfReference_False")]
    [TestCase(12f, 12f, 0f, ExpectedResult = true, TestName = "IsPassed_ZeroDistance_AtReference_True")]
    public bool IsPassed(float obstacleZ, float referenceZ, float distanceBehind)
    {
        return ObstacleDespawn.IsPassed(obstacleZ, referenceZ, distanceBehind);
    }
}
