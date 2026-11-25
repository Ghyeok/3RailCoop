using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObstaclePattern", menuName = "3 Rail Coop/Obstacle Pattern")]
public class ObstaclePattern : ScriptableObject
{
    [SerializeField] private List<int> cycles = new List<int>();
    [Tooltip("무작위 모드에서 스테이지의 난이도 비율에 따라 고를 때 쓰는 난이도 (Game_Design §5).")]
    [SerializeField, Min(1)] private int difficulty = 1;

    public IReadOnlyList<int> Cycles => cycles;
    public int Difficulty => difficulty;
}
