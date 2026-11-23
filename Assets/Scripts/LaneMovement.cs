/// <summary>
/// 한 플레이어의 레일 위치와 이동 쿨타임을 실시간으로 판정한다 (D-007, 틱 없음).
/// 레일이 실제로 바뀐 이동만 쿨타임을 시작하고, 쿨타임 중 입력은 버린다(나중에 실행하지 않음).
/// </summary>
public class LaneMovement
{
    private readonly int minLane;
    private readonly int maxLane;
    private float readyTime = float.NegativeInfinity;

    public int CurrentLane { get; private set; }

    public LaneMovement(int startLane, int minLane, int maxLane)
    {
        this.minLane = minLane;
        this.maxLane = maxLane;
        CurrentLane = Clamp(startLane);
    }

    public bool IsCoolingDown(float time)
    {
        return time < readyTime;
    }

    /// <summary>
    /// direction의 부호 방향으로 한 칸 이동을 시도한다. 레일이 바뀌면 true를 반환하고 쿨타임을 시작한다.
    /// </summary>
    public bool TryMove(int direction, float time, float cooldownSeconds)
    {
        if (IsCoolingDown(time)) return false;

        // 한 번에 한 칸만 이동한다.
        int nextLane = Clamp(CurrentLane + System.Math.Sign(direction));
        if (nextLane == CurrentLane) return false;

        CurrentLane = nextLane;
        readyTime = time + cooldownSeconds;
        return true;
    }

    private int Clamp(int lane)
    {
        if (lane < minLane) return minLane;
        if (lane > maxLane) return maxLane;
        return lane;
    }
}
