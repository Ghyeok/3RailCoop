using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private Transform forwardReference;
    [SerializeField] private float spawnDistance = 12f;
    [SerializeField] private float centerLaneX;
    [SerializeField, Min(0.01f)] private float laneSpacing = 4f;
    [SerializeField, Min(0.01f)] private float cycleIntervalSeconds = 1f;
    [SerializeField] private List<ObstaclePattern> patterns = new List<ObstaclePattern>();

    private readonly List<int> lanesBuffer = new List<int>();
    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();
    private readonly List<ObstaclePattern> attemptPatterns = new List<ObstaclePattern>();
    private bool canSpawn;

    private void Start()
    {
        if (obstaclePrefab == null || forwardReference == null)
        {
            Debug.LogError("장애물 프리팹과 전진 기준 오브젝트를 설정해야 합니다. 장애물 없이 진행합니다.", this);
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
        ResolveAttemptPatterns();
        var wait = new WaitForSeconds(cycleIntervalSeconds);

        foreach (ObstaclePattern pattern in attemptPatterns)
        {
            foreach (int cycle in pattern.Cycles)
            {
                SpawnCycle(cycle);
                yield return wait;
            }
        }
    }

    /// <summary>
    /// 이번 시도에 생성할 패턴을 정한다. 잘못된 패턴은 유효한 패턴으로 무작위 대체하고 오류 로그를 남긴다 (D-009).
    /// </summary>
    private void ResolveAttemptPatterns()
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

        if (!attemptPatterns.Exists(pattern => ObstaclePatternResolver.HasObstacle(pattern.Cycles)))
        {
            Debug.LogError("장애물 패턴 설정 오류: 장애물이 있는 유효한 패턴이 없어 장애물 없이 진행합니다.", this);
        }
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

    private void SpawnCycle(int cycle)
    {
        float spawnZ = forwardReference.position.z + spawnDistance;
        float spawnY = transform.position.y;

        // 시도 시작 때 검증한 뒤 Inspector에서 패턴을 바꾼 경우를 대비한다.
        if (!ObstacleCycle.TryGetLanes(cycle, lanesBuffer))
        {
            Debug.LogError($"유효하지 않은 장애물 사이클이라 생성하지 않습니다: {cycle}", this);
            return;
        }

        foreach (int lane in lanesBuffer)
        {
            float spawnX = ObstacleCycle.GetLaneX(lane, centerLaneX, laneSpacing);
            SpawnObstacle(spawnX, spawnY, spawnZ);
        }
    }

    private void SpawnObstacle(float x, float y, float z)
    {
        spawnedObstacles.Add(Instantiate(obstaclePrefab, new Vector3(x, y, z), obstaclePrefab.transform.rotation));
    }
}
