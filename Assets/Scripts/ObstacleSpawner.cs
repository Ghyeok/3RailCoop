using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class ObstacleSpawner : MonoBehaviour
{
    public enum PatternOrder
    {
        /// <summary>패턴 목록을 적힌 순서대로 생성한다 (D-008 테스트 스테이지).</summary>
        Fixed,

        /// <summary>후보 목록에서 난이도 비율대로 무작위로 골라 생성한다 (D-012).</summary>
        Random
    }

    [Serializable]
    public class DifficultyShare
    {
        [Min(1)] public int difficulty = 1;
        [Min(0f)] public float percent = 100f;
    }

    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private Transform forwardReference;
    [SerializeField] private float spawnDistance = 12f;
    [SerializeField] private float centerLaneX;
    [SerializeField, Min(0.01f)] private float laneSpacing = 4f;
    [SerializeField, Min(0.01f)] private float cycleIntervalSeconds = 1f;
    [SerializeField] private PatternOrder patternOrder = PatternOrder.Fixed;

    [Header("고정 순서")]
    [SerializeField] private List<ObstaclePattern> patterns = new List<ObstaclePattern>();

    [Header("무작위 (D-012)")]
    [SerializeField] private List<ObstaclePattern> randomPatternPool = new List<ObstaclePattern>();
    [SerializeField, Min(1)] private int randomPatternCount = 6;
    [SerializeField] private List<DifficultyShare> difficultyShares = new List<DifficultyShare>();
    [Tooltip("시작과 패턴 사이에 넣는 빈 사이클 수.")]
    [SerializeField, Min(0)] private int emptyCyclesBetweenPatterns = 3;

    private readonly List<int> lanesBuffer = new List<int>();
    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();
    private readonly List<ObstaclePattern> attemptPatterns = new List<ObstaclePattern>();
    private readonly List<int> attemptCycles = new List<int>();
    private bool canSpawn;

    /// <summary>무작위 모드는 스테이지 길이가 시도마다 달라 클리어 지점을 스포너가 정한다.</summary>
    public bool UsesAutoClearDistance => patternOrder == PatternOrder.Random;

    /// <summary>이번 시도의 사이클을 모두 생성했는지. 생성할 수 없는 설정이면 처음부터 true다.</summary>
    public bool HasFinishedSpawning { get; private set; }

    /// <summary>이번 시도에서 마지막으로 장애물을 생성한 월드 Z. 아직 없거나 장애물이 하나도 없으면 null이다.</summary>
    public float? LastObstacleZ { get; private set; }

    private void Start()
    {
        if (obstaclePrefab == null || forwardReference == null)
        {
            Debug.LogError("장애물 프리팹과 전진 기준 오브젝트를 설정해야 합니다. 장애물 없이 진행합니다.", this);
            HasFinishedSpawning = true;
            return;
        }

        canSpawn = true;
        StartCoroutine(SpawnPatterns());
    }

    /// <summary>
    /// 남은 사이클 생성을 멈춘다 (게임 오버·클리어).
    /// </summary>
    public void StopSpawning()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// 생성한 장애물을 모두 지우고 첫 패턴부터 다시 생성한다 (재시도).
    /// </summary>
    public void Restart()
    {
        StopAllCoroutines();

        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null) Destroy(obstacle);
        }

        spawnedObstacles.Clear();

        if (canSpawn) StartCoroutine(SpawnPatterns());
    }

    private IEnumerator SpawnPatterns()
    {
        HasFinishedSpawning = false;
        LastObstacleZ = null;
        if (patternOrder == PatternOrder.Random) PlanRandomAttemptCycles();
        else ResolveFixedAttemptCycles();

        if (!attemptCycles.Exists(cycle => cycle != 0))
        {
            Debug.LogError("장애물 패턴 설정 오류: 장애물이 있는 유효한 패턴이 없어 장애물 없이 진행합니다.", this);
        }

        var wait = new WaitForSeconds(cycleIntervalSeconds);
        for (int i = 0; i < attemptCycles.Count; i++)
        {
            if (i > 0) yield return wait;
            if (SpawnCycle(attemptCycles[i], out float spawnZ)) LastObstacleZ = spawnZ;
        }

        HasFinishedSpawning = true;
    }

    /// <summary>
    /// 이번 시도에 생성할 패턴을 정한다. 잘못된 패턴은 유효한 패턴으로 무작위 대체하고 오류 로그를 남긴다 (D-009).
    /// </summary>
    private void ResolveFixedAttemptCycles()
    {
        var cycles = new List<IReadOnlyList<int>>(patterns.Count);
        foreach (ObstaclePattern pattern in patterns)
        {
            cycles.Add(pattern != null ? pattern.Cycles : null);
        }

        int[] resolved = ObstaclePatternResolver.Resolve(cycles, count => Random.Range(0, count));

        attemptPatterns.Clear();
        for (int i = 0; i < resolved.Length; i++)
        {
            if (resolved[i] != i) LogPatternError(i, resolved[i]);
            if (resolved[i] != ObstaclePatternResolver.NO_REPLACEMENT) attemptPatterns.Add(patterns[resolved[i]]);
        }

        attemptCycles.Clear();
        foreach (ObstaclePattern pattern in attemptPatterns) attemptCycles.AddRange(pattern.Cycles);
    }

    /// <summary>
    /// 후보 목록에서 난이도 비율대로 패턴을 골라 섞고, 시작과 패턴 사이에 빈 사이클을 넣는다 (D-012, D-013).
    /// </summary>
    private void PlanRandomAttemptCycles()
    {
        var shares = new List<(int Difficulty, float Percent)>(difficultyShares.Count);
        foreach (DifficultyShare share in difficultyShares) shares.Add((share.difficulty, share.percent));

        var pool = new List<(IReadOnlyList<int> Cycles, int Difficulty)>(randomPatternPool.Count);
        foreach (ObstaclePattern pattern in randomPatternPool)
        {
            pool.Add(pattern != null ? (pattern.Cycles, pattern.Difficulty) : (null, 0));
        }

        StagePatternPlanner.Plan plan = StagePatternPlanner.Create(randomPatternCount, shares, pool, count => Random.Range(0, count));

        foreach (int index in plan.UnusablePoolIndices) LogUnusablePoolPattern(index);
        if (plan.NoAllowedDifficulty)
        {
            Debug.LogError("난이도 비율 설정 오류: 비율이 0보다 큰 난이도가 없습니다.", this);
        }

        if (plan.PercentSumAdjusted)
        {
            Debug.LogError($"난이도 비율 설정 오류: 비율 합이 {plan.PercentSum:0.##}%입니다. 비율대로 다시 계산해 진행합니다.", this);
        }

        string missingResult = plan.PatternIndices.Count > 0 ? "그 개수를 남은 난이도에 비율대로 나눕니다" : "나눌 난이도가 남지 않았습니다";
        foreach (int difficulty in plan.MissingDifficulties)
        {
            Debug.LogError($"난이도 비율 설정 오류: 난이도 {difficulty}에 유효한 패턴이 없습니다. {missingResult}.", this);
        }

        attemptCycles.Clear();
        foreach (int index in plan.PatternIndices)
        {
            for (int i = 0; i < emptyCyclesBetweenPatterns; i++) attemptCycles.Add(0);
            attemptCycles.AddRange(randomPatternPool[index].Cycles);
        }
    }

    private void LogUnusablePoolPattern(int index)
    {
        ObstaclePattern pattern = randomPatternPool[index];
        string reason;
        if (pattern == null) reason = "빈 참조";
        else if (!ObstaclePatternResolver.IsValid(pattern.Cycles)) reason = $"{pattern.name}, 유효하지 않은 사이클 {FindInvalidCycle(pattern)}";
        else reason = $"{pattern.name}, 장애물 없음";

        Object context = pattern != null ? (Object)pattern : this;
        Debug.LogError($"장애물 패턴 설정 오류: 무작위 후보 {index + 1}번째 패턴({reason}). 후보에서 제외합니다.", context);
    }

    private void LogPatternError(int index, int replacement)
    {
        ObstaclePattern pattern = patterns[index];
        string reason = pattern == null ? "빈 참조" : $"{pattern.name}, 유효하지 않은 사이클 {FindInvalidCycle(pattern)}";
        string result = replacement == ObstaclePatternResolver.NO_REPLACEMENT
            ? "대체할 패턴이 없어 건너뜁니다"
            : $"{patterns[replacement].name}(으)로 대체합니다";

        Object context = pattern != null ? (Object)pattern : this;
        Debug.LogError($"장애물 패턴 설정 오류: {index + 1}번째 패턴({reason}). {result}.", context);
    }

    private static int FindInvalidCycle(ObstaclePattern pattern)
    {
        foreach (int cycle in pattern.Cycles)
        {
            if (!ObstacleCycle.IsValid(cycle)) return cycle;
        }

        return 0;
    }

    /// <summary>
    /// 사이클의 장애물을 생성한다. 장애물을 하나 이상 생성했으면 true와 생성한 Z를 돌려준다.
    /// </summary>
    private bool SpawnCycle(int cycle, out float spawnZ)
    {
        spawnZ = forwardReference.position.z + spawnDistance;
        float spawnY = transform.position.y;

        // 사이클은 시도 시작 때 검증해 복사한 값이다. 플레이 중 Inspector에서 패턴을 바꾸면 다음 시도부터 반영된다.
        ObstacleCycle.TryGetLanes(cycle, lanesBuffer);
        foreach (int lane in lanesBuffer)
        {
            float spawnX = ObstacleCycle.GetLaneX(lane, centerLaneX, laneSpacing);
            SpawnObstacle(spawnX, spawnY, spawnZ);
        }

        return lanesBuffer.Count > 0;
    }

    private void SpawnObstacle(float x, float y, float z)
    {
        spawnedObstacles.Add(Instantiate(obstaclePrefab, new Vector3(x, y, z), obstaclePrefab.transform.rotation));
    }
}
