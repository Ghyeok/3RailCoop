using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// 프로젝트의 장애물 패턴 에셋이 모두 유효한 사이클만 담고 있는지 (S-304),
/// Game_Design §5 난이도 1·2 패턴이 표와 같은 사이클·난이도로 있는지 확인한다 (S-502).
/// </summary>
public class ObstaclePatternAssetTests
{
    private const string FOLDER = "Assets/ObstaclePatterns";

    // 이름 → (난이도, 사이클). D-008 테스트 스테이지의 빈 사이클 패턴(EmptyGap3)은 기획 표에 없다.
    private static readonly Dictionary<string, (int Difficulty, int[] Cycles)> DesignTable = new Dictionary<string, (int, int[])>
    {
        { "EndRailForce1", (1, new[] { 12, 12 }) },
        { "EndRailForce2", (1, new[] { 23, 23 }) },
        { "MergeSplitForce", (1, new[] { 1, 3, 1, 3, 13, 2 }) },
        { "CarelessLure1", (1, new[] { 13, 13, 13, 13, 2 }) },
        { "CarelessLure2", (1, new[] { 2, 2, 2, 2, 13 }) },
        { "RepeatMergeSplit", (1, new[] { 13, 2, 13, 2, 13, 2 }) },
        { "BlameOther1", (1, new[] { 13, 1, 3, 1, 3, 2 }) },
        { "BlameOther2", (1, new[] { 13, 1, 1, 2, 3, 3, 2 }) },
        { "QuickSplitKill1", (1, new[] { 13, 1, 2 }) },
        { "QuickSplitKill2", (1, new[] { 13, 3, 2 }) },
        { "LaneOrderLimit", (2, new[] { 12, 23, 12, 23 }) },
        { "MergeSplitVariation", (2, new[] { 13, 2, 13, 2, 13, 13, 2 }) },
        { "ZigZag", (2, new[] { 1, 2, 3, 2, 1, 2, 3, 13 }) },
        { "BackAndForth", (2, new[] { 12, 23, 13, 2, 13 }) },
        { "Difficulty2Unnamed", (2, new[] { 3, 3, 2, 2, 1, 1 }) },
    };

    private static IEnumerable<string> DesignPatternNames() => DesignTable.Keys;

    private static IEnumerable<string> PatternPaths()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ObstaclePattern", new[] { FOLDER }))
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

    [TestCaseSource(nameof(DesignPatternNames))]
    public void DesignPattern_MatchesTable(string name)
    {
        var pattern = AssetDatabase.LoadAssetAtPath<ObstaclePattern>($"{FOLDER}/{name}.asset");

        Assert.IsNotNull(pattern, $"{name} 에셋이 없음");
        Assert.AreEqual(DesignTable[name].Difficulty, pattern.Difficulty);
        CollectionAssert.AreEqual(DesignTable[name].Cycles, pattern.Cycles);
    }

    [Test]
    public void DesignTable_HasTenDifficulty1AndFiveDifficulty2()
    {
        Assert.AreEqual(10, DesignTable.Values.Count(p => p.Difficulty == 1));
        Assert.AreEqual(5, DesignTable.Values.Count(p => p.Difficulty == 2));
    }
}
