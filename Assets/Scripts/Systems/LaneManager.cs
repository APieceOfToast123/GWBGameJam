using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public class LaneManager : MonoBehaviour
    {
        [SerializeField] private LaneWaypointConfig _waypointConfig;
        [SerializeField] private MonsterConfig _monsterConfig;
        [SerializeField] private Camera _camera;
        [SerializeField, Min(1f)] private float _hoverScaleMultiplier = 1.05f;
        [SerializeField] private LaneVisualData[] _laneVisuals;

        [System.Serializable]
        private struct LaneVisualData
        {
            public Transform Visual;
            public SpriteRenderer Renderer;
            public RectTransform UiVisual;
            public Image UiImage;
            public Sprite NormalSprite;
            public Sprite HoveredSprite;
        }

        private int _hoveredLaneIndex = -1;
        private bool _isPlayingState;
        private bool _isBakingActive;
        private bool _hasConfigError;

        private bool IsHoverActive => _isPlayingState && _isBakingActive && !_hasConfigError;

        private void Awake()
        {
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (_waypointConfig == null)
            {
                Debug.LogError("[LaneManager] LaneWaypointConfig 未赋值");
                _hasConfigError = true;
                return;
            }
            if (_monsterConfig == null)
            {
                Debug.LogError("[LaneManager] MonsterConfig 未赋值");
                _hasConfigError = true;
                return;
            }
            if (_camera == null)
            {
                Debug.LogError("[LaneManager] Camera 未赋值");
                _hasConfigError = true;
                return;
            }
            if (_laneVisuals == null || _laneVisuals.Length != 5)
            {
                Debug.LogError("[LaneManager] _laneVisuals 必须包含 5 个元素");
                _hasConfigError = true;
            }
            if (_waypointConfig.RecordedStepCount != _monsterConfig.MoveStepCount)
            {
                Debug.LogError($"[LaneManager] 点位数据已过期（RecordedStepCount={_waypointConfig.RecordedStepCount}，MoveStepCount={_monsterConfig.MoveStepCount}），请重新运行 Lane 点位计算器");
                _hasConfigError = true;
            }
        }

        private void OnEnable()
        {
            EventBus<OnGameStateChanged>.Subscribe(HandleGameStateChanged);
            EventBus<OnBakingStateChanged>.Subscribe(HandleBakingStateChanged);
        }

        private void OnDestroy()
        {
            EventBus<OnGameStateChanged>.Unsubscribe(HandleGameStateChanged);
            EventBus<OnBakingStateChanged>.Unsubscribe(HandleBakingStateChanged);
        }

        private void Update()
        {
            if (!IsHoverActive) return;
            SetHoveredLane(ResolvePointerLaneIndex());
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            bool wasActive = IsHoverActive;
            _isPlayingState = e.NewState == GameState.Playing;

            if (wasActive && !IsHoverActive)
                ResetAllLanes();
        }

        private void HandleBakingStateChanged(OnBakingStateChanged e)
        {
            bool wasActive = IsHoverActive;
            _isBakingActive = e.NewState != BakingState.Idle;

            if (wasActive && !IsHoverActive)
                ResetAllLanes();
        }

        // 由 LaneHoverDetector 调用
        public void OnLaneEnter(int laneIndex)
        {
            if (!IsHoverActive) return;
            SetHoveredLane(ResolvePointerLaneIndex());
        }

        public void OnLaneExit(int laneIndex)
        {
            if (!IsHoverActive) return;
            SetHoveredLane(ResolvePointerLaneIndex());
        }

        private int ResolvePointerLaneIndex()
        {
            int laneCount = _waypointConfig.Lanes != null ? _waypointConfig.Lanes.Length : 0;
            if (laneCount <= 0) return -1;

            Vector2 pointerWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
            int bestLane = -1;
            float bestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < laneCount; i++)
            {
                float sqrDistance = DistanceToLaneSqr(pointerWorld, i);
                if (sqrDistance >= bestSqrDistance) continue;

                bestSqrDistance = sqrDistance;
                bestLane = i;
            }

            return bestLane;
        }

        private float DistanceToLaneSqr(Vector2 point, int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= _waypointConfig.Lanes.Length)
                return float.PositiveInfinity;

            Vector2[] positions = _waypointConfig.Lanes[laneIndex].Positions;
            if (positions == null || positions.Length == 0)
                return float.PositiveInfinity;
            if (positions.Length == 1)
                return (point - positions[0]).sqrMagnitude;

            float best = float.PositiveInfinity;
            for (int i = 0; i < positions.Length - 1; i++)
            {
                float sqrDistance = DistancePointToSegmentSqr(point, positions[i], positions[i + 1]);
                if (sqrDistance < best)
                    best = sqrDistance;
            }

            return best;
        }

        private static float DistancePointToSegmentSqr(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSqr = ab.sqrMagnitude;
            if (lengthSqr <= Mathf.Epsilon)
                return (point - a).sqrMagnitude;

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSqr);
            Vector2 closest = a + ab * t;
            return (point - closest).sqrMagnitude;
        }

        private void SetHoveredLane(int laneIndex)
        {
            if (_hoveredLaneIndex == laneIndex) return;

            if (_hoveredLaneIndex != -1)
                ApplyVisual(_hoveredLaneIndex, false);

            _hoveredLaneIndex = laneIndex;

            if (_hoveredLaneIndex != -1)
                ApplyVisual(_hoveredLaneIndex, true);

            EventBus<OnLaneHoverChanged>.Publish(new OnLaneHoverChanged(_hoveredLaneIndex));
        }

        private void ResetAllLanes()
        {
            if (_hoveredLaneIndex == -1) return;
            ApplyVisual(_hoveredLaneIndex, false);
            _hoveredLaneIndex = -1;
            EventBus<OnLaneHoverChanged>.Publish(new OnLaneHoverChanged(-1));
        }

        private void ApplyVisual(int laneIndex, bool hovered)
        {
            if (laneIndex < 0 || laneIndex >= _laneVisuals.Length) return;
            var v = _laneVisuals[laneIndex];
            Sprite sprite = hovered ? v.HoveredSprite : v.NormalSprite;
            if (v.Renderer != null)
            {
                v.Renderer.sprite = sprite;
                v.Renderer.enabled = sprite != null;
            }
            if (v.UiImage != null)
            {
                v.UiImage.sprite = sprite;
                v.UiImage.enabled = sprite != null;
            }
            if (v.Visual != null)
                v.Visual.localScale = hovered ? Vector3.one * _hoverScaleMultiplier : Vector3.one;
            if (v.UiVisual != null)
                v.UiVisual.localScale = hovered ? Vector3.one * _hoverScaleMultiplier : Vector3.one;
        }

        public bool TryGetWaypoint(int laneIndex, int posIndex, out Vector2 position)
        {
            position = Vector2.zero;
            if (_hasConfigError) return false;
            if (laneIndex < 0 || laneIndex >= _waypointConfig.Lanes.Length)
            {
                Debug.LogWarning($"[LaneManager] TryGetWaypoint: laneIndex {laneIndex} 越界");
                return false;
            }
            var positions = _waypointConfig.Lanes[laneIndex].Positions;
            if (posIndex < 0 || posIndex >= positions.Length)
            {
                Debug.LogWarning($"[LaneManager] TryGetWaypoint: posIndex {posIndex} 越界（laneIndex={laneIndex}）");
                return false;
            }
            position = positions[posIndex];
            return true;
        }

        public int GetHoveredLaneIndex()
        {
            if (IsHoverActive)
                SetHoveredLane(ResolvePointerLaneIndex());
            return _hoveredLaneIndex;
        }

        private void OnDrawGizmos()
        {
            if (_waypointConfig == null) return;
            Gizmos.color = Color.yellow;
            foreach (var lane in _waypointConfig.Lanes)
            {
                if (lane?.Positions == null) continue;
                foreach (var pos in lane.Positions)
                    Gizmos.DrawSphere(pos, 0.1f);
            }
        }
    }
}
