using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 스포너가 패턴의 사이클을 순서대로 생성하고 (S-301), 잘못된 패턴을 유효한 패턴으로 대체하며 (S-303, D-009),
/// 무작위 모드에서 난이도 비율대로 골라 생성하는지 확인한다 (S-502, D-012, D-013).
/// </summary>
public class ObstacleSpawnerTests
{
    private const BindingFlags FIELD_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
    private const string OBSTACLE_NAME = "TestObstacle";
    private const float SPAWN_DISTANCE = 12f;
    private const float CENTER_LANE_X = 0f;
    private const float LANE_SPACING = 4f;

    private readonly List<Object> created = new List<Object>();
    private Random.State randomState;

    [SetUp]
    public void SetUp()
    {
        randomState = Random.state;
    }

    [TearDown]
    public void TearDown()
    {
        // Random.InitState를 쓴 테스트가 뒤 테스트의 난수 순서에 영향을 주지 않게 되돌린다.
        Random.state = randomState;

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
    public IEnumerator Spawner_ReplacesNullPatternWithValidPattern()
    {
        ObstacleSpawner spawner = CreateSpawner(new List<List<int>> { null, new List<int> { 13 } }, 0.1f);
        LogAssert.Expect(LogType.Error, new Regex(@"장애물 패턴 설정 오류: 1번째 패턴\(빈 참조\)\. Pattern1\(으\)로 대체"));
        spawner.gameObject.SetActive(true);

        // 빈 참조 자리에 Pattern1(13)이 생성된다.
        yield return null;
        List<Vector3> first = SpawnedPositions();
        CollectionAssert.AreEquivalent(new[] { CENTER_LANE_X - LANE_SPACING, CENTER_LANE_X + LANE_SPACING }, first.Select(p => p.x));

        // 이어서 원래 Pattern1(13)도 생성된다.
        yield return new WaitForSeconds(0.15f);
        Assert.AreEqual(4, SpawnedPositions().Count);
    }

    [UnityTest]
    public IEnumerator Spawner_ReplacesInvalidCyclePatternWithPatternThatHasObstacles()
    {
        // Pattern0은 전 레일 차단(123)이라 잘못되었고, Pattern1은 빈 패턴이라 후보가 아니다. 후보는 Pattern2(3번 레일)뿐이다.
        ObstacleSpawner spawner = CreateSpawner(new List<List<int>>
        {
            new List<int> { 2, 123 },
            new List<int> { 0 },
            new List<int> { 3 },
        }, 0.1f);
        LogAssert.Expect(LogType.Error, new Regex(@"1번째 패턴\(Pattern0, 유효하지 않은 사이클 123\)\. Pattern2\(으\)로 대체"));
        spawner.gameObject.SetActive(true);

        yield return null;
        List<Vector3> first = SpawnedPositions();
        Assert.AreEqual(1, first.Count);
        Assert.AreEqual(CENTER_LANE_X + LANE_SPACING, first[0].x, 0.001f);
    }

    [UnityTest]
    public IEnumerator Restart_PicksReplacementAgainEachAttempt()
    {
        // 후보가 Pattern1(1번 레일)·Pattern2(3번 레일) 두 개다. 재시도마다 다시 고르면 두 후보가 모두 나온다.
        Random.InitState(303);
        ObstacleSpawner spawner = CreateSpawner(new List<List<int>> { null, new List<int> { 1 }, new List<int> { 3 } }, 10f);
        var firstLaneX = new HashSet<float>();

        LogAssert.Expect(LogType.Error, new Regex(@"1번째 패턴\(빈 참조\)"));
        spawner.gameObject.SetActive(true);
        yield return null;
        firstLaneX.Add(SpawnedPositions().Single().x);

        for (int attempt = 0; attempt < 20; attempt++)
        {
            LogAssert.Expect(LogType.Error, new Regex(@"1번째 패턴\(빈 참조\)"));
            spawner.Restart();
            yield return null;
            firstLaneX.Add(SpawnedPositions().Single().x);
        }

        CollectionAssert.AreEquivalent(new[] { CENTER_LANE_X - LANE_SPACING, CENTER_LANE_X + LANE_SPACING }, firstLaneX);
    }

    [UnityTest]
    public IEnumerator Spawner_NoReplacement_SkipsPatternWithoutStopping()
    {
        // 장애물이 있는 유효한 패턴이 없으면 잘못된 자리는 건너뛰고 나머지(빈 패턴)를 계속 진행한다.
        ObstacleSpawner spawner = CreateSpawner(new List<List<int>> { new List<int> { 2, 123 }, new List<int> { 0, 0 } }, 0.1f);
        LogAssert.Expect(LogType.Error, new Regex(@"1번째 패턴\(Pattern0, 유효하지 않은 사이클 123\)\. 대체할 패턴이 없어 건너뜁니다"));
        LogAssert.Expect(LogType.Error, new Regex(@"장애물이 있는 유효한 패턴이 없어 장애물 없이 진행합니다"));
        spawner.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.25f);

        Assert.AreEqual(0, SpawnedPositions().Count);
        Assert.IsTrue(spawner.isActiveAndEnabled);
    }

