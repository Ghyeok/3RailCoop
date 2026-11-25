public enum StageState
{
    Playing,
    GameOver,
    Cleared
}

/// <summary>
/// 한 스테이지 진행의 상태 전이(진행·게임 오버·클리어)와 진행 거리를 관리한다 (Game_Design §3 승리와 패배).
/// 두 플레이어가 상태를 공유하므로 한 명만 부딪혀도 공동 게임 오버가 된다.
/// </summary>
public class StageRun
{
    public StageState State { get; private set; } = StageState.Playing;
    public float Distance { get; private set; }
    public float TargetDistance { get; private set; }

    /// <summary>진행 중일 때만 이동·분리 입력과 전진을 받는다.</summary>
    public bool IsPlaying => State == StageState.Playing;

    public StageRun(float targetDistance)
    {
        TargetDistance = targetDistance;
    }

    /// <summary>
    /// 목표 거리를 바꾼다. 무작위 모드는 마지막 장애물을 생성한 뒤에 클리어 지점이 정해진다 (D-012).
    /// </summary>
    public void SetTargetDistance(float targetDistance)
    {
        TargetDistance = targetDistance;
    }

    /// <summary>
    /// 진행 거리를 늘린다. 목표 거리에 도달하면 클리어한다. 진행 중이 아니면 무시한다.
    /// </summary>
    public void Advance(float deltaDistance)
    {
        if (!IsPlaying) return;

        Distance += deltaDistance;
        if (Distance >= TargetDistance) State = StageState.Cleared;
    }

    /// <summary>
    /// 플레이어가 장애물에 부딪혔다. 진행 중이면 게임 오버로 바꾸고 true를 반환한다.
    /// </summary>
    public bool ReportCollision()
    {
        if (!IsPlaying) return false;

        State = StageState.GameOver;
        return true;
    }

    /// <summary>
    /// 게임 오버나 클리어 뒤 스테이지 처음부터 다시 시작한다. 진행 중이면 무시하고 false를 반환한다.
    /// </summary>
    public bool Restart()
    {
        if (IsPlaying) return false;

        Distance = 0f;
        State = StageState.Playing;
        return true;
    }
}
