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

    private void Start()
    {
        if (obstaclePrefab == null || forwardReference == null)
        {
            Debug.LogError("장애물 프리팹과 전진 기준 오브젝트를 설정해야 합니다.", this);
            return;
        }

        if (!ValidatePatterns()) return;

        StartCoroutine(SpawnPatterns());
    }

    private bool ValidatePatterns()
    {
        foreach (ObstaclePattern pattern in patterns)
        {
            if (pattern == null)
            {
                Debug.LogError("장애물 패턴이 설정되지 않았습니다.", this);
                return false;
            }

            foreach (int cycle in pattern.Cycles)
            {
                if (!ObstacleCycle.IsValid(cycle))
                {
                    Debug.LogError($"유효하지 않은 장애물 사이클: {cycle} ({pattern.name})", pattern);
                    return false;
                }
            }
        }

        return true;
    }

    private IEnumerator SpawnPatterns()
    {
        var wait = new WaitForSeconds(cycleIntervalSeconds);

        foreach (ObstaclePattern pattern in patterns)
        {
            foreach (int cycle in pattern.Cycles)
            {
                SpawnCycle(cycle);
                yield return wait;
            }
        }
    }

    private void SpawnCycle(int cycle)
    {
        float spawnZ = forwardReference.position.z + spawnDistance;
        float spawnY = transform.position.y;

        // 시작 시 검증한 뒤 Inspector에서 패턴을 바꾼 경우를 대비한다.
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
        Instantiate(obstaclePrefab, new Vector3(x, y, z), obstaclePrefab.transform.rotation);
    }
}
