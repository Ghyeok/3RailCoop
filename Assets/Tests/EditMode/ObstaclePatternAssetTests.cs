using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// 프로젝트의 장애물 패턴 에셋이 모두 유효한 사이클만 담고 있는지 확인한다 (S-304).
/// </summary>
public class ObstaclePatternAssetTests
{
    private static IEnumerable<string> PatternPaths()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ObstaclePattern", new[] { "Assets/ObstaclePatterns" }))
            yield return AssetDatabase.GUIDToAssetPath(guid);
    }

    [Test]
    public void PatternAssetsExist()
    {
        CollectionAssert.IsNotEmpty(PatternPaths());
    }

    [TestCaseSource(nameof(PatternPaths))]
    public void PatternAsset_HasOnlyValidCycles(string path)
    {
        var pattern = AssetDatabase.LoadAssetAtPath<ObstaclePattern>(path);

        Assert.IsNotNull(pattern);
        CollectionAssert.IsNotEmpty(pattern.Cycles);
        foreach (int cycle in pattern.Cycles)
            Assert.IsTrue(ObstacleCycle.IsValid(cycle), $"{path}: 유효하지 않은 사이클 {cycle}");
    }
}
