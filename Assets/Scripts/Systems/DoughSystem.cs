using UnityEngine;

namespace GWBGameJam
{
    public class DoughSystem : MonoBehaviour
    {
        [SerializeField] private DoughConfig _config;
        [SerializeField] private DoughStateBoundaryConfig _boundaryConfig;

        private float _currentRatio;
        private DoughState _currentDoughState = DoughState.None;
        private bool _isPlayingState;
        private bool _isBakingIdle = true;
        private bool _hasConfigError;

        public float GetCurrentRatio() => _currentRatio;
        public DoughState GetCurrentDoughState() => _currentDoughState;

        private bool IsInputActive =>
            !_hasConfigError &&
            _isPlayingState &&
            _isBakingIdle &&
            _currentDoughState != DoughState.None;

        private void Awake()
        {
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (_config == null)
            {
                Debug.LogError("[DoughSystem] DoughConfig 未赋值");
                _hasConfigError = true;
            }
            if (_boundaryConfig == null)
            {
                Debug.LogError("[DoughSystem] DoughStateBoundaryConfig 未赋值");
                _hasConfigError = true;
            }
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
            if (!IsInputActive) return;

            if (Input.GetMouseButtonDown(0))
                ApplyFlour();

            if (Input.GetMouseButton(1))
                ApplyWater();
        }

        private void ApplyFlour()
        {
            _currentRatio = Mathf.Clamp(_currentRatio - _config.FlourClickAmount, 0f, _config.MaxRatio);
            DeriveAndPublishState();
        }

        private void ApplyWater()
        {
            _currentRatio = Mathf.Clamp(_currentRatio + _config.WaterFillRate * Time.deltaTime, 0f, _config.MaxRatio);
            DeriveAndPublishState();
        }

        private void DeriveAndPublishState()
        {
            DoughState newState = _boundaryConfig.GetDoughState(_currentRatio);
            if (newState == _currentDoughState) return;

            DoughState prev = _currentDoughState;
            _currentDoughState = newState;
            EventBus<OnDoughStateChanged>.Publish(new OnDoughStateChanged(newState, prev));
        }

        private void ResetToInitial()
        {
            if (_hasConfigError) return;
            _currentRatio = _config.InitialRatio;
            DoughState newState = _boundaryConfig.GetDoughState(_currentRatio);
            DoughState prev = _currentDoughState;
            _currentDoughState = newState;
            if (newState != prev)
                EventBus<OnDoughStateChanged>.Publish(new OnDoughStateChanged(newState, prev));
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            _isPlayingState = e.NewState == GameState.Playing;
        }

        private void HandleBakingStateChanged(OnBakingStateChanged e)
        {
            _isBakingIdle = e.NewState == BakingState.Idle;
        }

        private void HandleLevelStarted(OnLevelStarted e) => ResetToInitial();

        private void HandleThrowStarted(OnThrowStarted e)
        {
            DoughState prev = _currentDoughState;
            _currentDoughState = DoughState.None;
            if (prev != DoughState.None)
                EventBus<OnDoughStateChanged>.Publish(new OnDoughStateChanged(DoughState.None, prev));
        }

        private void HandleThrowCompleted(OnThrowCompleted e) => ResetToInitial();
    }
}
