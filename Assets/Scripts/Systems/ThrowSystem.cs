using System.Collections.Generic;
using UnityEngine;

namespace GWBGameJam
{
    public class ThrowSystem : MonoBehaviour
    {
        private const float LaneEntryBlendTime = 0.25f;
        private static readonly DoughState[] RequiredBreadStates =
        {
            DoughState.TooSoft,
            DoughState.Softest,
            DoughState.Medium,
            DoughState.Hardest,
            DoughState.TooHard
        };

        [SerializeField] private ThrowConfig _config;
        [SerializeField] private DoughSystem _doughSystem;
        [SerializeField] private MonsterSystem _monsterSystem;
        [SerializeField] private LaneManager _laneManager;
        [SerializeField] private MonsterConfig _monsterConfig;
        [SerializeField] private Transform _throwOrigin;
        [SerializeField] private BreadProjectile _projectilePrefab;
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private BreadSprite[] _breadSprites;

        [System.Serializable]
        private struct BreadSprite
        {
            public DoughState State;
            public Sprite Sprite;
        }

        private DoughState _capturedDoughState;
        private BakingState _capturedBakingState;
        private Vector2 _startPos;
        private Vector2 _laneEntryPos;
        private Vector2 _targetPos;
        private int _targetLaneIndex;
        private float _flightTimer;
        private BreadProjectile _activeProjectile;
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
            if (_throwOrigin == null) { Debug.LogError("[ThrowSystem] ThrowOrigin Transform 未赋值"); _hasConfigError = true; }
            if (_projectilePrefab == null) { Debug.LogError("[ThrowSystem] ProjectilePrefab 未赋值"); _hasConfigError = true; }
            if (_explosionPrefab == null)
                Debug.LogWarning("[ThrowSystem] ExplosionPrefab 未赋值，命中时无爆炸特效");
            ValidateBreadSprites();
        }

        private void ValidateBreadSprites()
        {
            if (_breadSprites == null || _breadSprites.Length == 0)
            {
                Debug.LogError("[ThrowSystem] BreadSprites 未配置，投射物会使用 Prefab 默认图片");
                return;
            }

            for (int i = 0; i < RequiredBreadStates.Length; i++)
            {
                DoughState state = RequiredBreadStates[i];
                if (!HasBreadSprite(state))
                    Debug.LogError($"[ThrowSystem] BreadSprites 缺少 {state} 对应图片");
            }

            for (int i = 0; i < _breadSprites.Length; i++)
            {
                if (_breadSprites[i].Sprite == null)
                    Debug.LogError($"[ThrowSystem] BreadSprites 第 {i} 项 Sprite 为空");

                for (int j = i + 1; j < _breadSprites.Length; j++)
                    if (_breadSprites[i].State == _breadSprites[j].State)
                        Debug.LogError($"[ThrowSystem] BreadSprites 存在重复状态 {_breadSprites[i].State}");
            }
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
                Destroy(_activeProjectile.gameObject);
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
            _capturedDoughState = _doughSystem.GetCurrentDoughState();
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
            ApplyBreadSprite(_activeProjectile, _capturedDoughState);
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
                Destroy(_activeProjectile.gameObject);
                _activeProjectile = null;
            }
        }

        private ThrowResult DetermineResult(MonsterController monster)
        {
            if (monster == null) return ThrowResult.EmptyLane;
            if (_capturedBakingState != BakingState.Cooked) return ThrowResult.WrongBake;

            return _capturedDoughState == monster.Data.TargetDoughState
                ? ThrowResult.Hit
                : ThrowResult.WrongRatio;
        }

        private void ApplyBreadSprite(BreadProjectile projectile, DoughState state)
        {
            if (projectile == null) return;

            if (TryGetBreadSprite(state, out Sprite sprite))
                projectile.SetSprite(sprite);
            else
                Debug.LogError($"[ThrowSystem] 未找到 {state} 对应投射物图片，保留 Prefab 默认图片");
        }

        private bool HasBreadSprite(DoughState state)
        {
            return TryGetBreadSprite(state, out _);
        }

        private bool TryGetBreadSprite(DoughState state, out Sprite sprite)
        {
            sprite = null;
            if (_breadSprites == null) return false;

            for (int i = 0; i < _breadSprites.Length; i++)
            {
                if (_breadSprites[i].State != state || _breadSprites[i].Sprite == null)
                    continue;

                sprite = _breadSprites[i].Sprite;
                return true;
            }

            return false;
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            _isPlayingState = e.NewState == GameState.Playing;
        }
    }
}
