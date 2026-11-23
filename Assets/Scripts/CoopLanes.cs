/// <summary>
/// 두 플레이어의 레일 이동·합체·분리를 실시간으로 판정한다 (D-007, Game_Design §4). 레일 번호는 1~3이다.
/// 한 번의 Step이 한 판정 단위(프레임)이며, 두 플레이어의 이동을 모두 반영한 뒤 최종 레일로 합체를 판정한다.
/// </summary>
public class CoopLanes
{
    public const int LEFT_LANE = 1;
    public const int CENTER_LANE = 2;
    public const int RIGHT_LANE = 3;

    private readonly LaneMovement player1;
    private readonly LaneMovement player2;

    // 이번 합체를 위해 다른 레일에서 들어온 플레이어의 출발 레일. 원래 그 레일에 있던 플레이어는 null.
    private int? player1EntryLane;
    private int? player2EntryLane;

    public bool IsMerged { get; private set; }
    public int Player1Lane => player1.CurrentLane;
    public int Player2Lane => player2.CurrentLane;

    public CoopLanes(int player1StartLane, int player2StartLane)
    {
        player1 = new LaneMovement(player1StartLane, LEFT_LANE, RIGHT_LANE);
        player2 = new LaneMovement(player2StartLane, LEFT_LANE, RIGHT_LANE);

        // 출발 레일 기록 없이 합체한 상태는 분리 목적지를 정할 수 없다.
        if (player1.CurrentLane == player2.CurrentLane)
            throw new System.ArgumentException("두 플레이어는 서로 다른 레일에서 시작해야 합니다.");
    }

    /// <summary>
    /// 한 프레임의 입력을 판정한다. move는 -1(왼쪽)·0·1(오른쪽), split은 각 플레이어의 분리 입력이다.
    /// 합체 중에는 이동을 무시하고, 분리 입력이 하나라도 있으면 한 번만 분리한다.
    /// </summary>
    public void Step(float time, int move1, int move2, bool split1, bool split2, float cooldownSeconds)
    {
        // 합체 전에 들어온 분리 입력과 분리한 프레임의 이동 입력은 버린다(가정). 같은 주기의 분리·이동 순서는 미정(O-09)이다.
        if (IsMerged)
        {
            if (split1 || split2) Split(time, cooldownSeconds);
            return;
        }

        int from1 = player1.CurrentLane;
        int from2 = player2.CurrentLane;
        bool moved1 = player1.TryMove(move1, time, cooldownSeconds);
        bool moved2 = player2.TryMove(move2, time, cooldownSeconds);

        if (player1.CurrentLane != player2.CurrentLane) return;

        IsMerged = true;
        player1EntryLane = moved1 ? from1 : (int?)null;
        player2EntryLane = moved2 ? from2 : (int?)null;
    }

    private void Split(float time, float cooldownSeconds)
    {
        int mergedLane = player1.CurrentLane;
        int destination1;
        int destination2;

        if (mergedLane == CENTER_LANE)
        {
            // 들어온 플레이어는 출발 레일로, 원래 중앙에 있던 플레이어는 반대쪽 끝 레일로 간다.
            destination1 = player1EntryLane ?? Opposite(player2EntryLane.Value);
            destination2 = player2EntryLane ?? Opposite(player1EntryLane.Value);
        }
        else
        {
            // 끝 레일 합체: 들어온 플레이어만 출발 레일(중앙)로 돌아가고 원래 있던 플레이어는 남는다.
            destination1 = player1EntryLane ?? mergedLane;
            destination2 = player2EntryLane ?? mergedLane;
        }

        player1.MoveTo(destination1, time, cooldownSeconds);
        player2.MoveTo(destination2, time, cooldownSeconds);

        // 이전 합체의 기록이 다음 분리에 영향을 주지 않도록 삭제한다.
        player1EntryLane = null;
        player2EntryLane = null;
        IsMerged = false;
    }

    private static int Opposite(int edgeLane)
    {
        return LEFT_LANE + RIGHT_LANE - edgeLane;
    }
}
