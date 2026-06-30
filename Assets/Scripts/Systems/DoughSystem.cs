using UnityEngine;

namespace GWBGameJam
{
    public class DoughSystem : MonoBehaviour
    {
        [SerializeField] private DoughConfig _config;
        [SerializeField] private RectTransform _barRect;
        [SerializeField] private RectTransform _indicatorRect;
        [SerializeField] private DoughZone[] _zones;

        private float _pos;
        private DoughState _currentDoughState = DoughState.None;
        private bool _isPlayingState;
        private bool _isBakingIdle = true;
        private bool _inFlight;
        private bool _hasConfigError;
        private float _activeWaterFactor = 1f;

        private readonly Vector3[] _cornersA = new Vector3[4];
        private readonly Vector3[] _cornersB = new Vector3[4];

        private bool CanProcessInput =>
            !_hasConfigError && _isPlayingState && _isBakingIdle && !_inFlight;

        public DoughState GetCurrentDoughState() => _currentDoughState;
        public bool IsInputActive() => CanProcessInput;
        public float GetNormalizedPos() => _pos;

        private void Awake()
        {
            ValidateConfig();
            _pos = _hasConfigError ? 0.5f : Mathf.Clamp(_config.InitialPos, _config.PosMin, _config.PosMax);
        }

        private void ValidateConfig()
        {
            if (_config == null)        { Debug.LogError("[DoughSystem] DoughConfig 未赋值");   _hasConfigError = true; }
            if (_barRect == null)       { Debug.LogError("[DoughSystem] BarRect 未赋值");        _hasConfigError = true; }
            if (_indicatorRect == null) { Debug.LogError("[DoughSystem] IndicatorRect 未赋值");  _hasConfigError = true; }
            if (_zones == null || _zones.Length == 0) { Debug.LogError("[DoughSystem] Zones 未配置"); _hasConfigError = true; }
            if (!_hasConfigError)
                _config.Validate();
        }

        private void OnEnable()
        {
            EventBus<OnGameStateChanged>.Subscribe(HandleGameStateChanged);
            EventBus<OnBakingStateChanged>.Subscribe(HandleBakingStateChanged);
            EventBus<OnLevelStarted>.Subscribe(HandleLevelStarted);
            EventBus<OnThrowStarted>.Subscribe(HandleThrowStarted);
            EventBus<OnThrowCompleted>.Subscribe(HandleThrowCompleted);
        }

        private void OnDestroy()
        {
            EventBus<OnGameStateChanged>.Unsubscribe(HandleGameStateChanged);
            EventBus<OnBakingStateChanged>.Unsubscribe(HandleBakingStateChanged);
            EventBus<OnLevelStarted>.Unsubscribe(HandleLevelStarted);
            EventBus<OnThrowStarted>.Unsubscribe(HandleThrowStarted);
            EventBus<OnThrowCompleted>.Unsubscribe(HandleThrowCompleted);
        }

        private void Update()
        {
            if (!CanProcessInput) return;

            // 左键加粉 → 指示器右移（_pos 增大），每次乘随机倍率
            if (Input.GetMouseButtonDown(0))
                _pos = Mathf.Clamp(_pos + _config.FlourStep * Random.Range(_config.FlourFactorMin, _config.FlourFactorMax), _config.PosMin, _config.PosMax);

            // 右键按下时抽一次倍率，本次长按固定
            if (Input.GetMouseButtonDown(1))
                _activeWaterFactor = Random.Range(_config.WaterFactorMin, _config.WaterFactorMax);

            // 右键加水 → 指示器左移（_pos 减小），连续
            if (Input.GetMouseButton(1))
                _pos = Mathf.Clamp(_pos - _config.WaterSpeed * _activeWaterFactor * Time.deltaTime, _config.PosMin, _config.PosMax);

            MoveIndicator();
            DeriveAndPublishState();
        }

        private void MoveIndicator()
        {
            float halfWidth = _barRect.rect.width * 0.5f;
            float x = Mathf.Lerp(-halfWidth, halfWidth, _pos);
            _indicatorRect.anchoredPosition = new Vector2(x, _indicatorRect.anchoredPosition.y);
        }

        private void DeriveAndPublishState()
        {
            DoughState newState = ResolveOverlapState();
            if (newState == _currentDoughState) return;

            DoughState prev = _currentDoughState;
            _currentDoughState = newState;
            EventBus<OnDoughStateChanged>.Publish(new OnDoughStateChanged(newState, prev));
        }

        // 取指示器与各 DoughZone 水平重叠面积最大的那个；无重叠则按位置记失败档位
        private DoughState ResolveOverlapState()
        {
            _indicatorRect.GetWorldCorners(_cornersA);
            float aMin = _cornersA[0].x, aMax = _cornersA[2].x;

            float bestOverlap = 0f;
            DoughState bestState = DoughState.None;
            for (int i = 0; i < _zones.Length; i++)
            {
                if (_zones[i] == null) continue;
                _zones[i].Rect.GetWorldCorners(_cornersB);
                float bMin = _cornersB[0].x, bMax = _cornersB[2].x;
                float overlap = Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin);
                if (overlap > bestOverlap)
                {
                    bestOverlap = overlap;
                    bestState = _zones[i].State;
                }
            }

            if (bestOverlap > 0f)
                return bestState;
            return _pos <= 0.5f ? DoughState.TooSoft : DoughState.TooHard;
        }

        private void ResetToInitial()
        {
            if (_hasConfigError) return;
            _pos = Mathf.Clamp(_config.InitialPos, _config.PosMin, _config.PosMax);
            MoveIndicator();
            DeriveAndPublishState();
        }

        private void HandleGameStateChanged(OnGameStateChanged e) => _isPlayingState = e.NewState == GameState.Playing;

        private void HandleBakingStateChanged(OnBakingStateChanged e) => _isBakingIdle = e.NewState == BakingState.Idle;

        private void HandleLevelStarted(OnLevelStarted e) => ResetToInitial();

        private void HandleThrowStarted(OnThrowStarted e)
        {
            _inFlight = true;
            DoughState prev = _currentDoughState;
            _currentDoughState = DoughState.None;
            if (prev != DoughState.None)
                EventBus<OnDoughStateChanged>.Publish(new OnDoughStateChanged(DoughState.None, prev));
        }

        private void HandleThrowCompleted(OnThrowCompleted e)
        {
            _inFlight = false;
            ResetToInitial();
        }
    }
}
