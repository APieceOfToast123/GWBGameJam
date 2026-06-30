using UnityEngine;

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
            public Sprite NormalSprite;
            public Sprite HoveredSprite;
        }

        private int _hoveredLaneIndex = -1;
        private bool _isPlayingState;
        private bool _isBakingActive;
        private bool _hasConfigError;
        private bool _needsHoverCheck;
        private Vector3[] _baseScales;

        private bool IsHoverActive => _isPlayingState && _isBakingActive && !_hasConfigError;

        private void Awake()
        {
            ValidateConfig();
            CacheBaseScales();
        }

        private void CacheBaseScales()
        {
            if (_laneVisuals == null) return;
            _baseScales = new Vector3[_laneVisuals.Length];
            for (int i = 0; i < _laneVisuals.Length; i++)
                _baseScales[i] = _laneVisuals[i].Visual != null ? _laneVisuals[i].Visual.localScale : Vector3.one;
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

            // 每帧主动检测鼠标所在球道（不依赖 OnMouseEnter，避免被 UI 挡住事件）
            Vector2 mouseWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null && hit.TryGetComponent(out LaneHoverDetector detector))
                SetHoveredLane(detector.LaneIndex);
            else
                SetHoveredLane(-1);
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            bool wasActive = IsHoverActive;
            _isPlayingState = e.NewState == GameState.Playing;

            if (wasActive && !IsHoverActive)
                ResetAllLanes();
            else if (!wasActive && IsHoverActive)
                _needsHoverCheck = true;
        }

        private void HandleBakingStateChanged(OnBakingStateChanged e)
        {
            bool wasActive = IsHoverActive;
            _isBakingActive = e.NewState != BakingState.Idle;

            if (wasActive && !IsHoverActive)
                ResetAllLanes();
            else if (!wasActive && IsHoverActive)
                _needsHoverCheck = true;
        }

        // 由 LaneHoverDetector 调用
        public void OnLaneEnter(int laneIndex)
        {
            // ╔══════ DEBUG：lane hover ══════╗
#pragma warning disable 162
            const bool DebugHover = true;
            if (DebugHover)
                Debug.Log($"[LaneHover] OnLaneEnter lane={laneIndex} | IsHoverActive={IsHoverActive} (Playing={_isPlayingState} Baking={_isBakingActive} ConfigError={_hasConfigError})");
#pragma warning restore 162
            // ╚══════════════════════════════╝
            if (!IsHoverActive) return;
            SetHoveredLane(laneIndex);
        }

        public void OnLaneExit(int laneIndex)
        {
            if (_hoveredLaneIndex != laneIndex) return;
            SetHoveredLane(-1);
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
            // ╔══════ DEBUG：lane 换图 ══════╗
#pragma warning disable 162
            const bool DebugHover = true;
            if (DebugHover && hovered)
                Debug.Log($"[LaneHover] ApplyVisual lane={laneIndex} hovered=true | Renderer={(v.Renderer != null ? "OK" : "NULL")} HoveredSprite={(v.HoveredSprite != null ? v.HoveredSprite.name : "NULL")}");
#pragma warning restore 162
            // ╚══════════════════════════════╝
            if (v.Renderer != null)
                v.Renderer.sprite = hovered ? v.HoveredSprite : v.NormalSprite;
            if (v.Visual != null && _baseScales != null)
                v.Visual.localScale = _baseScales[laneIndex] * (hovered ? _hoverScaleMultiplier : 1f);
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

        public int GetHoveredLaneIndex() => _hoveredLaneIndex;

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
