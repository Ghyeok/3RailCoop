using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스테이지 진행·게임 오버·재시도·클리어 흐름을 연결한다 (MVP, 대기방 없음).
/// 상태 전이 규칙은 <see cref="StageRun"/>이 맡고, 이 컴포넌트는 씬 오브젝트를 멈추거나 시작 상태로 되돌린다.
/// </summary>
public class StageController : MonoBehaviour
{
    private const string GAME_OVER_MESSAGE = "GAME OVER\nPress R to retry";
    private const string CLEAR_MESSAGE = "CLEAR!\nPress R to play again";

    [SerializeField] private CoopController coopController;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private QuarterViewCameraController cameraController;
    [SerializeField] private PlayerHitbox[] playerHitboxes = new PlayerHitbox[0];
    [SerializeField] private TMP_Text messageText;

    [Tooltip("고정 순서 모드에서 이 거리만큼 전진하면 클리어한다. 무작위 모드는 스포너가 정한 클리어 지점(마지막 장애물 직후)을 쓴다. 마지막 장애물 Z(대략 전체 사이클 수 × 사이클 간격 × 전진 속도 + 생성 거리, 프레임 지연만큼 더 뒤)보다 커야 하므로 패턴·간격·속도를 바꾸면 다시 맞춘다.")]
    [SerializeField, Min(0.01f)] private float targetDistance = 100f;
    [Tooltip("무작위 모드에서 클리어 지점을 마지막 장애물보다 이만큼 뒤에 둔다.")]
    [SerializeField, Min(0f)] private float clearMarginAfterLastObstacle = 2f;
    [SerializeField] private Key retryKey = Key.R;

    private StageRun run;

    public StageState State => run.State;

    private void Awake()
    {
        run = new StageRun(targetDistance);

        if (coopController == null)
        {
            Debug.LogError("CoopController를 설정해야 합니다.", this);
            enabled = false;
            return;
        }

        if (playerHitboxes.Length == 0)
            Debug.LogError("플레이어 히트박스가 없어 충돌 게임 오버가 일어나지 않습니다.", this);

        UpdateMessage();
    }

    private void OnEnable()
    {
        foreach (PlayerHitbox hitbox in playerHitboxes)
        {
            if (hitbox != null) hitbox.HitObstacle += OnHitObstacle;
        }
    }

    private void OnDisable()
    {
        foreach (PlayerHitbox hitbox in playerHitboxes)
        {
            if (hitbox != null) hitbox.HitObstacle -= OnHitObstacle;
        }
    }

    private void Update()
    {
        if (run.IsPlaying)
        {
            run.SetTargetDistance(GetTargetDistance());

            // 플레이어가 실제로 전진한 거리를 기준으로 진행한다.
            run.Advance(coopController.TravelledDistance - run.Distance);
            if (!run.IsPlaying) EndRun();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[retryKey].wasPressedThisFrame) Retry();
    }

    /// <summary>
    /// 게임 오버나 클리어 뒤 스테이지 처음부터 다시 시작한다. 진행 중이면 무시한다.
    /// </summary>
    public void Retry()
    {
        if (!enabled || !run.Restart()) return;

        // 스포너가 첫 사이클을 기준 오브젝트 위치에서 바로 생성하므로, 위치를 모두 되돌린 뒤 생성을 다시 시작한다.
        coopController.ResetToStart();
        coopController.enabled = true;
        if (cameraController != null) cameraController.ResetToStart();
        if (obstacleSpawner != null) obstacleSpawner.Restart();
        UpdateMessage();
    }

    /// <summary>
    /// 고정 순서 모드는 Inspector 목표 거리, 무작위 모드는 마지막 장애물 직후를 클리어 지점으로 쓴다 (D-012).
    /// 무작위 모드 스포너가 비활성이거나 장애물을 하나도 생성하지 않았으면(설정 오류) Inspector 목표 거리를 쓴다.
    /// </summary>
    private float GetTargetDistance()
    {
        if (obstacleSpawner == null || !obstacleSpawner.isActiveAndEnabled || !obstacleSpawner.UsesAutoClearDistance) return targetDistance;
        if (!obstacleSpawner.HasFinishedSpawning) return float.PositiveInfinity;

        return obstacleSpawner.LastObstacleZ is float lastObstacleZ
            ? StagePatternPlanner.GetClearDistance(coopController.StartZ, lastObstacleZ, clearMarginAfterLastObstacle)
            : targetDistance;
    }

    private void OnHitObstacle()
    {
        if (run.ReportCollision()) EndRun();
    }

    private void EndRun()
    {
        // 진행을 멈추고 이동·분리 입력을 받지 않는다. 카메라는 CoopController가 꺼지면 멈춘다.
        coopController.enabled = false;
        if (obstacleSpawner != null) obstacleSpawner.StopSpawning();
        UpdateMessage();
    }

    private void UpdateMessage()
    {
        if (messageText == null) return;

        messageText.gameObject.SetActive(!run.IsPlaying);
        messageText.text = run.State == StageState.GameOver ? GAME_OVER_MESSAGE : CLEAR_MESSAGE;
    }
}
