using UnityEngine;

namespace GWBGameJam
{
    [RequireComponent(typeof(RectTransform))]
    public class DoughZone : MonoBehaviour
    {
        [SerializeField] private DoughState _state = DoughState.Medium;

        public DoughState State => _state;
        public RectTransform Rect => (RectTransform)transform;
    }
}
