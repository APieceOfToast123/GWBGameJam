using UnityEngine;

namespace GWBGameJam
{
    [CreateAssetMenu(fileName = "LaneCalculatorData", menuName = "GWBGameJam/Editor/LaneCalculatorData")]
    public class LaneCalculatorData : ScriptableObject
    {
        public float[] WaypointYPositions = new float[8];
        public string[] LaneRootNames = { "Lane_0", "Lane_1", "Lane_2", "Lane_3", "Lane_4" };
    }
}
