using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 스포너가 패턴의 사이클을 순서대로 생성하고, 유효하지 않은 패턴을 거부하는지 확인한다 (S-301).
/// </summary>
public class ObstacleSpawnerTests
{
    private const BindingFlags FIELD_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
    private const string OBSTACLE_NAME = "TestObstacle";
    private const float SPAWN_DISTANCE = 12f;
    private const float CENTER_LANE_X = 0f;
    private const float LANE_SPACING = 4f;

    private readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject spawned in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(o => o.name.StartsWith(OBSTACLE_NAME)))
        {
            Object.Destroy(spawned);
        }

        foreach (Object o in created)
        {
            if (o != null) Object.Destroy(o);
        }

        created.Clear();
    }

    [UnityTest]
    public IEnumerator Spawner_SpawnsCyclesInOrderAtSameZ()
    {
        ObstacleSpawner spawner = CreateSpawner(new List<int> { 13, 0, 2 }, 0.1f);
        spawner.gameObject.SetActive(true);

        // 첫 사이클(13)은 시작 직후 생성된다.
        yield return null;
        List<Vector3> first = SpawnedPositions();
        Assert.AreEqual(2, first.Count);
        CollectionAssert.AreEquivalent(new[] { CENTER_LANE_X - LANE_SPACING, CENTER_LANE_X + LANE_SPACING }, first.Select(p => p.x));
        Assert.That(first.Select(p => p.z).Distinct().Count(), Is.EqualTo(1), "같은 사이클은 같은 Z");
        Assert.AreEqual(SPAWN_DISTANCE, first[0].z, 0.001f);

        // 빈 사이클(0) 뒤 2번 레일 사이클이 생성된다.
        yield return new WaitForSeconds(0.35f);
        List<Vector3> all = SpawnedPositions();
        Assert.AreEqual(3, all.Count);
        Assert.AreEqual(1, all.Count(p => Mathf.Approximately(p.x, CENTER_LANE_X)));
    }

    [UnityTest]
    public IEnumerator Spawner_RejectsFullBlockPattern()
    {
        ObstacleSpawner spawner = CreateSpawner(new List<int> { 2, 123 }, 0.1f);
        LogAssert.Expect(LogType.Error, new Regex("유효하지 않은 장애물 사이클: 123"));
        spawner.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.25f);

        Assert.AreEqual(0, SpawnedPositions().Count);
    }

    private ObstacleSpawner CreateSpawner(List<int> cycles, float interval)
    {
        var prefab = new GameObject(OBSTACLE_NAME);
        var reference = new GameObject("TestForwardReference");
        var pattern = ScriptableObject.CreateInstance<ObstaclePattern>();
        typeof(ObstaclePattern).GetField("cycles", FIELD_FLAGS).SetValue(pattern, cycles);

        var spawnerObject = new GameObject("TestSpawner");
        spawnerObject.SetActive(false);
        ObstacleSpawner spawner = spawnerObject.AddComponent<ObstacleSpawner>();
        SetField(spawner, "obstaclePrefab", prefab);
        SetField(spawner, "forwardReference", reference.transform);
        SetField(spawner, "cycleIntervalSeconds", interval);
        SetField(spawner, "spawnDistance", SPAWN_DISTANCE);
        SetField(spawner, "centerLaneX", CENTER_LANE_X);
        SetField(spawner, "laneSpacing", LANE_SPACING);
        SetField(spawner, "patterns", new List<ObstaclePattern> { pattern });

        created.AddRange(new Object[] { prefab, reference, pattern, spawnerObject });
        return spawner;
    }

    private static void SetField(object target, string name, object value)
    {
        typeof(ObstacleSpawner).GetField(name, FIELD_FLAGS).SetValue(target, value);
    }

    private static List<Vector3> SpawnedPositions()
    {
        return Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(o => o.name == OBSTACLE_NAME + "(Clone)")
            .Select(o => o.transform.position)
            .ToList();
    }
}
