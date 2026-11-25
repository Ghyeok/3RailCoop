/// <summary>
/// 지나간 장애물을 정리하는 기준 (S-305, D-014).
/// </summary>
public static class ObstacleDespawn
{
    /// <summary>
    /// 장애물이 기준 Z보다 distanceBehind 이상 뒤에 있으면 true를 돌려준다.
    /// </summary>
    public static bool IsPassed(float obstacleZ, float referenceZ, float distanceBehind)
    {
        return referenceZ - obstacleZ >= distanceBehind;
    }
}