    [UnityTest]
    public IEnumerator Spawner_OnlyEmptyPatterns_LogsAndProceedsWithoutObstacles()
    {
        ObstacleSpawner spawner = CreateSpawner(new List<int> { 0, 0 }, 0.1f);
        LogAssert.Expect(LogType.Error, new Regex(@"장애물이 있는 유효한 패턴이 없어 장애물 없이 진행합니다"));
        spawner.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.25f);

        Assert.AreEqual(0, SpawnedPositions().Count);
    }

    [UnityTest]
    public IEnumerator Spawner_MissingPrefab_LogsAndProceedsWithoutObstacles()
    {
        return MissingReference_LogsAndProceedsWithoutObstacles("obstaclePrefab");
    }

    [UnityTest]
    public IEnumerator Spawner_MissingForwardReference_LogsAndProceedsWithoutObstacles()
    {
        return MissingReference_LogsAndProceedsWithoutObstacles("forwardReference");
    }

    private IEnumerator MissingReference_LogsAndProceedsWithoutObstacles(string field)
    {
        ObstacleSpawner spawner = CreateSpawner(new List<int> { 1, 2 }, 0.1f);
        SetField(spawner, field, null);
        LogAssert.Expect(LogType.Error, new Regex(@"장애물 프리팹과 전진 기준 오브젝트를 설정해야 합니다\. 장애물 없이 진행합니다"));
        spawner.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.15f);

        // 재시도해도 예외 없이 장애물 없이 진행한다.
        spawner.Restart();
        yield return null;
        Assert.AreEqual(0, SpawnedPositions().Count);
    }

    [UnityTest]
    public IEnumerator Restart_ClearsSpawnedAndStartsFromFirstCycle()
    {
        ObstacleSpawner spawner = CreateSpawner(new List<int> { 1, 2, 3 }, 0.1f);
        spawner.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.25f);
        Assert.Greater(SpawnedPositions().Count, 1);

        spawner.StopSpawning();
        spawner.Restart();
        yield return null;

        // 이전 장애물은 지워지고 첫 사이클(1번 레일)만 다시 생성된다.
        List<Vector3> positions = SpawnedPositions();
        Assert.AreEqual(1, positions.Count);
        Assert.AreEqual(CENTER_LANE_X - LANE_SPACING, positions[0].x, 0.001f);
    }

    [UnityTest]
    public IEnumerator StopSpawning_StopsRemainingCycles()
    {
        ObstacleSpawner spawner = CreateSpawner(new List<int> { 1, 2, 3 }, 0.1f);
        spawner.gameObject.SetActive(true);
        yield return null;

        spawner.StopSpawning();
        yield return new WaitForSeconds(0.35f);

        Assert.AreEqual(1, SpawnedPositions().Count);
    }

    [UnityTest]
    public IEnumerator Random_SpawnsExactCountPerDifficulty()
    {
        // 난이도 1은 1번 레일, 난이도 2는 3번 레일 패턴이다. 4개 × 50% → 각 2개.
        ObstacleSpawner spawner = CreateRandomSpawner(
            new List<(List<int>, int)> { (new List<int> { 1 }, 1), (new List<int> { 3 }, 2) },
            4, new[] { (1, 50f), (2, 50f) }, 0, 0.02f);
        spawner.gameObject.SetActive(true);

        yield return WaitUntilOrTimeout(() => spawner.HasFinishedSpawning, 2f);

        List<Vector3> positions = SpawnedPositions();
        Assert.AreEqual(2, positions.Count(p => Mathf.Approximately(p.x, CENTER_LANE_X - LANE_SPACING)));
        Assert.AreEqual(2, positions.Count(p => Mathf.Approximately(p.x, CENTER_LANE_X + LANE_SPACING)));
    }

    [UnityTest]
    public IEnumerator Random_InsertsEmptyCyclesBeforeEachPattern_AndReportsLastObstacleZ()
    {
        ObstacleSpawner spawner = CreateRandomSpawner(
            new List<(List<int>, int)> { (new List<int> { 2 }, 1) }, 1, new[] { (1, 100f) }, 2, 0.1f);
        spawner.gameObject.SetActive(true);

        // 빈 사이클 2개 동안은 장애물이 없고 생성도 끝나지 않는다.
        yield return null;
        Assert.AreEqual(0, SpawnedPositions().Count);
        Assert.IsFalse(spawner.HasFinishedSpawning);
        Assert.IsNull(spawner.LastObstacleZ);

        yield return WaitUntilOrTimeout(() => spawner.HasFinishedSpawning, 1f);
        Assert.AreEqual(1, SpawnedPositions().Count);

        // 전진 기준이 움직이지 않으므로 마지막 장애물은 생성 거리에 있다.
        Assert.AreEqual(SPAWN_DISTANCE, spawner.LastObstacleZ.Value, 0.001f);
    }

    [UnityTest]
    public IEnumerator Random_RestartPicksAgain()
    {
        Random.InitState(502);
        ObstacleSpawner spawner = CreateRandomSpawner(
            new List<(List<int>, int)> { (new List<int> { 1 }, 1), (new List<int> { 3 }, 1) }, 1, new[] { (1, 100f) }, 0, 10f);
        var firstLaneX = new HashSet<float>();

        spawner.gameObject.SetActive(true);
        yield return null;
        firstLaneX.Add(SpawnedPositions().Single().x);

        for (int attempt = 0; attempt < 20; attempt++)
        {
            spawner.Restart();
            yield return null;
            firstLaneX.Add(SpawnedPositions().Single().x);
        }

        CollectionAssert.AreEquivalent(new[] { CENTER_LANE_X - LANE_SPACING, CENTER_LANE_X + LANE_SPACING }, firstLaneX);
    }

    [UnityTest]
    public IEnumerator Random_ConfigErrors_LoggedAndRedistributed()
    {
        // 비율 합 120%, 난이도 2에는 잘못된 패턴만 있다 → 오류 로그 후 4개 모두 난이도 1(2번 레일).
        ObstacleSpawner spawner = CreateRandomSpawner(
            new List<(List<int>, int)> { (new List<int> { 2 }, 1), (new List<int> { 2, 123 }, 2) },
            4, new[] { (1, 60f), (2, 60f) }, 0, 0.02f);
        LogAssert.Expect(LogType.Error, new Regex(@"무작위 후보 2번째 패턴\(Pool1, 유효하지 않은 사이클 123\)\. 후보에서 제외합니다"));
        LogAssert.Expect(LogType.Error, new Regex(@"난이도 비율 설정 오류: 비율 합이 120%입니다"));
        LogAssert.Expect(LogType.Error, new Regex(@"난이도 2에 유효한 패턴이 없습니다\. 그 개수를 남은 난이도에 비율대로 나눕니다"));
        spawner.gameObject.SetActive(true);

        yield return WaitUntilOrTimeout(() => spawner.HasFinishedSpawning, 2f);

        List<Vector3> positions = SpawnedPositions();
        Assert.AreEqual(4, positions.Count);
        Assert.That(positions.Select(p => p.x), Is.All.EqualTo(CENTER_LANE_X));
    }

    [UnityTest]
    public IEnumerator Random_NoUsablePattern_LogsAndProceedsWithoutObstacles()
    {
        ObstacleSpawner spawner = CreateRandomSpawner(
            new List<(List<int>, int)> { (new List<int> { 0 }, 1) }, 3, new[] { (1, 100f) }, 1, 0.02f);
        LogAssert.Expect(LogType.Error, new Regex(@"무작위 후보 1번째 패턴\(Pool0, 장애물 없음\)"));
        LogAssert.Expect(LogType.Error, new Regex(@"난이도 1에 유효한 패턴이 없습니다\. 나눌 난이도가 남지 않았습니다"));
        LogAssert.Expect(LogType.Error, new Regex(@"장애물이 있는 유효한 패턴이 없어 장애물 없이 진행합니다"));
        spawner.gameObject.SetActive(true);

        yield return WaitUntilOrTimeout(() => spawner.HasFinishedSpawning, 1f);

        Assert.AreEqual(0, SpawnedPositions().Count);
        Assert.IsTrue(spawner.HasFinishedSpawning);
        Assert.IsNull(spawner.LastObstacleZ, "장애물이 없으면 마지막 장애물 Z가 없다");
    }

    [Test]
    public void FixedOrder_IsDefault_AndDoesNotUseAutoClearDistance()
    {
        ObstacleSpawner spawner = CreateSpawner(new List<int> { 1 }, 0.1f);

        Assert.IsFalse(spawner.UsesAutoClearDistance);
    }

    private ObstacleSpawner CreateRandomSpawner(
        List<(List<int> Cycles, int Difficulty)> pool, int count, (int Difficulty, float Percent)[] shares, int emptyCycles, float interval)
    {
        ObstacleSpawner spawner = CreateSpawner(new List<List<int>>(), interval);
        var poolPatterns = new List<ObstaclePattern>();
        for (int i = 0; i < pool.Count; i++)
        {
            ObstaclePattern pattern = CreatePattern($"Pool{i}", pool[i].Cycles);
            typeof(ObstaclePattern).GetField("difficulty", FIELD_FLAGS).SetValue(pattern, pool[i].Difficulty);
            poolPatterns.Add(pattern);
        }

        SetField(spawner, "patternOrder", ObstacleSpawner.PatternOrder.Random);
        SetField(spawner, "randomPatternPool", poolPatterns);
        SetField(spawner, "randomPatternCount", count);
        SetField(spawner, "difficultyShares", shares
            .Select(s => new ObstacleSpawner.DifficultyShare { difficulty = s.Difficulty, percent = s.Percent })
            .ToList());
        SetField(spawner, "emptyCyclesBetweenPatterns", emptyCycles);
        return spawner;
    }

    private ObstacleSpawner CreateSpawner(List<int> cycles, float interval)
    {
        return CreateSpawner(new List<List<int>> { cycles }, interval);
    }

    /// <summary>
    /// 패턴 목록으로 스포너를 만든다. null 항목은 빈 참조이고, 패턴 이름은 Pattern{인덱스}다.
    /// </summary>
    private ObstacleSpawner CreateSpawner(List<List<int>> patternCycles, float interval)
    {
        var prefab = new GameObject(OBSTACLE_NAME);
        var reference = new GameObject("TestForwardReference");
        var patterns = new List<ObstaclePattern>();
        for (int i = 0; i < patternCycles.Count; i++)
        {
            if (patternCycles[i] == null)
            {
                patterns.Add(null);
                continue;
            }

            patterns.Add(CreatePattern($"Pattern{i}", patternCycles[i]));
        }

        var spawnerObject = new GameObject("TestSpawner");
        spawnerObject.SetActive(false);
        ObstacleSpawner spawner = spawnerObject.AddComponent<ObstacleSpawner>();
        SetField(spawner, "obstaclePrefab", prefab);
        SetField(spawner, "forwardReference", reference.transform);
        SetField(spawner, "cycleIntervalSeconds", interval);
        SetField(spawner, "spawnDistance", SPAWN_DISTANCE);
        SetField(spawner, "centerLaneX", CENTER_LANE_X);
        SetField(spawner, "laneSpacing", LANE_SPACING);
        SetField(spawner, "patterns", patterns);

        created.AddRange(new Object[] { prefab, reference, spawnerObject });
        return spawner;
    }

    private ObstaclePattern CreatePattern(string name, List<int> cycles)
    {
        var pattern = ScriptableObject.CreateInstance<ObstaclePattern>();
        pattern.name = name;
        typeof(ObstaclePattern).GetField("cycles", FIELD_FLAGS).SetValue(pattern, cycles);
        created.Add(pattern);
        return pattern;
    }

    private static IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float timeoutSeconds)
    {
        float end = Time.time + timeoutSeconds;
        while (!condition() && Time.time < end) yield return null;
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
