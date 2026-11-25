using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 한 기기에서 두 플레이어를 조작한다 (MVP, D-007). 두 플레이어의 입력을 한 프레임 단위로 모아
/// <see cref="CoopLanes"/>로 이동·합체·분리를 판정하고, 전진 속도를 한 곳에서 관리한다.
/// </summary>
public class CoopController : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [Header("Input")]
    [SerializeField] private Key player1LeftKey = Key.A;
    [SerializeField] private Key player1RightKey = Key.D;
    [SerializeField] private Key player2LeftKey = Key.LeftArrow;
    [SerializeField] private Key player2RightKey = Key.RightArrow;
    [Tooltip("합체 중 누르면 분리한다. 한 기기에서는 두 플레이어가 같은 키를 쓴다.")]
    [SerializeField] private Key splitKey = Key.Space;

    [Header("Lanes")]
    [SerializeField] private float centerLaneX;
    [SerializeField, Min(0.01f)] private float laneSpacing = 4f;

    [Header("Movement")]
    [Tooltip("두 플레이어와 카메라가 함께 쓰는 +Z 전진 속도.")]
    [SerializeField, Min(0f)] private float forwardSpeed = 5f;
    [Tooltip("레일이 바뀐 뒤·분리 직후 다음 이동을 받기까지의 시간(초). 최종값은 플레이 테스트로 정한다 (O-01).")]
    [SerializeField, Min(0f)] private float moveCooldownSeconds = 0.2f;

    private CoopLanes lanes;
    private Vector3 player1StartPosition;
    private Vector3 player2StartPosition;

    public float ForwardSpeed => forwardSpeed;

    /// <summary>진행 거리(TravelledDistance)의 기준인 Player1 시작 Z.</summary>
    public float StartZ => player1StartPosition.z;

    /// <summary>시작 위치에서 실제로 전진한 거리. 설정 오류로 시작하지 못했으면 0이다.</summary>
    public float TravelledDistance => lanes == null ? 0f : player1.position.z - player1StartPosition.z;

    private void Awake()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogError("두 플레이어를 설정해야 합니다.", this);
            enabled = false;
            return;
        }

        int player1StartLane = LaneFromX(player1.position.x);
        int player2StartLane = LaneFromX(player2.position.x);
        if (player1StartLane == player2StartLane)
        {
            Debug.LogError("두 플레이어는 서로 다른 레일에서 시작해야 합니다.", this);
            enabled = false;
            return;
        }

        lanes = new CoopLanes(player1StartLane, player2StartLane);
        AlignPlayers();
        player1StartPosition = player1.position;
        player2StartPosition = player2.position;
    }

    /// <summary>
    /// 두 플레이어 위치와 합체 상태·분리 기록·쿨타임을 스테이지 시작 상태로 되돌린다 (재시도).
    /// </summary>
    public void ResetToStart()
    {
        if (lanes == null) return;

        player1.position = player1StartPosition;
        player2.position = player2StartPosition;
        lanes.ResetToStart();
    }

    private void Update()
    {
        // Awake에서 설정 오류로 꺼진 뒤 Inspector에서 다시 켜진 경우
        if (lanes == null) return;

        Vector3 forward = Vector3.forward * (forwardSpeed * Time.deltaTime);
        player1.position += forward;
        player2.position += forward;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 한 기기에서는 누가 분리 키를 눌렀는지 구분할 수 없어 두 플레이어의 분리 입력으로 함께 넘긴다.
        // 분리한 플레이어가 필요한 암살 기록(S-601)은 플레이어별 입력이 생기는 네트워크 단계에서 다룬다.
        bool split = keyboard[splitKey].wasPressedThisFrame;
        lanes.Step(
            Time.time,
            ReadMove(keyboard, player1LeftKey, player1RightKey),
            ReadMove(keyboard, player2LeftKey, player2RightKey),
            split,
            split,
            moveCooldownSeconds);

        AlignPlayers();
    }

    private static int ReadMove(Keyboard keyboard, Key leftKey, Key rightKey)
    {
        bool left = keyboard[leftKey].wasPressedThisFrame;
        bool right = keyboard[rightKey].wasPressedThisFrame;
        if (left == right) return 0;
        return left ? -1 : 1;
    }

    private int LaneFromX(float x)
    {
        // SetLaneX와 같은 변환(ObstacleCycle.GetLaneX)을 거꾸로 적용하고, 레일 밖 위치는 가장 가까운 끝 레일로 본다.
        float leftLaneX = ObstacleCycle.GetLaneX(CoopLanes.LEFT_LANE, centerLaneX, laneSpacing);
        int lane = Mathf.RoundToInt((x - leftLaneX) / laneSpacing) + CoopLanes.LEFT_LANE;
        return Mathf.Clamp(lane, CoopLanes.LEFT_LANE, CoopLanes.RIGHT_LANE);
    }

    private void AlignPlayers()
    {
        SetLaneX(player1, lanes.Player1Lane);
        SetLaneX(player2, lanes.Player2Lane);
    }

    private void SetLaneX(Transform player, int lane)
    {
        Vector3 position = player.position;
        position.x = ObstacleCycle.GetLaneX(lane, centerLaneX, laneSpacing);
        player.position = position;
    }
}
