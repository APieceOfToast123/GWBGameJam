using UnityEngine;

namespace GWBGameJam
{
    public class BakingSystem : MonoBehaviour
    {
        private const int LaneCount = 5;

        [SerializeField] private BakingConfig _config;
        [SerializeField] private LaneManager _laneManager;
        [SerializeField] private DoughSystem _doughSystem;

        private float _bakingTimer;
        private BakingState _currentBakingState = BakingState.Idle;
        private bool _isPlayingState;
        private bool _hasConfigError;
        private float _activeCookDuration;

        public float GetBakingTimer() => _bakingTimer;
        public BakingState GetBakingState() => _currentBakingState;
        public float GetActiveCookDuration() => _activeCookDuration;
        public float GetActiveTotalDuration() =>
            _activeCookDuration + _config.PerfectWindowDuration + _config.BurntWindowDuration;

        private void Awake()
        {
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (_config == null)
            {
                Debug.LogError("[BakingSystem] BakingConfig 未赋值");
                _hasConfigError = true;
            }
            if (_laneManager == null)
            {
                Debug.LogError("[BakingSystem] LaneManager 未赋值");
                _hasConfigError = true;
            }
            if (_doughSystem == null)
            {
                Debug.LogError("[BakingSystem] DoughSystem 未赋值");
                _hasConfigError = true;
            }
            if (!_hasConfigError)
                _config.Validate();
        }

        private void OnEnable()
        {
            EventBus<OnGameStateChanged>.Subscribe(HandleGameStateChanged);
        }

        private void OnDestroy()
        {
            EventBus<OnGameStateChanged>.Unsubscribe(HandleGameStateChanged);
        }

        private void Update()
        {
            if (_hasConfigError || !_isPlayingState) return;

            HandleSpaceInput();

            if (_currentBakingState != BakingState.Idle)
                AdvanceTimer();
        }

        private void HandleSpaceInput()
        {
            if (_currentBakingState == BakingState.Idle)
            {
                if (Input.GetKeyDown(KeyCode.Space) && _doughSystem.GetCurrentDoughState() != DoughState.None)
                    StartBaking();
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                TriggerThrow();
            }
        }

        private void StartBaking()
        {
            _bakingTimer = 0f;
            _activeCookDuration = ResolveCookDuration(_doughSystem.GetCurrentDoughState());
            BakingState prev = _currentBakingState;
            _currentBakingState = BakingState.Undercooked;
            EventBus<OnBakingStateChanged>.Publish(new OnBakingStateChanged(BakingState.Undercooked, prev));
        }

        private void AdvanceTimer()
        {
            _bakingTimer += Time.deltaTime;

            if (_currentBakingState == BakingState.Undercooked && _bakingTimer >= _activeCookDuration)
                TransitionTo(BakingState.Cooked);
            else if (_currentBakingState == BakingState.Cooked
                     && _bakingTimer >= _activeCookDuration + _config.PerfectWindowDuration)
                TransitionTo(BakingState.Burnt);
            else if (_currentBakingState == BakingState.Burnt
                     && _bakingTimer >= GetActiveTotalDuration())
                TriggerThrow();
        }

        private float ResolveCookDuration(DoughState state)
        {
            return state switch
            {
                DoughState.TooSoft => _config.SoftestCookDuration,
                DoughState.Softest => _config.SoftestCookDuration,
                DoughState.Medium => _config.MediumCookDuration,
                DoughState.Hardest => _config.HardestCookDuration,
                DoughState.TooHard => _config.HardestCookDuration,
                _ => _config.MediumCookDuration
            };
        }

        private void TransitionTo(BakingState newState)
        {
            BakingState prev = _currentBakingState;
            _currentBakingState = newState;
            EventBus<OnBakingStateChanged>.Publish(new OnBakingStateChanged(newState, prev));
        }

        private void TriggerThrow()
        {
            int laneIndex = ResolveThrowLaneIndex();
            BakingState throwBakingState = _currentBakingState;
            BakingState prev = _currentBakingState;
            _bakingTimer = 0f;
            _activeCookDuration = 0f;
            _currentBakingState = BakingState.Idle;

            EventBus<OnThrowRequested>.Publish(new OnThrowRequested(laneIndex, throwBakingState));
            EventBus<OnBakingStateChanged>.Publish(new OnBakingStateChanged(BakingState.Idle, prev));
        }

        private int ResolveThrowLaneIndex()
        {
            int laneIndex = _laneManager.GetHoveredLaneIndex();
            return laneIndex >= 0 ? laneIndex : Random.Range(0, LaneCount);
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            bool wasPlaying = _isPlayingState;
            _isPlayingState = e.NewState == GameState.Playing;

            if (!wasPlaying && _isPlayingState && ShouldResetWhenEnteringPlaying(e.PreviousState))
            {
                ResetToIdle();
            }
        }

        private static bool ShouldResetWhenEnteringPlaying(GameState previousState)
        {
            return previousState switch
            {
                GameState.LevelTransition => true,
                GameState.Death => true,
                GameState.Victory => true,
                _ => false
            };
        }

        private void ResetToIdle()
        {
            BakingState prev = _currentBakingState;
            _bakingTimer = 0f;
            _activeCookDuration = 0f;
            _currentBakingState = BakingState.Idle;
            EventBus<OnBakingStateChanged>.Publish(new OnBakingStateChanged(BakingState.Idle, prev));
        }
    }
}
