using System.Collections.Generic;
using UnityEngine;

namespace GWBGameJam
{
    public class ThrowSystem : MonoBehaviour
    {
        private const float LaneEntryBlendTime = 0.25f;

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
        private Vector2 _laneEntryPos;
        private Vector2 _targetPos;
        private int _targetLaneIndex;
        private float _flightTimer;
        private GameObject _activeProjectile;
        private bool _inFlight;
        private bool _isPlayingState;
        private bool _hasLaneEntryPos;
        private bool _hasConfigError;

        private void Awake() => ValidateConfig();

        private void ValidateConfig()
        {
            if (_config == null) { Debug.LogError("[ThrowSystem] ThrowConfig 未赋值"); _hasConfigError = true; }
            if (_doughSystem == null) { Debug.LogError("[ThrowSystem] DoughSystem 未赋值"); _hasConfigError = true; }
            if (_monsterSystem == null) { Debug.LogError("[ThrowSystem] MonsterSystem 未赋值"); _hasConfigError = true; }
            if (_laneManager == null) { Debug.LogError("[ThrowSystem] LaneManager 未赋值"); _hasConfigError = true; }
            if (_monsterConfig == null) { Debug.LogError("[ThrowSystem] MonsterConfig 未赋值"); _hasConfigError = true; }
            if (_boundaryConfig == null) { Debug.LogError("[ThrowSystem] DoughStateBoundaryConfig 未赋值"); _hasConfigError = true; }
            if (_throwOrigin == null) { Debug.LogError("[ThrowSystem] ThrowOrigin Transform 未赋值"); _hasConfigError = true; }
            if (_projectilePrefab == null) { Debug.LogError("[ThrowSystem] ProjectilePrefab 未赋值"); _hasConfigError = true; }
            if (_explosionPrefab == null)
                Debug.LogWarning("[ThrowSystem] ExplosionPrefab 未赋值，命中时无爆炸特效");
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
            if (_activeProjectile != null)
                Destroy(_activeProjectile);
        }

        private void Update()
        {
            if (_hasConfigError || !_inFlight || !_isPlayingState) return;

            _flightTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_flightTimer / _config.ThrowDuration);
            Vector2 position = EvaluateProjectilePosition(t);

            if (_activeProjectile != null)
                _activeProjectile.transform.position = position;
            if (t >= 1f)
                CompleteThrow();
        }

        private void HandleThrowRequested(OnThrowRequested e)
        {
            if (_hasConfigError || _inFlight) return;

            _targetLaneIndex = e.LaneIndex;
            _capturedRatio = _doughSystem.GetCurrentRatio();
            _capturedBakingState = e.BakingState;
            _startPos = _throwOrigin.position;

            MonsterController target = _monsterSystem.GetMonsterInLane(_targetLaneIndex);
            if (target != null)
                _targetPos = target.GetTargetPosition();
            else if (!_laneManager.TryGetWaypoint(_targetLaneIndex, 0, out _targetPos))
                _targetPos = _startPos;

            int laneEntryIndex = _monsterConfig.MoveStepCount - 1;
            _hasLaneEntryPos = _laneManager.TryGetWaypoint(_targetLaneIndex, laneEntryIndex, out _laneEntryPos);
            _flightTimer = 0f;
            _activeProjectile = Instantiate(_projectilePrefab, _startPos, Quaternion.identity);
            _inFlight = true;
            EventBus<OnThrowStarted>.Publish(new OnThrowStarted(_targetLaneIndex));
        }

        private Vector2 EvaluateProjectilePosition(float t)
        {
            Vector2 pathPosition;
            if (!_hasLaneEntryPos)
            {
                pathPosition = Vector2.Lerp(_startPos, _targetPos, t);
            }
            else if (t < LaneEntryBlendTime)
            {
                float entryT = Mathf.Clamp01(t / LaneEntryBlendTime);
                pathPosition = Vector2.Lerp(_startPos, _laneEntryPos, entryT);
            }
            else
            {
                float laneT = Mathf.Clamp01((t - LaneEntryBlendTime) / (1f - LaneEntryBlendTime));
                pathPosition = Vector2.Lerp(_laneEntryPos, _targetPos, laneT);
            }

            return pathPosition + Vector2.up * _config.PeakHeight * 4f * t * (1f - t);
        }

        private void CompleteThrow()
        {
            _inFlight = false;
            IReadOnlyList<MonsterController> monsters =
                _monsterSystem.GetMonstersInLane(_targetLaneIndex);

            ThrowResult result;
            int defeatedCount = 0;

            if (monsters.Count == 0)
            {
                result = ThrowResult.EmptyLane;
            }
            else if (_capturedBakingState != BakingState.Cooked)
            {
                result = ThrowResult.WrongBake;
            }
            else
            {
                for (int i = 0; i < monsters.Count; i++)
                {
                    MonsterController monster = monsters[i];
                    if (DetermineResult(monster) == ThrowResult.Hit)
                    {
                        Vector2 explosionPosition = monster.GetTargetPosition();
                        _monsterSystem.DefeatMonster(monster);
                        defeatedCount++;
                        if (_explosionPrefab != null)
                            Instantiate(_explosionPrefab, explosionPosition, Quaternion.identity);
                    }
                }

                if (defeatedCount == 0)
                {
                    result = ThrowResult.WrongRatio;
                    for (int i = 0; i < monsters.Count; i++)
                        _monsterSystem.TriggerWrongHitFeedback(monsters[i]);
                }
                else
                {
                    result = defeatedCount == monsters.Count
                        ? ThrowResult.Hit
                        : ThrowResult.PartialHit;
                }
            }

            EventBus<OnThrowCompleted>.Publish(
                new OnThrowCompleted(_targetLaneIndex, result, defeatedCount));

            if (_activeProjectile != null)
            {
                Destroy(_activeProjectile);
                _activeProjectile = null;
            }
        }

        private ThrowResult DetermineResult(MonsterController monster)
        {
            if (monster == null) return ThrowResult.EmptyLane;
            if (_capturedBakingState != BakingState.Cooked) return ThrowResult.WrongBake;

            float center = _boundaryConfig.GetCenterRatio(monster.Data.TargetDoughState);
            if (center < 0f) return ThrowResult.WrongRatio;

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
