using UnityEngine;

namespace GWBGameJam
{
    public class ThrowSystem : MonoBehaviour
    {
        [SerializeField] private ThrowConfig _config;
        [SerializeField] private DoughSystem _doughSystem;
        [SerializeField] private MonsterSystem _monsterSystem;
        [SerializeField] private LaneManager _laneManager;
        [SerializeField] private MonsterConfig _monsterConfig;
        [SerializeField] private DoughStateBoundaryConfig _boundaryConfig;
        [SerializeField] private Transform _throwOrigin;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _explosionPrefab;

        private float _capturedRatio;
        private BakingState _capturedBakingState;
        private Vector2 _startPos;
        private Vector2 _targetPos;
        private int _targetLaneIndex;
        private float _flightTimer;
        private GameObject _activeProjectile;
        private bool _inFlight;
        private bool _isPlayingState;
        private bool _hasConfigError;

        private void Awake()
        {
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (_config == null)        { Debug.LogError("[ThrowSystem] ThrowConfig 未赋值");               _hasConfigError = true; }
            if (_doughSystem == null)   { Debug.LogError("[ThrowSystem] DoughSystem 未赋值");               _hasConfigError = true; }
            if (_monsterSystem == null) { Debug.LogError("[ThrowSystem] MonsterSystem 未赋值");             _hasConfigError = true; }
            if (_laneManager == null)   { Debug.LogError("[ThrowSystem] LaneManager 未赋值");               _hasConfigError = true; }
            if (_monsterConfig == null) { Debug.LogError("[ThrowSystem] MonsterConfig 未赋值");             _hasConfigError = true; }
            if (_boundaryConfig == null){ Debug.LogError("[ThrowSystem] DoughStateBoundaryConfig 未赋值");  _hasConfigError = true; }
            if (_throwOrigin == null)   { Debug.LogError("[ThrowSystem] ThrowOrigin Transform 未赋值");     _hasConfigError = true; }
            if (_projectilePrefab == null) { Debug.LogError("[ThrowSystem] ProjectilePrefab 未赋值");       _hasConfigError = true; }
            if (_explosionPrefab == null)  Debug.LogWarning("[ThrowSystem] ExplosionPrefab 未赋值，命中时无爆炸特效");
        }

        private void OnEnable()
        {
            EventBus<OnThrowRequested>.Subscribe(HandleThrowRequested);
            EventBus<OnGameStateChanged>.Subscribe(HandleGameStateChanged);
        }

        private void OnDestroy()
        {
            EventBus<OnThrowRequested>.Unsubscribe(HandleThrowRequested);
            EventBus<OnGameStateChanged>.Unsubscribe(HandleGameStateChanged);
            // Destroy in-flight projectile without publishing events (scene may be unloading)
            if (_activeProjectile != null)
                Destroy(_activeProjectile);
        }

        private void Update()
        {
            if (_hasConfigError || !_inFlight || !_isPlayingState) return;

            _flightTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_flightTimer / _config.ThrowDuration);

            Vector2 pos = Vector2.Lerp(_startPos, _targetPos, t)
                        + Vector2.up * _config.PeakHeight * 4f * t * (1f - t);

            if (_activeProjectile != null)
                _activeProjectile.transform.position = pos;

            if (t >= 1f)
                CompleteThrow();
        }

        private void HandleThrowRequested(OnThrowRequested e)
        {
            if (_hasConfigError) return;

            _targetLaneIndex = e.LaneIndex;
            _capturedRatio = _doughSystem.GetCurrentRatio();
            _capturedBakingState = e.BakingState;
            _startPos = _throwOrigin.position;

            // Target position: monster's predicted pos if present, else mid-lane waypoint
            MonsterController monster = _monsterSystem.GetMonsterInLane(_targetLaneIndex);
            if (monster != null)
            {
                _targetPos = _monsterSystem.GetTargetPosition(_targetLaneIndex);
            }
            else if (!_laneManager.TryGetWaypoint(_targetLaneIndex, _monsterConfig.MoveStepCount / 2, out _targetPos))
            {
                _targetPos = _startPos;
            }

            _flightTimer = 0f;
            _activeProjectile = Instantiate(_projectilePrefab, _startPos, Quaternion.identity);
            _inFlight = true;

            EventBus<OnThrowStarted>.Publish(new OnThrowStarted(_targetLaneIndex));
        }

        private void CompleteThrow()
        {
            _inFlight = false;

            MonsterController monster = _monsterSystem.GetMonsterInLane(_targetLaneIndex);
            ThrowResult result = DetermineResult(monster);

            switch (result)
            {
                case ThrowResult.Hit:
                    _monsterSystem.DefeatMonster(_targetLaneIndex);
                    if (_explosionPrefab != null)
                        Instantiate(_explosionPrefab, _targetPos, Quaternion.identity);
                    break;
                case ThrowResult.WrongRatio:
                    _monsterSystem.TriggerWrongHitFeedback(_targetLaneIndex);
                    break;
            }

            EventBus<OnThrowCompleted>.Publish(new OnThrowCompleted(_targetLaneIndex, result));

            if (_activeProjectile != null)
            {
                Destroy(_activeProjectile);
                _activeProjectile = null;
            }
        }

        private ThrowResult DetermineResult(MonsterController monster)
        {
            if (monster == null) return ThrowResult.EmptyLane;
            if (_capturedBakingState != BakingState.Cooked) return ThrowResult.WrongRatio;

            float center = _boundaryConfig.GetCenterRatio(monster.Data.TargetDoughState);
            if (center < 0f) return ThrowResult.WrongRatio; // TooSoft/TooHard/None have no valid center

            return Mathf.Abs(_capturedRatio - center) <= _boundaryConfig.ToleranceHalfWidth
                ? ThrowResult.Hit
                : ThrowResult.WrongRatio;
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            _isPlayingState = e.NewState == GameState.Playing;
        }
    }
}
