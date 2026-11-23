using System.Collections.Generic;

/// <summary>
/// 장애물 사이클 정수(예: 13 = 1·3번 레일, 0 = 빈 사이클)를 해석하고 검증한다.
/// </summary>
public static class ObstacleCycle
{
    private const int MIN_LANE = 1;
    private const int MAX_LANE = 3;
    private const int CENTER_LANE = (MIN_LANE + MAX_LANE) / 2;
    private const int ALL_LANES_MASK = (1 << (MAX_LANE - MIN_LANE + 1)) - 1;

    /// <summary>
    /// 사이클이 유효한지 확인한다. 음수, 1~3 밖의 레일, 중복 레일, 세 레일을 모두 막는 조합은 거부한다.
    /// </summary>
    public static bool IsValid(int cycle)
    {
        return TryGetLanes(cycle, null);
    }

    /// <summary>
    /// 사이클에 포함된 레일 번호(1~3)를 lanes에 채운다. 유효하지 않으면 lanes를 비우고 false를 반환한다.
    /// </summary>
    public static bool TryGetLanes(int cycle, List<int> lanes)
    {
        lanes?.Clear();

        // 음수 제외
        if (cycle < 0) return false;

        // 사용한 레일을 비트로 기록한다 (할당 없이 중복 검사).
        int usedMask = 0;

        // 각 레일 번호를 차례대로 꺼냄
        while (cycle > 0)
        {
            int lane = cycle % 10;

            // 1 ~ 3번 레일만 허용
            if (lane < MIN_LANE || lane > MAX_LANE) return Fail(lanes);

            // 이미 들어 있는 레일이라면 중복
            int bit = 1 << (lane - MIN_LANE);
            if ((usedMask & bit) != 0) return Fail(lanes);

            usedMask |= bit;
            lanes?.Add(lane);
            cycle /= 10;
        }

        // 세 레일을 모두 막으면 피할 수 없으므로 거부
        if (usedMask == ALL_LANES_MASK) return Fail(lanes);

        return true;
    }

    /// <summary>
    /// 레일 번호(1~3)를 월드 X 좌표로 변환한다. 중앙 레일이 centerLaneX다.
    /// </summary>
    public static float GetLaneX(int lane, float centerLaneX, float laneSpacing)
    {
        return centerLaneX + (lane - CENTER_LANE) * laneSpacing;
    }

    private static bool Fail(List<int> lanes)
    {
        lanes?.Clear();
        return false;
    }
}
