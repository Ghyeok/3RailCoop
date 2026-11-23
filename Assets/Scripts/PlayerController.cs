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

    private int currentLane;

    private void Awake()
    {
        currentLane = Mathf.Clamp(
            Mathf.RoundToInt((transform.position.x - centerLaneX) / laneSpacing) + CENTER_LANE,
            LEFT_LANE,
            RIGHT_LANE);
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
    /// 입력한 방향으로 이동
    /// </summary>
    /// <param name="direction"></param>
    private void MoveLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, LEFT_LANE, RIGHT_LANE);
        AlignToLane();
    }

    /// <summary>
    /// 이동한 Lane의 중앙 좌표에 정렬시킴
    /// </summary>
    private void AlignToLane()
    {
        Vector3 position = transform.position;
        position.x = centerLaneX + (currentLane - CENTER_LANE) * laneSpacing;
        transform.position = position;
    }
}
