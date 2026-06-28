using UnityEngine;

namespace GWBGameJam
{
    public class LaneHoverDetector : MonoBehaviour
    {
        [SerializeField] private LaneManager _laneManager;
        [SerializeField] private int _laneIndex;

        public int LaneIndex => _laneIndex;

        private void Awake()
        {
            if (_laneManager == null)
                Debug.LogError($"[LaneHoverDetector] Lane_{_laneIndex} 的 LaneManager 引用未赋值");
        }

        private void OnMouseEnter() => _laneManager?.OnLaneEnter(_laneIndex);
        private void OnMouseExit() => _laneManager?.OnLaneExit(_laneIndex);
    }
}
