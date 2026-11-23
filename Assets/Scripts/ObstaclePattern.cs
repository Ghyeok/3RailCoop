using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObstaclePattern", menuName = "3 Rail Coop/Obstacle Pattern")]
public class ObstaclePattern : ScriptableObject
{
    [SerializeField] private List<int> cycles = new List<int>();

    public IReadOnlyList<int> Cycles => cycles;
}
