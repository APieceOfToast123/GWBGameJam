using UnityEngine;

namespace GWBGameJam
{
    public class MonsterController : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public MonsterData Data { get; private set; }
        public int LaneIndex { get; private set; }
        public int PositionIndex => _posIndex;

        private MonsterConfig _config;
        private LaneManager _laneManager;
        private int _posIndex;
        private float _moveTimer;
        private bool _isMoving;
        private int _targetPosIndex;
        private Vector2 _moveFromPos;
        private Vector2 _moveToPos;
        private float _moveProgress;
        private float _wrongHitTimer;
        private int _wrongHitCompletedCount;
        private bool _wrongHitActive;
        private bool _wrongHitShowingHit;
        private bool _hasVisualError;
        private bool _hasSpriteRendererError;

        // _config may be null before Initialize; guard before reading
        public bool IsPendingMove => _config != null && !_isMoving && _moveTimer <= _config.PendingMoveThreshold;

        private void Awake()
        {
            if (_visual == null)
            {
                Debug.LogError("[MonsterController] _visual 未赋值");
                _hasVisualError = true;
            }
            if (_spriteRenderer == null)
            {
                Debug.LogError("[MonsterController] _spriteRenderer 未赋值");
                _hasSpriteRendererError = true;
            }
        }

        public void Initialize(int laneIndex, MonsterData data, MonsterConfig config, LaneManager laneManager)
        {
            LaneIndex = laneIndex;
            Data = data;
            _config = config;
            _laneManager = laneManager;
            _posIndex = 0;
            _moveTimer = config.MoveIntervalSeconds;

            if (!_hasSpriteRendererError)
                _spriteRenderer.sprite = data.IdleSprite;

            if (_laneManager.TryGetWaypoint(laneIndex, 0, out Vector2 spawnPosition))
                transform.position = spawnPosition;
            ApplyScale(0f);
        }

        private void Update()
        {
            if (_config == null) return;
            if (_isMoving)
                UpdateMovement();
            else
                UpdateTimer();

            UpdateWrongHitFeedback();
        }

        private void UpdateTimer()
        {
            _moveTimer -= Time.deltaTime;
            if (_moveTimer <= 0f)
                StartMoving();
        }

        private void StartMoving()
        {
            if (_posIndex >= _config.MoveStepCount - 1)
            {
                ReachedTable();
                return;
            }

            _targetPosIndex = _posIndex + 1;
            if (!_laneManager.TryGetWaypoint(LaneIndex, _posIndex, out _moveFromPos)
                || !_laneManager.TryGetWaypoint(LaneIndex, _targetPosIndex, out _moveToPos))
            {
                ReachedTable();
                return;
            }
            _moveProgress = 0f;
            _isMoving = true;
        }

        private void UpdateMovement()
        {
            _moveProgress = Mathf.Min(_moveProgress + Time.deltaTime / _config.MoveDuration, 1f);
            transform.position = Vector2.Lerp(_moveFromPos, _moveToPos, _moveProgress);
            ApplyScale(Mathf.Lerp(_posIndex, _targetPosIndex, _moveProgress));

            if (_moveProgress >= 1f)
                FinishMoving();
        }

        private void FinishMoving()
        {
            _isMoving = false;
            _posIndex = _targetPosIndex;
            _moveTimer = _config.MoveIntervalSeconds;
        }

        private void ApplyScale(float posLerp)
        {
            if (_hasVisualError) return;
            float displayScale = Data != null ? Data.DisplayScale : 1f;
            _visual.localScale = Vector3.one * (_config.ScaleCurve.Evaluate(posLerp) * displayScale);
        }

        private void ReachedTable()
        {
            EventBus<OnMonsterReachedTable>.Publish(new OnMonsterReachedTable(LaneIndex));
            Destroy(gameObject);
        }

        public void Defeat()
        {
            EventBus<OnMonsterDefeated>.Publish(new OnMonsterDefeated(LaneIndex));
            Destroy(gameObject);
        }

        public void TriggerWrongHitFeedback()
        {
            if (_hasSpriteRendererError || Data == null) return;

            _wrongHitTimer = 0f;
            _wrongHitCompletedCount = 0;
            _wrongHitActive = true;
            _wrongHitShowingHit = true;
            _spriteRenderer.sprite = Data.HitSprite;
        }

        private void UpdateWrongHitFeedback()
        {
            if (!_wrongHitActive || _hasSpriteRendererError || Data == null) return;

            _wrongHitTimer += Time.deltaTime;
            if (_wrongHitTimer < _config.WrongHitFlashDuration) return;

            _wrongHitTimer = 0f;
            _wrongHitShowingHit = !_wrongHitShowingHit;

            if (_wrongHitShowingHit)
            {
                _spriteRenderer.sprite = Data.HitSprite;
                return;
            }

            _spriteRenderer.sprite = Data.IdleSprite;
            _wrongHitCompletedCount++;
            if (_wrongHitCompletedCount >= _config.WrongHitFlashCount)
                _wrongHitActive = false;
        }

        public Vector2 GetTargetPosition()
        {
            if (_config == null) return Vector2.zero;
            Vector2 position;
            if (IsPendingMove && _posIndex < _config.MoveStepCount - 1)
                return _laneManager.TryGetWaypoint(LaneIndex, _posIndex + 1, out position) ? position : (Vector2)transform.position;
            return _laneManager.TryGetWaypoint(LaneIndex, _posIndex, out position) ? position : (Vector2)transform.position;
        }
    }
}
