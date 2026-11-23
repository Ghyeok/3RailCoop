using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode 테스트가 프레임을 진행시키고 게임 어셈블리를 참조할 수 있는지 확인한다.
/// </summary>
public class PlayModeSetupTests
{
    [UnityTest]
    public IEnumerator PlayMode_AdvancesFrameWithGameAssembly()
    {
        int startFrame = Time.frameCount;

        yield return null;

        Assert.Greater(Time.frameCount, startFrame);
        Assert.AreEqual("ThreeRailCoop", typeof(SteamBootstrap).Assembly.GetName().Name);
    }
}
