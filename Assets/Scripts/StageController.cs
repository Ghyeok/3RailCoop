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

    [Tooltip("이 거리만큼 전진하면 클리어한다. 테스트 스테이지 값은 S-304에서 정한다 (O-06).")]
    [SerializeField, Min(0.01f)] private float targetDistance = 100f;
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
