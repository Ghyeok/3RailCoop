using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private const int LEFT_LANE = 0;
    private const int RIGHT_LANE = 2;
    private const int CENTER_LANE = 1;

    [SerializeField] private Key moveLeftKey = Key.A;
    [SerializeField] private Key moveRightKey = Key.D;
    [SerializeField] private float centerLaneX;
    [SerializeField, Min(0.01f)] private float laneSpacing = 4f;
    [SerializeField, Min(0f)] private float forwardSpeed = 5f;
    [Tooltip("레일이 바뀐 뒤 다음 이동을 받기까지의 시간(초). 최종값은 플레이 테스트로 정한다 (O-01).")]
    [SerializeField, Min(0f)] private float moveCooldownSeconds = 0.2f;

    private LaneMovement laneMovement;

    private void Awake()
    {
        int startLane = Mathf.RoundToInt((transform.position.x - centerLaneX) / laneSpacing) + CENTER_LANE;
        laneMovement = new LaneMovement(startLane, LEFT_LANE, RIGHT_LANE);
        AlignToLane();
    }

    private void Update()
    {
        transform.position += Vector3.forward * (forwardSpeed * Time.deltaTime);

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool moveLeft = keyboard[moveLeftKey].wasPressedThisFrame;
        bool moveRight = keyboard[moveRightKey].wasPressedThisFrame;
        if (moveLeft == moveRight) return;

        MoveLane(moveLeft ? -1 : 1);
    }

    /// <summary>
    /// 입력한 방향으로 이동. 쿨타임 중이거나 끝 레일 바깥쪽이면 무시한다.
    /// </summary>
    /// <param name="direction"></param>
    private void MoveLane(int direction)
    {
        if (laneMovement.TryMove(direction, Time.time, moveCooldownSeconds))
            AlignToLane();
    }

    /// <summary>
    /// 이동한 Lane의 중앙 좌표에 정렬시킴
    /// </summary>
    private void AlignToLane()
    {
        Vector3 position = transform.position;
        position.x = centerLaneX + (laneMovement.CurrentLane - CENTER_LANE) * laneSpacing;
        transform.position = position;
    }
}
