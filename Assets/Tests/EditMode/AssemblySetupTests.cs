using NUnit.Framework;

/// <summary>
/// 게임 코드가 ThreeRailCoop 어셈블리로 분리되어 EditMode 테스트에서 참조되는지 확인한다.
/// </summary>
public class AssemblySetupTests
{
    [Test]
    public void GameScripts_CompileIntoGameAssembly()
    {
        Assert.AreEqual("ThreeRailCoop", typeof(SteamAvatarUtility).Assembly.GetName().Name);
    }
}
