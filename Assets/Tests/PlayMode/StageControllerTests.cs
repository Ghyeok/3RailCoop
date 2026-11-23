using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 충돌 → 게임 오버 → 재시도, 목표 거리 클리어의 씬 통합 (S-302).
/// </summary>
public class StageControllerTests
{
    private const BindingFlags FIELD_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly List<Object> created = new List<Object>();
    private Transform player1;
    private Transform player2;
    private CoopController coop;

    [TearDown]
    public void TearDown()
    {
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
