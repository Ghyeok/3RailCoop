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
                if (!IsValidCycle(cycle))
                {
                    Debug.LogError($"유효하지 않은 장애물 사이클: {cycle} ({pattern.name})", pattern);
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsValidCycle(int cycle)
    {
        // 음수 제외
        if (cycle < 0) return false;

        HashSet<int> usedLanes = new HashSet<int>();

        // 각 Lane 번호를 차례대로 꺼냄
        while (cycle > 0)
        {
            int lane = cycle % 10;

            // 1 ~ 3번 Lane만 허용
            if (lane < 1 || lane > 3)
                return false;

            // 이미 들어 있는 Lane이라면 중복
            if (!usedLanes.Add(lane))
                return false;

            cycle /= 10;
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

        while (cycle > 0)
        {
            int lane = cycle % 10;
            float spawnX = centerLaneX + (lane - 2) * laneSpacing;
            SpawnObstacle(spawnX, spawnY, spawnZ);
            cycle /= 10;
        }
    }

    private void SpawnObstacle(float x, float y, float z)
    {
        Instantiate(obstaclePrefab, new Vector3(x, y, z), obstaclePrefab.transform.rotation);
    }
}
