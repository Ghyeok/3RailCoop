using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 충돌 → 게임 오버 → 재시도, 목표 거리 클리어의 씬 통합 (S-302), 무작위 모드의 자동 클리어 지점 (S-502).
/// </summary>
public class StageControllerTests
{
    private const BindingFlags FIELD_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
    private const float CLEAR_MARGIN = 1f;

    private readonly List<Object> created = new List<Object>();
    private Transform player1;
    private Transform player2;
    private CoopController coop;

    [TearDown]
    public void TearDown()
    {
        // 스포너가 만든 장애물은 created에 없으므로, 테스트가 중간에 실패해도 다음 테스트에 남지 않게 지운다.
        foreach (Obstacle obstacle in Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
            Object.Destroy(obstacle.gameObject);

        foreach (Object o in created)
        {
            if (o != null) Object.Destroy(o);
        }

        created.Clear();
    }

    [UnityTest]
    public IEnumerator Collision_GameOver_ThenRetryResetsToStart()
    {
        // Player1(1번 레일, x=-4) 앞에 장애물을 둔다.
        CreateObstacle(new Vector3(-4f, 0f, 1.5f));
        StageController stage = CreateStage(1000f);

        yield return WaitUntilOrTimeout(() => stage.State != StageState.Playing, 2f);
        Assert.AreEqual(StageState.GameOver, stage.State, "한 명이 부딪히면 공동 게임 오버");
        Assert.IsFalse(coop.enabled, "게임 오버 중 이동·분리 입력과 전진을 멈춘다");

        float stoppedZ = player2.position.z;
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(stoppedZ, player2.position.z, "진행이 멈춘다");

        stage.Retry();

        Assert.AreEqual(StageState.Playing, stage.State);
        Assert.IsTrue(coop.enabled);
        Assert.AreEqual(new Vector3(-4f, 0f, 0f), player1.position);
        Assert.AreEqual(new Vector3(4f, 0f, 0f), player2.position);
    }

    [UnityTest]
    public IEnumerator Retry_ResetsMergeState()
    {
        StageController stage = CreateStage(1000f);
        yield return null;

        // 1번·3번에서 중앙으로 들어와 합체한 상태를 만든다.
        CoopLanes lanes = GetLanes();
        lanes.Step(Time.time, 1, -1, false, false, 0f);
        Assert.IsTrue(lanes.IsMerged);

        // 게임 오버로 만든 뒤 재시도한다.
        typeof(StageController).GetMethod("OnHitObstacle", FIELD_FLAGS).Invoke(stage, null);
        stage.Retry();

        CoopLanes reset = GetLanes();
        Assert.IsFalse(reset.IsMerged);
        Assert.AreEqual(CoopLanes.LEFT_LANE, reset.Player1Lane);
        Assert.AreEqual(CoopLanes.RIGHT_LANE, reset.Player2Lane);
    }

    [UnityTest]
    public IEnumerator ReachingTargetDistance_Clears_ThenRetryRestarts()
    {
        StageController stage = CreateStage(1f);

        yield return WaitUntilOrTimeout(() => stage.State != StageState.Playing, 2f);
        Assert.AreEqual(StageState.Cleared, stage.State);
        Assert.IsFalse(coop.enabled);

        stage.Retry();

        Assert.AreEqual(StageState.Playing, stage.State);
        Assert.AreEqual(0f, player1.position.z);
    }

    [UnityTest]
    public IEnumerator Retry_IsIgnoredWhilePlaying()
    {
        StageController stage = CreateStage(1000f);
        yield return new WaitForSeconds(0.1f);
        float z = player1.position.z;

        stage.Retry();

        Assert.AreEqual(StageState.Playing, stage.State);
        Assert.AreEqual(z, player1.position.z, "진행 중 재시도는 위치를 되돌리지 않는다");
    }

    [UnityTest]
    public IEnumerator RandomMode_ClearsJustAfterLastObstacle_ThenRetryClearsAgain()
    {
        // 고정 목표 거리(1000)가 아니라 스포너가 정한 클리어 지점(마지막 장애물 직후)에서 클리어한다 (S-502).
        StageController stage = CreateStage(1000f);
        ObstacleSpawner spawner = CreateRandomSpawner(2);
        SetStageField(stage, "obstacleSpawner", spawner);
        SetStageField(stage, "clearMarginAfterLastObstacle", CLEAR_MARGIN);
        spawner.gameObject.SetActive(true);

        for (int attempt = 0; attempt < 2; attempt++)
        {
            yield return WaitUntilOrTimeout(() => stage.State != StageState.Playing, 5f);

            Assert.AreEqual(StageState.Cleared, stage.State);
            Assert.IsTrue(spawner.LastObstacleZ.HasValue);
            float clearDistance = spawner.LastObstacleZ.Value - coop.StartZ + CLEAR_MARGIN;
            Assert.That(coop.TravelledDistance, Is.GreaterThanOrEqualTo(clearDistance));
            Assert.That(coop.TravelledDistance, Is.LessThanOrEqualTo(clearDistance + coop.ForwardSpeed * Time.maximumDeltaTime),
                "클리어는 클리어 지점을 넘은 첫 프레임에 일어난다");

            stage.Retry();
            Assert.AreEqual(StageState.Playing, stage.State);
        }
    }

    [UnityTest]
    public IEnumerator RandomMode_InactiveSpawner_UsesInspectorTargetDistance()
    {
        // 무작위 모드 스포너가 비활성이면 클리어 지점이 정해지지 않으므로 Inspector 목표 거리로 클리어한다.
        StageController stage = CreateStage(1f);
        SetStageField(stage, "obstacleSpawner", CreateRandomSpawner(2));

        yield return WaitUntilOrTimeout(() => stage.State != StageState.Playing, 2f);

        Assert.AreEqual(StageState.Cleared, stage.State);
    }

    [UnityTest]
    public IEnumerator RandomMode_NoObstacles_UsesInspectorTargetDistance()
    {
        // 설정 오류로 장애물이 하나도 없으면 생성 거리 직후가 아니라 Inspector 목표 거리에서 클리어한다.
        StageController stage = CreateStage(6f);
        ObstacleSpawner spawner = CreateRandomSpawner(0);
        SetStageField(stage, "obstacleSpawner", spawner);
        LogAssert.Expect(LogType.Error, new Regex(@"장애물 없음"));
        LogAssert.Expect(LogType.Error, new Regex(@"난이도 1에 유효한 패턴이 없습니다"));
        LogAssert.Expect(LogType.Error, new Regex(@"장애물이 있는 유효한 패턴이 없어 장애물 없이 진행합니다"));
        spawner.gameObject.SetActive(true);

        yield return WaitUntilOrTimeout(() => stage.State != StageState.Playing, 3f);

        Assert.AreEqual(StageState.Cleared, stage.State);
        Assert.That(coop.TravelledDistance, Is.GreaterThanOrEqualTo(6f));
    }

    private StageController CreateStage(float targetDistance)
    {
        player1 = CreatePlayer("TestPlayer1", -4f);
        player2 = CreatePlayer("TestPlayer2", 4f);

        var coopObject = new GameObject("TestCoopController");
        coopObject.SetActive(false);
        created.Add(coopObject);
        coop = coopObject.AddComponent<CoopController>();
        typeof(CoopController).GetField("player1", FIELD_FLAGS).SetValue(coop, player1);
        typeof(CoopController).GetField("player2", FIELD_FLAGS).SetValue(coop, player2);

        var stageObject = new GameObject("TestStageController");
        stageObject.SetActive(false);
        created.Add(stageObject);
        StageController stage = stageObject.AddComponent<StageController>();
        SetStageField(stage, "coopController", coop);
        SetStageField(stage, "targetDistance", targetDistance);
        SetStageField(stage, "playerHitboxes", new[]
        {
            player1.GetComponent<PlayerHitbox>(),
            player2.GetComponent<PlayerHitbox>()
        });

        coopObject.SetActive(true);
        stageObject.SetActive(true);
        return stage;
    }

    private Transform CreatePlayer(string name, float x)
    {
        var player = new GameObject(name);
        created.Add(player);
        player.transform.position = new Vector3(x, 0f, 0f);
        player.AddComponent<CapsuleCollider>().isTrigger = true;
        var body = player.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        player.AddComponent<PlayerHitbox>();
        return player.transform;
    }

    private void CreateObstacle(Vector3 position)
    {
        var obstacle = new GameObject("TestObstacle");
        created.Add(obstacle);
        obstacle.transform.position = position;
        obstacle.AddComponent<BoxCollider>();
        obstacle.AddComponent<Obstacle>();
    }

    private CoopLanes GetLanes()
    {
        return (CoopLanes)typeof(CoopController).GetField("lanes", FIELD_FLAGS).GetValue(coop);
    }

    /// <summary>
    /// 빈 사이클 1개 뒤 사이클 하나짜리 패턴을 생성하는 무작위 모드 스포너. 2번 레일(가운데)에는 플레이어가 없다.
    /// </summary>
    private ObstacleSpawner CreateRandomSpawner(int cycle)
    {
        var prefab = new GameObject("TestRandomObstacle");
        prefab.AddComponent<Obstacle>();
        var pattern = ScriptableObject.CreateInstance<ObstaclePattern>();
        typeof(ObstaclePattern).GetField("cycles", FIELD_FLAGS).SetValue(pattern, new List<int> { cycle });

        var spawnerObject = new GameObject("TestRandomSpawner");
        spawnerObject.SetActive(false);
        ObstacleSpawner spawner = spawnerObject.AddComponent<ObstacleSpawner>();
        SetSpawnerField(spawner, "obstaclePrefab", prefab);
        SetSpawnerField(spawner, "forwardReference", player1);
        SetSpawnerField(spawner, "spawnDistance", 2f);
        SetSpawnerField(spawner, "cycleIntervalSeconds", 0.1f);
        SetSpawnerField(spawner, "patternOrder", ObstacleSpawner.PatternOrder.Random);
        SetSpawnerField(spawner, "randomPatternPool", new List<ObstaclePattern> { pattern });
        SetSpawnerField(spawner, "randomPatternCount", 1);
        SetSpawnerField(spawner, "difficultyShares", new List<ObstacleSpawner.DifficultyShare> { new ObstacleSpawner.DifficultyShare() });
        SetSpawnerField(spawner, "emptyCyclesBetweenPatterns", 1);

        created.AddRange(new Object[] { prefab, pattern, spawnerObject });
        return spawner;
    }

    private static void SetSpawnerField(ObstacleSpawner spawner, string name, object value)
    {
        typeof(ObstacleSpawner).GetField(name, FIELD_FLAGS).SetValue(spawner, value);
    }

    private static void SetStageField(StageController stage, string name, object value)
    {
        typeof(StageController).GetField(name, FIELD_FLAGS).SetValue(stage, value);
    }

    private static IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float timeoutSeconds)
    {
        float end = Time.time + timeoutSeconds;
        while (!condition() && Time.time < end) yield return null;
    }
}
