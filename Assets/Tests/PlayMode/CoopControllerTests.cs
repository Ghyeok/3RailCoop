using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// CoopController 씬 통합: 전진 속도 통합과 시작 레일 검증 (S-203).
/// </summary>
public class CoopControllerTests
{
    private const BindingFlags FIELD_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly List<Object> created = new List<Object>();

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
    public IEnumerator PlayersAndCamera_AdvanceAtSharedForwardSpeed()
    {
        CoopController controller = CreateController(-4f, 4f, out Transform player1, out Transform player2);
        SetField(controller, "forwardSpeed", 10f);

        var cameraObject = new GameObject("TestCamera");
        cameraObject.SetActive(false);
        created.Add(cameraObject);
        var camera = cameraObject.AddComponent<QuarterViewCameraController>();
        typeof(QuarterViewCameraController).GetField("movementMode", FIELD_FLAGS)
            .SetValue(camera, System.Enum.Parse(typeof(QuarterViewCameraController).GetNestedType("MovementMode", BindingFlags.NonPublic), "ConstantSpeed"));
        typeof(QuarterViewCameraController).GetField("forwardSource", FIELD_FLAGS).SetValue(camera, controller);

        controller.gameObject.SetActive(true);
        cameraObject.SetActive(true);

        // 활성화한 프레임에는 카메라 LateUpdate만 돌 수 있고, 코루틴은 Update와 LateUpdate 사이에 재개된다.
        // 그래서 다음 프레임부터, 두 컴포넌트가 모두 갱신된 프레임 끝에서 이동량을 잰다.
        yield return null;
        yield return new WaitForEndOfFrame();
        float player1Start = player1.position.z;
        float cameraStart = cameraObject.transform.position.z;
        yield return new WaitForSeconds(0.3f);
        yield return new WaitForEndOfFrame();

        float player1Delta = player1.position.z - player1Start;
        Assert.Greater(player1Delta, 0f);
        Assert.AreEqual(player1.position.z, player2.position.z, 0.0001f);
        Assert.AreEqual(player1Delta, cameraObject.transform.position.z - cameraStart, 0.0001f, "카메라도 같은 속도로 전진");
    }

    [UnityTest]
    public IEnumerator SameStartLane_LogsErrorAndDisables()
    {
        return AssertStartLaneRejected(-4f, -4.5f);
    }

    [UnityTest]
    public IEnumerator BothBeyondSameEdge_LogsErrorAndDisables()
    {
        // x=-8은 레일 밖이지만 1번 레일로 보므로 x=-4(1번)와 같은 레일이다.
        return AssertStartLaneRejected(-8f, -4f);
    }

    private IEnumerator AssertStartLaneRejected(float x1, float x2)
    {
        CoopController controller = CreateController(x1, x2, out Transform player1, out _);
        LogAssert.Expect(LogType.Error, new Regex("서로 다른 레일"));

        controller.gameObject.SetActive(true);
        yield return null;
        yield return null;

        Assert.IsFalse(controller.enabled);
        Assert.AreEqual(0f, player1.position.z, "비활성화되어 전진하지 않음");
    }

    private CoopController CreateController(float x1, float x2, out Transform player1, out Transform player2)
    {
        player1 = new GameObject("TestPlayer1").transform;
        player2 = new GameObject("TestPlayer2").transform;
        player1.position = new Vector3(x1, 0f, 0f);
        player2.position = new Vector3(x2, 0f, 0f);

        var controllerObject = new GameObject("TestCoopController");
        controllerObject.SetActive(false);
        CoopController controller = controllerObject.AddComponent<CoopController>();
        SetField(controller, "player1", player1);
        SetField(controller, "player2", player2);

        created.AddRange(new Object[] { player1.gameObject, player2.gameObject, controllerObject });
        return controller;
    }

    private static void SetField(CoopController target, string name, object value)
    {
        typeof(CoopController).GetField(name, FIELD_FLAGS).SetValue(target, value);
    }
}
